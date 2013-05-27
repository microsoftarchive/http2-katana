using System;
using System.Text;
using System.IO;
using System.Reflection;

namespace SharedProtocol.IO
{
    public static class FileHelper
    {
        public static byte[] GetFile(string localPath)
        {
            string assemblyPath = Assembly.GetEntryAssembly().Location;
            assemblyPath = Path.GetDirectoryName(assemblyPath);

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
