using System;

namespace TurtleRoute
{
    /// <summary>
    /// The primary and shadow results for one sampled request.
    /// </summary>
    public class GeocodingComparison
    {
        public DateTimeOffset Timestamp { get; init; }

        /// <summary>
        /// The address as sent, e.g. "Katwilgweg, 2, 2050, Antwerpen, BE".
        /// </summary>
        public string Query { get; init; }

        public string Country { get; init; }

        /// <summary>
        /// The result that was returned to the caller.
        /// </summary>
        public GeocodingAttempt Primary { get; init; }

        /// <summary>
        /// The candidate's result. It is never returned to the caller.
        /// </summary>
        public GeocodingAttempt Shadow { get; init; }

        /// <summary>
        /// Metres between the two coordinates; null when either side found nothing.
        /// </summary>
        public double? DistanceInMeters { get; init; }

        /// <summary>
        /// Whether the shadow result meets <see cref="GeocodingPipelineOptions.MinimumConfidence"/>, i.e. whether it would have been used.
        /// </summary>
        public bool ShadowAccepted { get; init; }
    }

    public class GeocodingAttempt
    {
        /// <summary>
        /// The provider that answered, or null when nothing was found.
        /// </summary>
        public string Provider { get; init; }

        /// <summary>
        /// Null when nothing was found or the request failed.
        /// </summary>
        public GeocodeResult Result { get; init; }

        public TimeSpan Duration { get; init; }

        /// <summary>
        /// The exception type when the request failed, e.g. "HttpRequestException" or "Timeout".
        /// </summary>
        public string Error { get; init; }
    }
}
