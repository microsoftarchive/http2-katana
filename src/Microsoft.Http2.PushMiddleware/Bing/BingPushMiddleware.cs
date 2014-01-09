using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Push;
using Microsoft.Http2.Push.Bing.BingHelpers;
using Microsoft.Owin;
using System;
using System.Collections.Generic;
using Owin;

namespace Microsoft.Http2.BingPushMiddleware
{
    using PushFunc = Action<IDictionary<string, string[]>>;
    using AddVertFunc = Action<string, string[]>;
    using RemoveVertFunc = Action<string>;

    public class BingPushMiddleware : PushMiddlewareBase
    {
        private const string BingKey = "Aq9ZXVjENT-rbUAS4KTwU_cfDzUYRbepjQzTyghvDPEEvuawmmxFrYhoS2o9gqfO";
        private const string OriginalReq = "Y3A9NTcuNjE2NjY1fjM5Ljg2NjY2NSZsdmw9NCZzdHk9ciZxPXlhcm9zbGF2bA==";

        public BingPushMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        public override async Task Invoke(IOwinContext context)
        {
            var contextEnv = context.Environment;

            if (!contextEnv.ContainsKey(CommonOwinKeys.AdditionalInfo))
            {
                PushFunc pushPromise = null;

                var bingProcessor = new BingRequestProcessor(BingKey, OriginalReq);

                var images = bingProcessor.Process();
                context.Set(CommonOwinKeys.AdditionalInfo, images);

                foreach (var image in images.Where(image => TryGetPushPromise(context, out pushPromise)))
                {
                    Push(context.Request, pushPromise, image);
                }
            }

            else
            {
                var refList = context.Get<List<string>>(CommonOwinKeys.AdditionalInfo);
                var writer = new StreamWriter(context.Response.Body);
                
                var tile11Request =
                    (HttpWebRequest)
                    WebRequest.Create(refList[0]
                        );
                
                var responseStream = tile11Request.GetResponse().GetResponseStream();
                
                using (var ms = new MemoryStream())
                {
                    if (responseStream != null) responseStream.CopyTo(ms);
                     context.Response.ContentLength = ms.Length;
                    writer.Write(ms.ToArray());
                    writer.Flush();
                }

                refList.RemoveAt(0);
            }

            await Next.Invoke(context);
        }

        protected override void Push(IOwinRequest request, PushFunc pushPromise, string pushReference)
        {
            // Copy the headers
            var headers = new HeaderDictionary(
                new Dictionary<string, string[]>(request.Headers, StringComparer.OrdinalIgnoreCase));

            // Populate special HTTP2 headers
            headers[CommonHeaders.Method] = request.Method;
                // TODO: Not all methods are allowed for push.  Don't push, or change to GET?
            headers[CommonHeaders.Scheme] = request.Scheme;
            headers.Remove("Host");
            headers[CommonHeaders.Host] = request.Headers["Host"];

            headers.Remove(CommonHeaders.ContentLength); // Push promises cannot emulate requests with bodies.

            // TODO: What about cache headers? If-Match, If-None-Match, If-Modified-Since, If-Unmodified-Since.
            // If-Match & If-None-Match are multi-value so the client could send e-tags for the primary resource and referenced resources.
            // If-Modified-Since and If-Unmodified-Since are single value, so it may not make sense to apply them for secondary resources.

            pushPromise(headers);
        }
    }
}