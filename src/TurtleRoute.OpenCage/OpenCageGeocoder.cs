using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TurtleRoute.OpenCage
{
    /// <summary>
    /// Geocodes with the OpenCage API (https://opencagedata.com). Results may be stored permanently.
    /// </summary>
    public class OpenCageGeocoder : IGeocoder
    {
        public const string ProviderName = "OpenCage";

        private readonly HttpClient _httpClient;
        private readonly OpenCageOptions _options;

        public OpenCageGeocoder(HttpClient httpClient, OpenCageOptions options)
        {
            ArgumentNullException.ThrowIfNull(httpClient);
            ArgumentNullException.ThrowIfNull(options);

            if (string.IsNullOrWhiteSpace(options.ApiKey))
                throw new ArgumentNullException(nameof(options), "The OpenCage API key is required.");

            _httpClient = httpClient;
            _options = options;
        }

        public async Task<GeocodeResult> GeocodeAsync(Address address, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(address);

            // OpenCage reads addresses best in their local postal format, e.g. "Katwilgweg 2, 2050 Antwerpen, BE".
            string query = Join(", ", Join(" ", address.Street, address.StreetNo), Join(" ", address.ZipCode, address.City), address.State, address.Country);

            OpenCageResult result = await SearchAsync(query, address.Country, cancellationToken);
            if (result?.Geometry == null)
                return null;

            GeocodeConfidence confidence = ToConfidence(result);

            // A match without a road means the street was not found and OpenCage fell back to the postcode or city.
            if (!string.IsNullOrWhiteSpace(address.Street) && !result.Components.ContainsKey("road"))
                confidence = GeocodeConfidence.Low;
            else if (!string.IsNullOrWhiteSpace(address.StreetNo) && !result.Components.ContainsKey("house_number") && confidence == GeocodeConfidence.High)
                confidence = GeocodeConfidence.Medium;

            return ToResult(result, confidence);
        }

        public async Task<GeocodeResult> GeocodeAsync(string address, string countryFilter, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentNullException(nameof(address));

            // An ISO 3166-1 alpha-2 code restricts the search to that country; anything else is added to the query.
            string query = ToCountryCode(countryFilter) == null && !string.IsNullOrWhiteSpace(countryFilter) ? $"{address}, {countryFilter}" : address;

            OpenCageResult result = await SearchAsync(query, countryFilter, cancellationToken);
            if (result?.Geometry == null)
                return null;

            return ToResult(result, ToConfidence(result));
        }

        private async Task<OpenCageResult> SearchAsync(string query, string country, CancellationToken cancellationToken)
        {
            Dictionary<string, string> parameters = new()
            {
                ["q"] = query,
                ["key"] = _options.ApiKey,
                ["limit"] = "1",
                ["no_annotations"] = "1"
            };

            string countryCode = ToCountryCode(country);
            if (countryCode != null)
                parameters["countrycode"] = countryCode;

            if (!string.IsNullOrWhiteSpace(_options.Language))
                parameters["language"] = _options.Language;

            string queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
            Uri requestUri = new(_options.BaseAddress, $"json?{queryString}");

            using HttpResponseMessage response = await _httpClient.GetAsync(requestUri, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // 401 invalid key, 402 quota exceeded, 403 key disabled, 429 rate limited, 503 unavailable.
                OpenCageResponse error = await TryReadAsync(response, cancellationToken);
                throw new HttpRequestException($"OpenCage returned {(int)response.StatusCode}: {error?.Status?.Message ?? response.ReasonPhrase}", null, response.StatusCode);
            }

            OpenCageResponse body = await response.Content.ReadFromJsonAsync<OpenCageResponse>(cancellationToken);
            return body?.Results?.FirstOrDefault();
        }

        private static async Task<OpenCageResponse> TryReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            try
            {
                return await response.Content.ReadFromJsonAsync<OpenCageResponse>(cancellationToken);
            }
            catch (Exception ex) when (ex is JsonException or NotSupportedException)
            {
                return null;
            }
        }

        private static GeocodeResult ToResult(OpenCageResult result, GeocodeConfidence confidence)
            => new(new GeoCoordinate(result.Geometry.Lat, result.Geometry.Lng), confidence, ProviderName);

        // Thresholds are a first estimate and should be tuned against real addresses.
        private static GeocodeConfidence ToConfidence(OpenCageResult result)
        {
            // When OpenCage cannot place the address it falls back to the centre of the postcode area,
            // which can still score 7 (under 5 km) but is never an address match.
            if (result.Components.TryGetValue("_type", out JsonElement type) && type.ValueKind == JsonValueKind.String && type.GetString() == "postcode")
                return GeocodeConfidence.Low;

            if (result.Confidence >= 9) return GeocodeConfidence.High;   // under 500 m
            if (result.Confidence >= 7) return GeocodeConfidence.Medium; // under 5 km
            return GeocodeConfidence.Low;
        }

        private static string ToCountryCode(string country)
        {
            if (string.IsNullOrWhiteSpace(country) || country.Trim().Length != 2)
                return null;

            string code = country.Trim().ToLowerInvariant();
            return code == "uk" ? "gb" : code;
        }

        private static string Join(string separator, params string[] parts)
            => string.Join(separator, parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }
}
