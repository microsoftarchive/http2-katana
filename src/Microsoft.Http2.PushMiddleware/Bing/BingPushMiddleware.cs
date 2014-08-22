using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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
        private const string BingRequestsUrl = "http://www.bing.com/maps/#";
        private const string BingServiceUrl = "http://www.bing.com";
        //Y3A9NTcuNjE2NjY1fjM5Ljg2NjY2NSZsdmw9MyZzdHk9ciZxPVlhcm9zbGF2bA==
        private const string TileExtension = ".jpeg";

        private const string Base64Regex =
            "^([A-Za-z0-9+/]{4})*([A-Za-z0-9+/]{4}|[A-Za-z0-9+/]{3}=|[A-Za-z0-9+/]{2}==)$";

        public BingPushMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        private void DownloadVia11(string url, IOwinContext context)
        {
            var resourceRequest = (HttpWebRequest)WebRequest.Create(url);

            var responseStream = resourceRequest.GetResponse().GetResponseStream();
            //TODO handle null correctly
            if (responseStream == null) 
                return;

            responseStream.CopyTo(context.Response.Body);
            responseStream.Dispose();
        }

        public override async Task Invoke(IOwinContext context)
        {
            var contextEnv = context.Environment;
            Stream responseStream;

            if (!contextEnv.ContainsKey(CommonOwinKeys.AdditionalInfo))
            {
                PushFunc pushPromise = null;
                var path = context.Request.Path.Value;
                var base64Req = path.Remove(0, 1); //remove leading /
                var isHtmlReq = Regex.Match(base64Req, Base64Regex).Success;
                var isJpeg = !isHtmlReq && path.EndsWith(TileExtension);
                var url = String.Empty;

                if (isHtmlReq)
                {
                    var bingProcessor = new BingRequestProcessor(base64Req);

                    var images = bingProcessor.GetTilesSoapRequestsUrls();

                    foreach (var image in images.Where(image => TryGetPushPromise(context, out pushPromise)))
                    {
                        Push(context.Request, pushPromise, image);
                    }

                    url = BingRequestsUrl + base64Req;
                }
                else if (isJpeg)
                {
                    url = BingRequestProcessor.GetSoapUrlFromTileName(path);
                }
                else
                {
                    url = BingServiceUrl + path;
                }

                if (isHtmlReq)
                {
                    var responseString = new WebClient().DownloadString(url); //html on original request
                    //TODO handle errors
                    HtmlProcessor.PreprocessHtml(ref responseString);

                    var response = Encoding.UTF8.GetBytes(responseString);
                    responseStream = context.Response.Body;
                    responseStream.Write(response, 0, response.Length);
                }
                else
                {
                    DownloadVia11(url, context);
                }
            }
            else
            {
                var url = context.Get<string>(CommonOwinKeys.AdditionalInfo);
                DownloadVia11(url, context);
            }

            await Next.Invoke(context);
        }

        protected override void Push(IOwinRequest request, PushFunc pushPromise, string pushReference)
        {
            request.Set(CommonOwinKeys.AdditionalInfo, pushReference);
            // Copy the headers
            var headers = new HeaderDictionary(
                new Dictionary<string, string[]>(request.Headers, StringComparer.OrdinalIgnoreCase));

            // Populate special HTTP2 headers
            headers[PseudoHeaders.Method] = request.Method;
                // TODO: Not all methods are allowed for push.  Don't push, or change to GET?
            headers[PseudoHeaders.Scheme] = request.Scheme;
            headers.Remove(CommonHeaders.Host);
            headers[PseudoHeaders.Authority] = request.Headers[CommonHeaders.Host];
            headers[PseudoHeaders.Path] = BingRequestProcessor.GetTileQuadFromSoapUrl(pushReference);
            headers.Remove(CommonHeaders.ContentLength); // Push promises cannot emulate requests with bodies.

            pushPromise(headers);
        }
    }
}