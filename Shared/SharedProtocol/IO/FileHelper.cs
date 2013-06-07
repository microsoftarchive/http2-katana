using System;
using System.Text;
using System.IO;
using System.Reflection;

namespace SharedProtocol.IO
{
    public class FileHelper : IDisposable
    {
        private FileStream _iostream;

        /// <summary>
        /// Gets file contents.
        /// </summary>
        /// <param name="localPath">The local path.</param>
        /// <returns></returns>
        /// <exception cref="System.IO.FileNotFoundException">Requested file not found</exception>
        public byte[] GetFile(string localPath)
        {
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            const string rootPath = @"\root";

            if (!File.Exists(assemblyPath + rootPath + localPath))
            {
                throw new FileNotFoundException("Requested file not found");
            }

            string content = File.ReadAllText(assemblyPath + rootPath + localPath);
            return Encoding.ASCII.GetBytes(content);
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
                _iostream = new FileStream(path, FileMode.Append);
            }
            _iostream.Write(data, offset, count);
        }

        public void Dispose()
        {
            if (_iostream != null)
            {
                _iostream.Dispose();
            }
        }
    }
}
