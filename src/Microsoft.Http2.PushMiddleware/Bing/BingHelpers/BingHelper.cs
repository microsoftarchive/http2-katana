using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Http2.Push.Bing.BingHelpers
{
    public class BingRequestProcessor
    {
        private readonly string _originalReq;
        private const string Base64ParamsRegex = @"([^&]*=[^&]*)";

        //dimensions in tiles
        private const byte MapHeight = 4;
        private const byte MapWidth = 8;

        public BingRequestProcessor(string originalReq)
        {
            _originalReq = originalReq;
        }

        public List<string> GetTilesSoapRequestsUrls()
        {
            var parameters = ExtractParametersFromBase64(_originalReq);

            return GetTilesUrls(parameters);
        }

        public static string GetTileQuadFromSoapUrl(string soapUrl)
        {
            const string prefix = @"tiles/";
            int prefixIndex = soapUrl.IndexOf(prefix, StringComparison.Ordinal);

            if (prefixIndex == -1)
                throw new Exception("incorrect soap url format");
            int prefixEndIndex = prefixIndex + prefix.Length;

            const string postfix = "?";
            int postfixIndex = soapUrl.IndexOf(postfix, prefixIndex, StringComparison.Ordinal);
            if (postfixIndex == -1)
                throw new Exception("incorrect soap url format");

            return soapUrl.Substring(prefixEndIndex, postfixIndex - prefixEndIndex);
        }

        private static Dictionary<string, string> ExtractParametersFromBase64(String base64Params)
        {
            var parameters = new Dictionary<string, string>();
            var decodedString = Encoding.UTF8.GetString(Convert.FromBase64String(base64Params));

            var match = Regex.Match(decodedString, Base64ParamsRegex);
            while (match.Success)
            {
                var parVal = match.Value.Split(new[]{'='}, StringSplitOptions.RemoveEmptyEntries);
                parameters.Add(parVal[0], parVal[1]);

                match = match.NextMatch();
            }
            var latLon = parameters[CommonNames.Cp].Split(new []{'~'}, StringSplitOptions.RemoveEmptyEntries);
            parameters.Add(CommonNames.Latitude, latLon[0]);
            parameters.Add(CommonNames.Longtitude, latLon[1]);
            parameters.Remove(CommonNames.Cp);

            return parameters;
        }

        private List<string> GetTilesUrls(IDictionary<string, string> parameters)
        {
            var soapRequests = new List<string>();

            var originalLat = Double.Parse(parameters[CommonNames.Latitude]);
            var originalLon = Double.Parse(parameters[CommonNames.Longtitude]);
            var level = int.Parse(parameters[CommonNames.Level]);

            var tiles = GetTiles(originalLat, originalLon, level , MapWidth, MapHeight);
            soapRequests.AddRange(tiles.Select(GetSoapUrl));

            return soapRequests;
        }

        private string GetSoapUrl(Tile tile)
        {
            var tileQuad = TileSystem.LatLongToQuadKey(tile.Latitude, tile.Longitude, tile.Level);
            int origQuad = int.Parse(tileQuad);

            return String.Format("http://t{0}.tiles.virtualearth.net/tiles/a{1}.jpeg?g=2213&mkt={{culture}}&token={{token}}",
                                origQuad % 10, origQuad);
        }

        private IEnumerable<Tile> GetTiles(double lat, double lon, int level, int w, int h)
        {
            int originalPixelx;
            int originalPixely;

            TileSystem.LatLongToPixelXy(lat, lon, level, out originalPixelx, out originalPixely);

            var originalTile = new Tile(lat, lon, originalPixelx, originalPixely, level);

            var tiles = new List<Tile>(8);
            const int width = TileSystem.TileWidth;
            const int height = TileSystem.TileWidth;
            int tilesOnTop = h / 2;
            int tilesOnLeft = w / 2;

            var topLeftTile = GetTileFromPixels(originalTile.XStartPixel - tilesOnTop * width,
                                                originalTile.YStartPixel - tilesOnLeft * height,
                                                level);
         
            for (byte i = 0; i < w; i++)
            {
                for (byte j = 0; j < h; j++)
                {
                    var nextTile = GetTileFromPixels(topLeftTile.XStartPixel + i * width, 
                                                     topLeftTile.YStartPixel + j * height, 
                                                     level);
                    tiles.Add(nextTile);
                }
            }

            return tiles.Where(t => t.Latitude > TileSystem.MinLatitude
                                    && t.Latitude < TileSystem.MaxLatitude
                                    && t.Longitude > TileSystem.MinLongitude
                                    && t.Longitude < TileSystem.MaxLongitude);
        }

        private Tile GetTileFromPixels(int x, int y, int levelOfDetail)
        {
            double lat;
            double lon;
            TileSystem.PixelXyToLatLong(x + TileSystem.TileWidth, y + TileSystem.TileHeight, levelOfDetail, out lat, out lon);
            return new Tile(lat, lon, x, y, levelOfDetail);
        }
    }
}