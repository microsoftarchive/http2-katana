using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Http2.Push.ImageryService;

namespace Microsoft.Http2.Push.Bing.BingHelpers
{
    public class BingRequestProcessor
    {
        private readonly string _bingKey = "Aq9ZXVjENT-rbUAS4KTwU_cfDzUYRbepjQzTyghvDPEEvuawmmxFrYhoS2o9gqfO";
        private readonly string _originalReq = "Y3A9NTcuNjE2NjY1fjM5Ljg2NjY2NSZsdmw9NCZzdHk9ciZxPXlhcm9zbGF2bA==";
        private const string Base64ParamsRegex = @"([^&]*=[^&]*)";

        private const int TileWidth = 256;
        private const int TileHeight = 256;
        private const int MaxReceivedDataSizeFromSoap = 10240000; //That's big enough

        public BingRequestProcessor(string bingKey, string originalReq)
        {
            _bingKey = bingKey;
            _originalReq = originalReq;
        }

        public IEnumerable<string> Process()
        {
            var parameters = ExtractParametersFromBase64(_originalReq);

            var binding = new BasicHttpBinding(BasicHttpSecurityMode.None)
                {
                    MaxReceivedMessageSize = MaxReceivedDataSizeFromSoap
                };

            var addr = new EndpointAddress(new Uri(CommonNames.ImageryServiceAddr));

            var client = new ImageryServiceClient(binding, addr);

            var requests = GetAroundTilesRequests(parameters);

            return requests.Select(client.GetImageryMetadata).Select(resp => resp.Results[0].ImageUri).ToList();
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
            string[] latLon = parameters[CommonNames.Cp].Split(new []{'~'}, StringSplitOptions.RemoveEmptyEntries);
            parameters.Add(CommonNames.Latitude, latLon[0]);
            parameters.Add(CommonNames.Longtitude, latLon[1]);
            parameters.Remove(CommonNames.Cp);

            return parameters;
        }

        private IEnumerable<ImageryMetadataRequest> GetAroundTilesRequests(IDictionary<string, string> parameters)
        {
            var requests = new List<ImageryMetadataRequest>();

            var originalLat = Double.Parse(parameters[CommonNames.Latitude]);
            var originalLon = Double.Parse(parameters[CommonNames.Longtitude]);
            var level = int.Parse(parameters[CommonNames.Level]);

            int originalPixelx;
            int originalPixely;

            TileSystem.LatLongToPixelXy(originalLat, originalLon, level, out originalPixelx, out originalPixely);

            var originalTile = new Tile(originalLat, originalLon, originalPixelx, originalPixely, level);

            var aroundTiles = GetAroundTilesClockwise(originalTile);

            foreach (var tile in aroundTiles)
            {
                var request = new ImageryMetadataRequest();

                var options = new ImageryMetadataOptions();

                var location = new Location {Latitude = tile.Latitude, Longitude = tile.Longitude};
                options.Location = location;

                options.ReturnImageryProviders = true;
                options.UriScheme = UriScheme.Http;

                options.ZoomLevel = tile.Level;
                request.Culture = CommonNames.CultureEnUs;
                request.Style = MapStyle.Aerial;
                var cred = new Credentials {ApplicationId = _bingKey};

                request.Credentials = cred;
                request.Options = options;

                requests.Add(request);
            }

            return requests;
        }

        private IEnumerable<Tile> GetAroundTilesClockwise(Tile tile)
        {
            var aroundTiles = new Tile[8];

            aroundTiles[0] = GetTileFromPixels(tile.XStartPixel + TileWidth, tile.YStartPixel, tile.Level);               //plusXTile
            aroundTiles[1] = GetTileFromPixels(tile.XStartPixel + TileWidth, tile.YStartPixel + TileHeight, tile.Level);  //plusXplusYTile
            aroundTiles[2] = GetTileFromPixels(tile.XStartPixel, tile.YStartPixel + TileHeight, tile.Level);              //plusYTile
            aroundTiles[3] = GetTileFromPixels(tile.XStartPixel + TileWidth, tile.YStartPixel - TileHeight, tile.Level);  //plusXminusYTile

            aroundTiles[4] = GetTileFromPixels(tile.XStartPixel - TileWidth, tile.YStartPixel + TileHeight, tile.Level);  //minusXplusYTile
            aroundTiles[5] = GetTileFromPixels(tile.XStartPixel - TileWidth, tile.YStartPixel, tile.Level);               //minusXTile
            aroundTiles[6] = GetTileFromPixels(tile.XStartPixel - TileWidth, tile.YStartPixel - TileHeight, tile.Level);  //minusXminusYTile
            aroundTiles[7] = GetTileFromPixels(tile.XStartPixel, tile.YStartPixel - TileHeight, tile.Level);              //minusYTile

            return aroundTiles.Where(t => t.Latitude > TileSystem.MinLatitude
                                    && t.Latitude < TileSystem.MaxLatitude
                                    && t.Longitude > TileSystem.MinLongitude
                                    && t.Longitude < TileSystem.MaxLongitude);
        }

        private Tile GetTileFromPixels(int x, int y, int zoomLevel)
        {
            double lat;
            double lon;
            TileSystem.PixelXyToLatLong(x + TileWidth, y + TileHeight, zoomLevel, out lat, out lon);
            return new Tile(lat, lon, x, y, zoomLevel);
        }
    }
}