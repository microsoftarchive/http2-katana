using System.Text;

namespace SharedProtocol.Pages
{
    public class NotFound404
    {
        public static byte[] Bytes { get { return Encoding.UTF8.GetBytes(NotFoundHtml); } }

        private const string NotFoundHtml = 
            "<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\" http://www.w3.org/TR/html4/strict.dtd >"
             + "<HTML><HEAD><TITLE>404: Not Found</TITLE>"
             + "<META HTTP-EQUIV=\"Content-Type\" Content=\"text/html; charset=Windows-1252\" >"
             + "</HEAD>"
             + "<BODY>"
             + "<h1><b>Error 404 - Not Found.</b></h1>"
             + "</p>No context on this server matched or handled this request.<p>"
             + "</BODY>"
             + "</HTML>";
    }
}
