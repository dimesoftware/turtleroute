using System;

namespace TurtleRoute.OpenCage
{
    public class OpenCageOptions
    {
        public string ApiKey { get; set; }

        public Uri BaseAddress { get; set; } = new("https://api.opencagedata.com/geocode/v1/");

        /// <summary>
        /// Optional IETF language tag for the returned address components, e.g. "nl" or "fr".
        /// </summary>
        public string Language { get; set; }
    }
}
