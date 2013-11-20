using System.IO;
using System.Threading.Tasks;
using Microsoft.Http2.Protocol.Tests;
using Microsoft.Owin;

namespace Http2.Katana.Tests
{

    public  class ResponseMiddleware : OwinMiddleware
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
                case "/10mbTest.txt":
                    writer.Write(TestHelpers.FileContent5bTest);
                    owinResponse.ContentLength = TestHelpers.FileContent5bTest.Length;
                    break;
                case "/simpleTest.txt":
                    writer.Write(TestHelpers.FileContentSimpleTest);
                    owinResponse.ContentLength = TestHelpers.FileContentSimpleTest.Length;
                    break;
                case "/emptyFile.txt":
                    writer.Write(TestHelpers.FileContentEmptyFile);
                    owinResponse.ContentLength = TestHelpers.FileContentEmptyFile.Length;
                    break;
                default:
                    writer.Write(TestHelpers.FileContentAnyFile);
                    owinResponse.ContentLength = TestHelpers.FileContentAnyFile.Length;
                    break;
            }

            await writer.FlushAsync();
        }

    }
   
}
