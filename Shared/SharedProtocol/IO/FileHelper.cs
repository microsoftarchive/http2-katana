using System;
using System.Text;
using System.IO;
using System.Reflection;
using System.Threading;

namespace SharedProtocol.IO
{
    public static class FileHelper
    {
        public static object writeLock = new object();

        public static byte[] GetFile(string localPath)
        {
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string rootPath = @"\root";

            if (!File.Exists(assemblyPath + rootPath + localPath))
            {
                throw new FileNotFoundException("Requested file not found");
            }

            string content = File.ReadAllText(assemblyPath + rootPath + localPath);
            return Encoding.ASCII.GetBytes(content);
        }


        public static void SaveToFile(byte[] data, int offset, int count, string path, bool append)
        {
            //Sync write streams and do not let multiple streams to write the same file. Avoid data mixing and access exceptions.
            lock (writeLock)
            {
                if (!append && File.Exists(path))
                {
                    File.Delete(path);
                }

                using (var stream = new FileStream(path, FileMode.Append))
                {
                    stream.Write(data, offset, count);
                }
            }
        }
    }
}
