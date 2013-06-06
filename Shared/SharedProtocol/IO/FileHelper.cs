using System;
using System.Text;
using System.IO;
using System.Reflection;

namespace SharedProtocol.IO
{
    public class FileHelper : IDisposable
    {
        private readonly object _writeLock = new object();
        private FileStream _iostream;

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
            //_iostream.Flush();
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
