using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.Core.GeoJson;
using Azure.Maps.Search;
using Azure.Maps.Search.Models;

namespace TurtleRoute
{
    public class Geocoder
    {
        private readonly MapsSearchClient _client;

        public Geocoder(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentNullException(nameof(token));

            _client = new MapsSearchClient(new AzureKeyCredential(token));
        }

        /// <summary>
        /// Geocode structured address parts. Returns null when no reliable result.
        /// </summary>
        public async Task<GeoCoordinate?> GeocodeAsync(string street, string streetNo, string zipCode, string city, string state, string country)
        {
            if (string.IsNullOrWhiteSpace(street) && string.IsNullOrWhiteSpace(city) && string.IsNullOrWhiteSpace(country))
                throw new ArgumentException("Provide at least street, city or country.");

            string query = BuildAddress(street, streetNo, zipCode, city, state, country);

            Response<GeocodingResponse> searchResult = await _client.GetGeocodingAsync(query);

            IReadOnlyList<FeaturesItem> features = searchResult?.Value?.Features;
            if (features == null || features.Count == 0)
                return null;

            FeaturesItem best = features[0];
            if (best?.Properties?.Confidence == ConfidenceEnum.Low)
                return null;

            GeoPosition? coords = best?.Geometry?.Coordinates;
            if (!coords.HasValue)
                return null;

            // Azure.Core.GeoJson.GeoPosition exposes coordinate values by index: [0]=longitude, [1]=latitude.
            double longitude = coords.Value[0];
            double latitude = coords.Value[1];

            return new GeoCoordinate(latitude, longitude);
        }

        /// <summary>
        /// Geocode freeform address with optional country filter. Returns null when no reliable result.
        /// </summary>
        public async Task<GeoCoordinate?> GeocodeAsync(string address, string countryFilter)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentNullException(nameof(address));

            string query = string.IsNullOrWhiteSpace(countryFilter) ? address : $"{address}, {countryFilter}";

            Response<GeocodingResponse> searchResult = await _client.GetGeocodingAsync(query);

            IReadOnlyList<FeaturesItem> features = searchResult?.Value?.Features;
            if (features == null || features.Count == 0)
                return null;

            FeaturesItem best = features.OrderBy(f => f?.Properties?.Confidence, new ConfidenceComparer()).FirstOrDefault();

            if (best?.Properties?.Confidence == ConfidenceEnum.Low)
                return null;

            GeoPosition? coords = best?.Geometry?.Coordinates;
            if (!coords.HasValue)
                return null;

            double longitude = coords.Value[0];
            double latitude = coords.Value[1];

            return new GeoCoordinate(latitude, longitude);
        }

        /// <summary>
        /// Geocode structured address parts and return the full Azure Maps response.
        /// Intended for troubleshooting tooling that needs to see the raw vendor payload.
        /// </summary>
        public async Task<GeocodingRawResult?> GeocodeRawAsync(string street, string streetNo, string zipCode, string city, string state, string country)
        {
            if (string.IsNullOrWhiteSpace(street) && string.IsNullOrWhiteSpace(city) && string.IsNullOrWhiteSpace(country))
                throw new ArgumentException("Provide at least street, city or country.");

            string query = BuildAddress(street, streetNo, zipCode, city, state, country);
            Response<GeocodingResponse> response = await _client.GetGeocodingAsync(query);
            return BuildRawResult(response);
        }

        /// <summary>
        /// Geocode freeform address with optional country filter and return the full Azure
        /// Maps response. Intended for troubleshooting tooling that needs to see the raw
        /// vendor payload.
        /// </summary>
        public async Task<GeocodingRawResult?> GeocodeRawAsync(string address, string countryFilter)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentNullException(nameof(address));

            // Modern Azure Maps API forbids mixing the free-text `query` parameter with
            // structured params like countryRegion, so append the country into the query.
            string query = string.IsNullOrWhiteSpace(countryFilter) ? address : $"{address}, {countryFilter}";
            Response<GeocodingResponse> response = await _client.GetGeocodingAsync(query);
            return BuildRawResult(response);
        }

        private static GeocodingRawResult? BuildRawResult(Response<GeocodingResponse> response)
        {
            if (response == null)
                return null;

            JsonElement raw = default;
            BinaryData content = response.GetRawResponse()?.Content;
            if (content != null && content.ToMemory().Length > 0)
                raw = JsonDocument.Parse(content.ToMemory()).RootElement;

            IReadOnlyList<FeaturesItem> features = response.Value?.Features;
            int count = features?.Count ?? 0;

            FeaturesItem best = features != null && features.Count > 0 ? features[0] : null;
            string bestConfidence = best?.Properties?.Confidence?.ToString();

            GeoCoordinate? bestCoord = null;
            GeoPosition? coords = best?.Geometry?.Coordinates;
            if (coords.HasValue)
                bestCoord = new GeoCoordinate(coords.Value[1], coords.Value[0]);

            return new GeocodingRawResult
            {
                Raw = raw,
                FeatureCount = count,
                BestConfidence = bestConfidence,
                BestCoordinate = bestCoord
            };
        }

        private static string BuildAddress(params string[] parts)
            => string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }
}