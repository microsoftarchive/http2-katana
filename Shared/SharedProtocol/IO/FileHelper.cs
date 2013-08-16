using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Org.Mentalis.Security.Ssl;
using SharedProtocol.Extensions;
using System.Linq;

namespace SharedProtocol.IO
{
    /// <summary>
    /// This class compares files by their md5hash, gets the file content, saves data to file.
    /// </summary>
    public class FileHelper : IDisposable
    {
        private readonly Dictionary<string, FileStream> _pathStreamDict;
        private readonly ConnectionEnd _end;

        public FileHelper(ConnectionEnd end)
        {
            _pathStreamDict = new Dictionary<string, FileStream>(5);
            _end = end;
        }

        public bool CompareFiles(string file1, string file2)
        {
            byte[] file1Md5;
            byte[] file2Md5;

            try
            {
                file1Md5 = MathEx.ComputeMD5ChecksumOf(file1);
                file2Md5 = MathEx.ComputeMD5ChecksumOf(file2);
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            

            return file1Md5.SequenceEqual(file2Md5); 
        }

        /// <summary>
        /// Gets file contents.
        /// </summary>
        /// <param name="localPath">The local path.</param>
        /// <returns></returns>
        /// <exception cref="System.IO.FileNotFoundException">Requested file not found</exception>
        public byte[] GetFile(string localPath)
        {
            //Remove file:// from Assembly.GetExecutingAssembly().CodeBase
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Substring(8));
            
            const string rootPath = @"\Root";

            string path;

            if (_end == ConnectionEnd.Server)
            {
                path = assemblyPath + rootPath + "\\" + localPath.Trim('\\');
            }
            else
            {
                path = localPath;
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Requested file not found", localPath);
            }
            return File.ReadAllBytes(path);
        }

        /// <summary>
        /// Saves to file.
        /// Use this method under synchronising lock.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <param name="path">The path to file.</param>
        /// <param name="append">if set to <c>true</c> content will be append to the existing file.</param>
        public void SaveToFile(byte[] data, int offset, int count, string path, bool append)
        {
            //Sync write streams and do not let multiple streams to write the same file. Avoid data mixing and access exceptions.
            if (!append && File.Exists(path))
            {
                File.Delete(path);
            }

            if (!append)
            {
                _pathStreamDict.Add(path, new FileStream(path, FileMode.Append));
            }
            _pathStreamDict[path].Write(data, offset, count);
        }

        public void RemoveStream(string path)
        {
            if (_pathStreamDict.ContainsKey(path))
            {            
                _pathStreamDict[path].Close();
                _pathStreamDict.Remove(path);
            }
        }

        public void Dispose()
        {
            foreach (var fileStream in _pathStreamDict.Values)
            {
                fileStream.Close();
            }
        }
    }
}
