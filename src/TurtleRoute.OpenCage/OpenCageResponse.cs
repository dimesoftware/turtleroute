using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TurtleRoute.OpenCage
{
    internal class OpenCageResponse
    {
        [JsonPropertyName("results")]
        public List<OpenCageResult> Results { get; set; } = [];

        [JsonPropertyName("status")]
        public OpenCageStatus Status { get; set; }
    }

    internal class OpenCageResult
    {
        /// <summary>
        /// 0-10: how small the bounding box of the match is. 10 is under 250 m, 7 under 5 km, 1 over 25 km, 0 unknown.
        /// </summary>
        [JsonPropertyName("confidence")]
        public int Confidence { get; set; }

        [JsonPropertyName("geometry")]
        public OpenCageGeometry Geometry { get; set; }

        [JsonPropertyName("formatted")]
        public string Formatted { get; set; }

        [JsonPropertyName("components")]
        public Dictionary<string, JsonElement> Components { get; set; } = [];
    }

    internal class OpenCageGeometry
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lng")]
        public double Lng { get; set; }
    }

    internal class OpenCageStatus
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
