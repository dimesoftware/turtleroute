using System.Collections.Generic;
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
