using System.IO;
using System.Threading.Tasks;
using Microsoft.Http2.Protocol.Tests;
using Microsoft.Owin;

namespace Http2.Katana.Tests
{
    public class ResponseMiddleware : OwinMiddleware
    {
        public ResponseMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        public override async Task Invoke(IOwinContext context)
        {
            // process response
            var owinResponse = new OwinResponse(context.Environment) { ContentType = "text/plain" };
            var owinRequest = new OwinRequest(context.Environment);
            var writer = new StreamWriter(owinResponse.Body);
            switch (owinRequest.Path.Value)
            {
                case "/root/10mbTest.txt":
                    writer.Write(TestHelper.FileContent5bTest);
                    owinResponse.ContentLength = TestHelper.FileContent5bTest.Length;
                    break;
                case "/root/simpleTest.txt":
                    writer.Write(TestHelper.FileContentSimpleTest);
                    owinResponse.ContentLength = TestHelper.FileContentSimpleTest.Length;
                    break;
                case "/root/index.html":
                    writer.Write(TestHelper.FileContentIndex);
                    owinResponse.ContentLength = TestHelper.FileContentIndex.Length;
                    break;
                case "/root/emptyFile.txt":
                    writer.Write(TestHelper.FileContentEmptyFile);
                    owinResponse.ContentLength = TestHelper.FileContentEmptyFile.Length;
                    break;
                default:
                    writer.Write(TestHelper.FileContentAnyFile);
                    owinResponse.ContentLength = TestHelper.FileContentAnyFile.Length;
                    break;
            }

            await writer.FlushAsync();
        }

    }
   
}
