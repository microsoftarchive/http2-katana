//-----------------------------------------------------------------------
// <copyright file="ContentTypes.cs" company="Microsoft Open Technologies, Inc.">
//
// The copyright in this software is being made available under the BSD License, included below. 
// This software may be subject to other third party and contributor rights, including patent rights, 
// and no such rights are granted under this license.
//
// Copyright (c) 2012, Microsoft Open Technologies, Inc. 
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, 
// are permitted provided that the following conditions are met:
//
// - Redistributions of source code must retain the above copyright notice, 
//   this list of conditions and the following disclaimer.
// - Redistributions in binary form must reproduce the above copyright notice, 
//   this list of conditions and the following disclaimer in the documentation 
//   and/or other materials provided with the distribution.
// - Neither the name of Microsoft Open Technologies, Inc. nor the names of its contributors 
//   may be used to endorse or promote products derived from this software 
//   without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS “AS IS” AND ANY EXPRESS OR IMPLIED WARRANTIES, 
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS 
// FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, 
// INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, 
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) 
// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, 
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, 
// EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
// </copyright>
//-----------------------------------------------------------------------
namespace Client
{
    using System.IO;

    /// <summary>
    /// Content types helper class
    /// </summary>
    public static class ContentTypes
    {
        /// <summary>
        /// Plain text definition
        /// </summary>
        public const string TextPlain = "text/plain";

        /// <summary>
        /// CSS text definition
        /// </summary>
        public const string TextCss = "text/css";

        /// <summary>
        /// HTML text definition
        /// </summary>
        public const string TextHtml = "text/html";

        /// <summary>
        /// Script text definition
        /// </summary>
        public const string TextScript = "text/script";

        /// <summary>
        /// Returns file type from file extension.
        /// </summary>
        /// <param name="name">File extension name.</param>
        /// <returns>File type.</returns>
        public static string GetTypeFromFileName(string name)
        {
            switch (Path.GetExtension(name))
            {
                case ".html":
                case ".htm":
                    return TextHtml;
                case ".js":
                    return TextScript;
                case ".css":
                    return TextCss;
                default:
                    return TextPlain;
            }                                                            
        }
    }
}
