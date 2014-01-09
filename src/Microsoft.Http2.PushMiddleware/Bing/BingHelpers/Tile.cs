namespace Microsoft.Http2.Push.Bing.BingHelpers
{
    class Tile
    {
        public Tile(double latitude, double longitude, int xStartPixel, int yStartPixel, int level)
        {
            Latitude = latitude;
            XStartPixel = xStartPixel;
            Longitude = longitude;
            YStartPixel = yStartPixel;
            Level = level;
        }

        public int Level { get; set; }

        public int XStartPixel { get; set; }

        public int YStartPixel { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }
    }
}
