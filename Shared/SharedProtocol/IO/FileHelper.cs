using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace SharedProtocol.IO
{
    public class FileHelper : IDisposable
    {
        private Dictionary<string, FileStream> _pathStreamDict;

        public FileHelper()
        {
            _pathStreamDict = new Dictionary<string, FileStream>(5);
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
            
            const string rootPath = @"\root";

            if (!File.Exists(assemblyPath + rootPath + localPath))
            {
                throw new FileNotFoundException("Requested file not found");
            }

            return File.ReadAllBytes(assemblyPath + rootPath + localPath);
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
            _pathStreamDict[path].Flush();
        }

        public void RemoveStream(string path)
        {
            _pathStreamDict[path].Close();
            _pathStreamDict.Remove(path);
        }

        public void Dispose()
        {
            foreach (var fileStream in _pathStreamDict.Values)
            {
                fileStream.Flush();
                fileStream.Dispose();
            }
        }
    }
}
