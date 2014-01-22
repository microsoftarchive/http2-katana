using System;
using System.Text;

namespace Microsoft.Http2.BingPushMiddleware
{
    internal static class HtmlProcessor
    {
        private const string AddTileFunc = "function addTile(quadKey, offset)";
        private const string ImgSrc = "img.src";
        private const string ImgSrcAssign = ImgSrc + " = ";

        //function addTile(quadKey, offset)
		//{
		//	var img = document.createElement("img");
		//	img.src = _tileFormat.replace("{quadkey}", quadKey).replace("{subdomain}", quadKey.charAt(quadKey.length - 1));
		//	img.src.replace("http:", window.location.protocol);
		//	img.style.cssText = "position:absolute;top:" + offset.y + "px;left:" + offset.x + "px";
		//	_tileContainer.appendChild(img);
		//}

        //This method modifies 
        //img.src = _tileFormat.replace("{quadkey}", quadKey).replace("{subdomain}", quadKey.charAt(quadKey.length - 1));
        // to be
        //img.src = "a" + quadkey + ".jpeg";
        internal static void PreprocessHtml(ref string html)
        {
            const string toReplace =
                "img.src = _tileFormat.replace(\"{quadkey}\", quadKey).replace(\"{subdomain}\", quadKey.charAt(quadKey.length - 1));";
            const string replaceWith = "img.src = quadKey + \".jpeg\";";

            html = html.Replace(toReplace, replaceWith);
        }
    }
}
