//-----------------------------------------------------------------------
// <copyright file="XHtmlDocument.cs" company="Microsoft Open Technologies, Inc.">
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
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// HTML parser class
    /// </summary>
    public class XHtmlDocument
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="XHtmlDocument"/> class.
        /// </summary>
        /// <param name="doc">the document.</param>
        private XHtmlDocument(XDocument doc)
        {
            this.Document = doc;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets image name list.
        /// </summary>
        public string[] Images { get; private set; }

        /// <summary>
        /// Gets links name list.
        /// </summary>
        public string[] Links { get; private set; }

        /// <summary>
        /// Gets or sets scripts name list.
        /// </summary>
        public string[] Scripts { get; set; }

        /// <summary>
        /// Gets document.
        /// </summary>
        public XDocument Document { get; private set; }

        #endregion

        /// <summary>
        /// Document parser.
        /// </summary>
        /// <param name="content">The document to parse.</param>
        /// <returns>The parsed document.</returns>
        public static XHtmlDocument Parse(string content)
        {
            var strContent = content.Trim(new[] { ' ', '\uFEFF', '\r', '\n' });
            if (content.Length == 0)
            {
                return new XHtmlDocument(new XDocument());
            }

            XDocument doc = XDocument.Parse(strContent);
            XHtmlDocument htmDoc = new XHtmlDocument(doc);
            if (doc.Root != null)
            {
                XNamespace ns = "http://www.w3.org/1999/xhtml";
                htmDoc.Images = (from img in doc.Root.Descendants(ns + "img")
                                    let xAttribute = img.Attribute("src")
                                    where xAttribute != null
                                    select xAttribute.Value).Distinct().ToArray();
                htmDoc.Links = (from linc in doc.Root.Descendants(ns + "link")
                                let xAttribute = linc.Attribute("href")
                                where xAttribute != null
                                select xAttribute.Value).Distinct().ToArray();
                htmDoc.Scripts = (from script in doc.Root.Descendants(ns + "script")
                                let xAttribute = script.Attribute("src")
                                where xAttribute != null
                                select xAttribute.Value).Distinct().ToArray();
            }

            return htmDoc;
        }
    }
}
