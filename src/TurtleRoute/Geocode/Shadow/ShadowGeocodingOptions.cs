using System;

namespace TurtleRoute
{
    public class ShadowGeocodingOptions
    {
        /// <summary>
        /// The share of requests also sent to the shadow geocoder, from 0 to 1. Defaults to 0.1.
        /// </summary>
        public double SampleRate { get; set; } = 0.1;

        /// <summary>
        /// How often the comparison summary is logged. Defaults to one hour.
        /// </summary>
        public TimeSpan SummaryInterval { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// An accepted shadow result further than this from the primary result counts as a disagreement. Defaults to 250 m.
        /// </summary>
        public double DisagreementThresholdInMeters { get; set; } = 250;

        /// <summary>
        /// The number of disagreements per interval logged individually, with their address. The rest are only counted. Defaults to 5.
        /// </summary>
        public int MaxDisagreementsPerInterval { get; set; } = 5;

        /// <summary>
        /// The number of countries broken down in the summary; the rest are grouped. Defaults to 10.
        /// </summary>
        public int MaxCountries { get; set; } = 10;

        /// <summary>
        /// Requests waiting for the shadow geocoder. When full, new samples are dropped rather than blocking the caller. Defaults to 1000.
        /// </summary>
        public int QueueCapacity { get; set; } = 1000;

        /// <summary>
        /// The rate at which the shadow geocoder is called, to stay within its plan. Defaults to 1, the OpenCage free trial limit.
        /// </summary>
        public double MaxRequestsPerSecond { get; set; } = 1;

        /// <summary>
        /// How long a shadow request may take before it counts as an error. Defaults to 10 seconds.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
    }
}
