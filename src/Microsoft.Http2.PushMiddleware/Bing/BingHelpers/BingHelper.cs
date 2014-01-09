using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Http2.Push.ImageryService;

namespace BingHelper
{
    public class BingRequestProcessor
    {
        private readonly string BingKey = "Aq9ZXVjENT-rbUAS4KTwU_cfDzUYRbepjQzTyghvDPEEvuawmmxFrYhoS2o9gqfO";
        private readonly string OriginalReq = "Y3A9NTcuNjE2NjY1fjM5Ljg2NjY2NSZsdmw9NCZzdHk9ciZxPXlhcm9zbGF2bA==";

        public BingRequestProcessor(string bingKey, string originalReq)
        {
            BingKey = bingKey;
            OriginalReq = originalReq;
        }

        // ReSharper disable UnusedParameter.Local
        public IEnumerable<String> Process(Stream output)
            // ReSharper restore UnusedParameter.Local
        {
            var parameters = ExtractParametersFromBase64(OriginalReq);

            BasicHttpBinding binding = new BasicHttpBinding(BasicHttpSecurityMode.None);
            EndpointAddress addr =
                new EndpointAddress(
                    new Uri("http://dev.virtualearth.net/webservices/v1/imageryservice/imageryservice.svc"));
            var client = new ImageryServiceClient(binding, addr);

            var requests = GetAroundTilesRequests(parameters);
            var imageUrls = new List<String>();

            foreach (var imageryMetadataRequest in requests)
            {
                var resp = client.GetImageryMetadata(imageryMetadataRequest);

                imageUrls.Add(resp.Results[0].ImageUri);
            }

            return imageUrls;
        }

        private Dictionary<string, string> ExtractParametersFromBase64(String base64Params)
        {
            var parameters = new Dictionary<string, string>();
            string decodedString = Encoding.UTF8.GetString(Convert.FromBase64String(base64Params));

            Match match = Regex.Match(decodedString, @"([^&]*=[^&]*)");
            while (match.Success)
            {
                string[] parVal = match.Value.Split('=');
                parameters.Add(parVal[0], parVal[1]);

                match = match.NextMatch();
            }
            string[] latLon = parameters["cp"].Split('~');
            parameters.Add("latitude", latLon[0]);
            parameters.Add("longitude", latLon[1]);
            parameters.Remove("cp");
            return parameters;
        }

        private IEnumerable<ImageryMetadataRequest> GetAroundTilesRequests(Dictionary<String, String> parameters)
        {
            var requests = new List<ImageryMetadataRequest>();

            Double originalLat = Double.Parse(parameters["latitude"]);
            Double originalLon = Double.Parse(parameters["longitude"]);
            int level = int.Parse(parameters["lvl"]);

            int originalPixelx;
            int originalPixely;

            TileSystem.LatLongToPixelXy(originalLat, originalLon, level, out originalPixelx, out originalPixely);

            var originalTile = new Tile(originalLat, originalLon, originalPixelx, originalPixely, level);

            IEnumerable<Tile> aroundTiles = GetAroundTilesClockwise(originalTile);


            foreach (var tile in aroundTiles)
            {
                var request = new ImageryMetadataRequest();

                var options = new ImageryMetadataOptions();

                var location = new Location {Latitude = tile.Latitude, Longitude = tile.Longitude};
                options.Location = location;

                options.ReturnImageryProviders = true;
                options.UriScheme = UriScheme.Http;

                options.ZoomLevel = tile.Level;
                request.Culture = "en-US";
                request.Style = MapStyle.Aerial;
                var cred = new Credentials {ApplicationId = BingKey};

                request.Credentials = cred;
                request.Options = options;

                requests.Add(request);
            }

            return requests;
        }

        private IEnumerable<Tile> GetAroundTilesClockwise(Tile tile)
        {
            var aroundTiles = new List<Tile>();

            Tile plusXTile = GetTileFromPixels(tile.XStartPixel + 256, tile.YStartPixel, tile.Level);
            Tile plusXyTile = GetTileFromPixels(tile.XStartPixel + 256, tile.YStartPixel + 256, tile.Level);
            Tile plusYTile = GetTileFromPixels(tile.XStartPixel, tile.YStartPixel + 256, tile.Level);
            Tile minusXplusYTile = GetTileFromPixels(tile.XStartPixel - 256, tile.YStartPixel + 256, tile.Level);
            Tile minusXTile = GetTileFromPixels(tile.XStartPixel - 256, tile.YStartPixel, tile.Level);
            Tile minusXminusYTile = GetTileFromPixels(tile.XStartPixel - 256, tile.YStartPixel - 256, tile.Level);
            Tile minusYTile = GetTileFromPixels(tile.XStartPixel, tile.YStartPixel - 256, tile.Level);
            Tile plusXminusYTile = GetTileFromPixels(tile.XStartPixel + 256, tile.YStartPixel - 256, tile.Level);
            aroundTiles.Add(plusXTile);
            aroundTiles.Add(plusXyTile);
            aroundTiles.Add(plusYTile);
            aroundTiles.Add(minusXplusYTile);
            aroundTiles.Add(minusXTile);
            aroundTiles.Add(minusXminusYTile);
            aroundTiles.Add(minusYTile);
            aroundTiles.Add(plusXminusYTile);

            return aroundTiles.Where(t => t.Latitude > -85 && t.Latitude < 85 && t.Longitude > -85 && t.Longitude < 85);
        }

        private Tile GetTileFromPixels(int x, int y, int zoomLevel)
        {
            Double lat;
            Double lon;
            TileSystem.PixelXyToLatLong(x + 256, y + 256, zoomLevel, out lat, out lon);
            return new Tile(lat, lon, x, y, zoomLevel);
        }
    }
}