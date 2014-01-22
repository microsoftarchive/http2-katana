// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using OpenSSL;
using Microsoft.Http2.Protocol.Extensions;
using System.Linq;

namespace Client.IO
{
    /// <summary>
    /// This class compares files by their md5hash, gets file's content, saves data to specified file.
    /// </summary>
    internal class FileHelper : IDisposable
    {
        private readonly Dictionary<string, FileStream> _pathStreamDict;
        private readonly ConnectionEnd _end;

        public FileHelper(ConnectionEnd end)
        {
            _pathStreamDict = new Dictionary<string, FileStream>(5);
            _end = end;
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
            if (!append)
            {
                if (_pathStreamDict.ContainsKey(path))
                {
                    throw new IOException("File is still downloading");
                }

                if (File.Exists(path))
                    File.Delete(path);

                _pathStreamDict.Add(path, new FileStream(path, FileMode.Append));
            }
            
             _pathStreamDict[path].WriteAsync(data, offset, count);
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
