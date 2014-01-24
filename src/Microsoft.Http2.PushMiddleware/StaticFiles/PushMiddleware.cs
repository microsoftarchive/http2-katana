// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System.Threading.Tasks;
using Microsoft.Http2.Protocol;
using Microsoft.Owin;
using System;
using System.Collections.Generic;
using Owin;

namespace Microsoft.Http2.Push
{
    using PushFunc = Action<IDictionary<string, string[]>>;
    using AddVertFunc = Action<string, string[]>;
    using RemoveVertFunc = Action<string>;

    public class PushMiddleware : PushMiddlewareBase
    {
        protected readonly ReferenceTable _references;

        public PushMiddleware(OwinMiddleware next)
            : base(next)
        {
            _references = new ReferenceTable(new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    {
                        "/root/index.html", new[]
                            {
                                "/root/simpleTest.txt"
                            }
                    },
                    {
                        "/root/simpleTest.txt", new string[0]
                    },
                    {
                        "/root/LookAround.htm", new []
                            {
                                "/root/LookAround_files/BaseStyles.css",
                                "/root/LookAround_files/Demo.css",
                                "/root/LookAround_files/DemoTemplate.css",
                                "/root/LookAround_files/FavIcon.ico",
                                "/root/LookAround_files/ie-logo.png",
                                "/root/LookAround_files/instructions.png",
                                "/root/LookAround_files/loading.gif",
                                "/root/LookAround_files/negx.jpg",
                                "/root/LookAround_files/negy.jpg",
                                "/root/LookAround_files/negz.jpg",
                                "/root/LookAround_files/posx.jpg",
                                "/root/LookAround_files/posy.jpg",
                                "/root/LookAround_files/posz.jpg",
                                "/root/LookAround_files/ReturnAndShareControls.css",
                                "/root/LookAround_files/ReturnAndShareControls.js",
                                "/root/LookAround_files/ReturnButtonImageStrip.png",
                                "/root/LookAround_files/TestDriveCommon.js",
                                "/root/LookAround_files/three.min.js",
                            }
                    },
                    {
                        "/root/LookAround_files/BaseStyles.css", new string[0]
                    },
                    {
                        "/root/LookAround_files/Demo.css", new string[0]
                    },
                    {
                        "/root/LookAround_files/DemoTemplate.css", new string[0]
                    },
                    {
                        "/root/LookAround_files/FavIcon.ico", new string[0]
                    },
                    {
                        "/root/LookAround_files/ie-logo.png", new string[0]
                    },
                    {
                        "/root/LookAround_files/instructions.png", new string[0]
                    },
                    {
                        "/root/LookAround_files/loading.gif", new string[0]
                    },
                    {
                        "/root/LookAround_files/negx.jpg", new string[0]
                    },
                    {
                        "/root/LookAround_files/negy.jpg", new string[0]
                    },
                    {
                        "/root/LookAround_files/negz.jpg", new string[0]
                    },
                    {
                        "/root/LookAround_files/posx.jpg", new string[0]
                    },
                    {
                        "/root/LookAround_files/posy.jpg", new string[0]
                    },
                    {
                        "/root/LookAround_files/posz.jpg", new string[0]
                    },
                    {
                        "/root/LookAround_files/ReturnAndShareControls.css", new string[0]
                    },
                    {
                        "/root/LookAround_files/ReturnAndShareControls.js", new string[0]
                    },
                    {
                        "/root/LookAround_files/ReturnButtonImageStrip.png", new string[0]
                    },
                    {
                        "/root/LookAround_files/TestDriveCommon.js", new string[0]
                    },
                    {
                        "/root/LookAround_files/three.min.js", new string[0]
                    },
                });
        }

        public override async Task Invoke(IOwinContext context)
        {
            var isPushEnabled = context.Get<bool>(CommonOwinKeys.EnableServerPush);

            //If push is not allowed then pass to then next layer
            if (!isPushEnabled)
            {
                await Next.Invoke(context);
                return;
            }

            bool gotError = false;
            var refTree = context.Get<Dictionary<string, string[]>>(CommonOwinKeys.AdditionalInfo);
            //original request from client
            if (refTree == null)
            {
                string root = context.Request.Path.Value;
                if (!_references.ContainsKey(root))
                {
                    gotError = true; // Cant build tree because no such name in the ref table. Should invoke next
                }
                else
                {
                    var refCpy = new ReferenceTable(_references);
                    var tree = new GraphHelper().BuildTree(refCpy, root);
                    refTree = tree;
                    context.Set(CommonOwinKeys.AdditionalInfo, tree);
                }
            }
            if (!gotError)
            {
                PushFunc pushPromise;
                string[] pushReferences;

                if (refTree.TryGetValue(context.Request.Path.Value, out pushReferences)
                    && TryGetPushPromise(context, out pushPromise))
                {
                    foreach (var pushReference in pushReferences)
                    {
                        Push(context.Request, pushPromise, pushReference);
                    }
                }
            }

            context.Set(CommonOwinKeys.AddVertex, new AddVertFunc(AddVertex));
            context.Set(CommonOwinKeys.RemoveVertex, new RemoveVertFunc(RemoveVertex));

            await Next.Invoke(context);
        }

        protected override void Push(IOwinRequest request, PushFunc pushPromise, string pushReference)
        {
            // Copy the headers
            var headers = new HeaderDictionary(
                new Dictionary<string, string[]>(request.Headers, StringComparer.OrdinalIgnoreCase));

            // Populate special HTTP2 headers
            headers[CommonHeaders.Method] = request.Method.ToUpper(); // TODO: Not all methods are allowed for push.  Don't push, or change to GET?
            headers[CommonHeaders.Scheme] = request.Scheme;
            headers.Remove("Host");
            headers[CommonHeaders.Authority] = request.Headers["Host"];
            headers.Remove(CommonHeaders.ContentLength); // Push promises cannot emulate requests with bodies.
            // TODO: What about cache headers? If-Match, If-None-Match, If-Modified-Since, If-Unmodified-Since.
            // If-Match & If-None-Match are multi-value so the client could send e-tags for the primary resource and referenced resources.
            // If-Modified-Since and If-Unmodified-Since are single value, so it may not make sense to apply them for secondary resources.

            // Change the request path to the pushed resource
            headers[CommonHeaders.Path] = pushReference;

            pushPromise(headers);
        }

        protected virtual void RemoveVertex(string key)
        {
            _references.RemoveVertex(key);   
        }

        protected virtual void AddVertex(string key, string[] value)
        {
            _references.AddVertex(key, value);
        }
    }
}