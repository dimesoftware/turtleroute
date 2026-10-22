using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TurtleRoute.Here
{
    /// <summary>
    /// Calculates travel times and distances with the HERE Matrix Routing API v8, using live traffic.
    /// </summary>
    /// <remarks>
    /// When all coordinates fit in a circle of 400 km diameter, the matrix is calculated in region mode: one request of up to 500 x 500.
    /// Otherwise flexible mode is used, which is limited to 15 origins per request. HERE bills 5 x the larger dimension per request
    /// once both dimensions are 5 or more, so region mode is considerably cheaper for larger matrices.
    /// </remarks>
    public class HereRouteMatrix : IRouteMatrix
    {
        private const int MaxRegionRadiusInMeters = 200_000;
        private const int RegionMarginInMeters = 10_000;
        private const int RegionMaxSize = 500;
        private const int FlexibleMaxOrigins = 15;
        private const int FlexibleMaxDestinations = 100;

        private readonly HttpClient _httpClient;
        private readonly HereOptions _options;

        public HereRouteMatrix(HttpClient httpClient, HereOptions options)
        {
            ArgumentNullException.ThrowIfNull(httpClient);
            ArgumentNullException.ThrowIfNull(options);

            if (string.IsNullOrWhiteSpace(options.ApiKey))
                throw new ArgumentNullException(nameof(options), "The HERE API key is required.");

            _httpClient = httpClient;
            _options = options;
        }

        public async Task<Route[,]> GetRouteMatrixAsync(IEnumerable<GeoCoordinate> coordinates, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(coordinates);

            List<HerePoint> points = [.. coordinates.Select(x => new HerePoint(x.Latitude, x.Longitude))];
            int count = points.Count;
            Route[,] routeMatrix = new Route[count, count];

            if (count == 0)
                return routeMatrix;

            HereRegion region = ToRegion(points);
            bool isFlexible = region.Type == "world";
            int maxOrigins = isFlexible ? FlexibleMaxOrigins : RegionMaxSize;
            int maxDestinations = isFlexible ? FlexibleMaxDestinations : RegionMaxSize;

            for (int originStart = 0; originStart < count; originStart += maxOrigins)
            {
                for (int destinationStart = 0; destinationStart < count; destinationStart += maxDestinations)
                {
                    HereMatrixRequest request = new()
                    {
                        Origins = points.GetRange(originStart, Math.Min(maxOrigins, count - originStart)),
                        Destinations = points.GetRange(destinationStart, Math.Min(maxDestinations, count - destinationStart)),
                        RegionDefinition = region,
                        Avoid = new HereAvoid { Features = ["ferry", "dirtRoad"] }
                    };

                    HereMatrix matrix = await CalculateAsync(request, cancellationToken);

                    for (int i = 0; i < matrix.NumOrigins; i++)
                    {
                        for (int j = 0; j < matrix.NumDestinations; j++)
                        {
                            int index = i * matrix.NumDestinations + j;

                            // Pairs without a route (non-zero error code) are left at 0, as the Azure Maps matrix does.
                            bool found = matrix.ErrorCodes == null || matrix.ErrorCodes[index] == 0;
                            routeMatrix[originStart + i, destinationStart + j] = new Route()
                            {
                                Distance = found ? matrix.Distances[index] : 0,
                                Duration = found ? matrix.TravelTimes[index] : 0
                            };
                        }
                    }
                }
            }

            return routeMatrix;
        }

        private async Task<HereMatrix> CalculateAsync(HereMatrixRequest request, CancellationToken cancellationToken)
        {
            Uri requestUri = new(_options.MatrixBaseAddress, $"matrix?async=false&apiKey={Uri.EscapeDataString(_options.ApiKey)}");

            using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(requestUri, request, cancellationToken);
            HereMatrixResponse body = await HereHttp.ReadAsync<HereMatrixResponse>(response, cancellationToken);

            return body?.Matrix ?? throw new InvalidOperationException("HERE did not return a matrix.");
        }

        /// <summary>
        /// A circle around all points when it fits within HERE's 400 km region limit, otherwise the whole world (flexible mode).
        /// </summary>
        internal static HereRegion ToRegion(List<HerePoint> points)
        {
            HerePoint center = new((points.Min(p => p.Lat) + points.Max(p => p.Lat)) / 2, (points.Min(p => p.Lng) + points.Max(p => p.Lng)) / 2);
            double radius = points.Max(p => DistanceInMeters(center, p)) + RegionMarginInMeters;

            if (radius > MaxRegionRadiusInMeters)
                return new HereRegion { Type = "world" };

            return new HereRegion { Type = "circle", Center = center, Radius = (int)Math.Ceiling(radius) };
        }

        private static double DistanceInMeters(HerePoint from, HerePoint to)
        {
            const double earthRadius = 6_371_000;
            double dLat = ToRadians(to.Lat - from.Lat);
            double dLng = ToRadians(to.Lng - from.Lng);
            double a = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Cos(ToRadians(from.Lat)) * Math.Cos(ToRadians(to.Lat)) * Math.Pow(Math.Sin(dLng / 2), 2);
            return 2 * earthRadius * Math.Asin(Math.Sqrt(a));
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180;
    }
}
