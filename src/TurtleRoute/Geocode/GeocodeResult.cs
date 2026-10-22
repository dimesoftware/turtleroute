namespace TurtleRoute
{
    public class GeocodeResult
    {
        public GeocodeResult(GeoCoordinate coordinate, GeocodeConfidence confidence, string provider)
        {
            Coordinate = coordinate;
            Confidence = confidence;
            Provider = provider;
        }

        public GeoCoordinate Coordinate { get; }

        public GeocodeConfidence Confidence { get; }

        /// <summary>
        /// The name of the provider that produced this result, e.g. "AzureMaps" or "OpenCage".
        /// </summary>
        public string Provider { get; }

        public override string ToString()
            => $"{Coordinate} ({Confidence}, {Provider})";
    }
}
