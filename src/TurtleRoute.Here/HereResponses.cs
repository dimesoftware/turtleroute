using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TurtleRoute.Here
{
    internal class HereRoutesResponse
    {
        [JsonPropertyName("routes")]
        public List<HereRoute> Routes { get; set; } = [];

        [JsonPropertyName("notices")]
        public List<HereNotice> Notices { get; set; } = [];
    }

    internal class HereRoute
    {
        /// <summary>
        /// One section per leg: every via waypoint is a stopover that starts a new section.
        /// </summary>
        [JsonPropertyName("sections")]
        public List<HereSection> Sections { get; set; } = [];
    }

    internal class HereSection
    {
        [JsonPropertyName("summary")]
        public HereSummary Summary { get; set; }

        [JsonPropertyName("polyline")]
        public string Polyline { get; set; }
    }

    internal class HereSummary
    {
        /// <summary>
        /// Seconds, including traffic when the route was calculated with a departure time.
        /// </summary>
        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        /// <summary>
        /// Meters.
        /// </summary>
        [JsonPropertyName("length")]
        public int Length { get; set; }
    }

    internal class HereNotice
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }
    }

    internal class HereError
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("cause")]
        public string Cause { get; set; }

        [JsonPropertyName("error_description")]
        public string ErrorDescription { get; set; }
    }

    internal class HereMatrixRequest
    {
        [JsonPropertyName("origins")]
        public List<HerePoint> Origins { get; set; }

        [JsonPropertyName("destinations")]
        public List<HerePoint> Destinations { get; set; }

        [JsonPropertyName("regionDefinition")]
        public HereRegion RegionDefinition { get; set; }

        [JsonPropertyName("transportMode")]
        public string TransportMode { get; set; } = "car";

        [JsonPropertyName("routingMode")]
        public string RoutingMode { get; set; } = "fast";

        [JsonPropertyName("matrixAttributes")]
        public List<string> MatrixAttributes { get; set; } = ["travelTimes", "distances"];

        [JsonPropertyName("avoid")]
        public HereAvoid Avoid { get; set; }
    }

    internal class HerePoint
    {
        public HerePoint(double lat, double lng)
        {
            Lat = lat;
            Lng = lng;
        }

        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lng")]
        public double Lng { get; set; }
    }

    internal class HereRegion
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("center")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public HerePoint Center { get; set; }

        [JsonPropertyName("radius")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Radius { get; set; }
    }

    internal class HereAvoid
    {
        [JsonPropertyName("features")]
        public List<string> Features { get; set; } = [];
    }

    internal class HereMatrixResponse
    {
        [JsonPropertyName("matrix")]
        public HereMatrix Matrix { get; set; }
    }

    internal class HereMatrix
    {
        [JsonPropertyName("numOrigins")]
        public int NumOrigins { get; set; }

        [JsonPropertyName("numDestinations")]
        public int NumDestinations { get; set; }

        /// <summary>
        /// Row-major: the entry for origin i and destination j is at i * NumDestinations + j.
        /// </summary>
        [JsonPropertyName("travelTimes")]
        public List<int> TravelTimes { get; set; } = [];

        [JsonPropertyName("distances")]
        public List<int> Distances { get; set; } = [];

        /// <summary>
        /// 0 when the route was found; any other value means there is no route between the pair.
        /// </summary>
        [JsonPropertyName("errorCodes")]
        public List<int> ErrorCodes { get; set; }
    }

    internal class HereSequenceResponse
    {
        [JsonPropertyName("results")]
        public List<HereSequenceResult> Results { get; set; } = [];
    }

    internal class HereSequenceResult
    {
        [JsonPropertyName("waypoints")]
        public List<HereSequenceWaypoint> Waypoints { get; set; } = [];
    }

    internal class HereSequenceWaypoint
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("sequence")]
        public int Sequence { get; set; }
    }
}
