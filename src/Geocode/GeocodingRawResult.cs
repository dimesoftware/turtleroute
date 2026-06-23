using System.Text.Json;

namespace TurtleRoute
{
    /// <summary>
    /// Result of a raw geocoding call: the full Azure Maps response as JSON plus a few
    /// pre-extracted summary fields. Use this when you need to inspect or display the
    /// vendor response (e.g. troubleshooting tooling), not for normal lookups —
    /// <see cref="Geocoder.GeocodeAsync(string, string)"/> remains the preferred entry point.
    /// </summary>
    public class GeocodingRawResult
    {
        /// <summary>
        /// The full Azure Maps geocoding response as JSON (GeoJSON FeatureCollection).
        /// </summary>
        public JsonElement Raw { get; init; }

        /// <summary>
        /// Number of features returned by Azure Maps.
        /// </summary>
        public int FeatureCount { get; init; }

        /// <summary>
        /// Confidence of the highest-scoring feature ("High", "Medium", "Low"), or null
        /// when no features were returned.
        /// </summary>
        public string? BestConfidence { get; init; }

        /// <summary>
        /// Coordinate of the highest-scoring feature, or null when no features were returned.
        /// </summary>
        public GeoCoordinate? BestCoordinate { get; init; }
    }
}
