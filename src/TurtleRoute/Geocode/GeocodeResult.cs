namespace TurtleRoute
{
    public class GeocodeResult
    {
        public GeocodeResult(GeoCoordinate coordinate, GeocodeConfidence confidence, string provider, string matchedAddress = null)
        {
            Coordinate = coordinate;
            Confidence = confidence;
            Provider = provider;
            MatchedAddress = matchedAddress;
        }

        public GeoCoordinate Coordinate { get; }

        public GeocodeConfidence Confidence { get; }

        /// <summary>
        /// The name of the provider that produced this result, e.g. "AzureMaps" or "OpenCage".
        /// </summary>
        public string Provider { get; }

        /// <summary>
        /// The address the provider matched, as it formats it, e.g. "Katwilgweg 2, 2050 Antwerpen, Belgium".
        /// It shows what the coordinate belongs to, e.g. only the postcode or city when the street was not found. Null when the provider did not return one.
        /// </summary>
        public string MatchedAddress { get; }

        public override string ToString()
            => MatchedAddress == null ? $"{Coordinate} ({Confidence}, {Provider})" : $"{Coordinate} ({Confidence}, {Provider}: {MatchedAddress})";
    }
}
