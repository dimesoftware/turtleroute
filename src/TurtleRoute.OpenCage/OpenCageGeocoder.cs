using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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

        // OpenCage asks clients to identify themselves with a version.
        private static readonly ProductInfoHeaderValue UserAgent = new("TurtleRoute", typeof(OpenCageGeocoder).Assembly.GetName().Version?.ToString(3));

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

            // Formatted as https://opencagedata.com/guides/how-to-format-your-geocoding-query recommends, e.g. "Katwilgweg 2, 2050, Antwerpen, Belgium".
            // The country goes in by name, as an ISO code in the query is ambiguous.
            string query = Join(", ", Join(" ", address.Street, address.StreetNo), address.ZipCode, address.City, address.State, ToCountryName(address.Country));
            if (query.Length < 2)
                return null;

            List<OpenCageResult> results = await SearchAsync(query, address.Country, cancellationToken);

            // A same-named street in another city may rank first, and a postcode area is only the last resort.
            OpenCageResult result = results.Where(r => r.Matches(address)).OrderBy(r => r.Type == "postcode").FirstOrDefault();

            if (result == null)
                return results.Count == 0 ? null : ToResult(results[0], GeocodeConfidence.Low);

            GeocodeConfidence confidence = ToConfidence(result);

            if (!string.IsNullOrWhiteSpace(address.StreetNo) && result.HouseNumber == null && confidence == GeocodeConfidence.High)
                confidence = GeocodeConfidence.Medium;

            return ToResult(result, confidence);
        }

        public async Task<GeocodeResult> GeocodeAsync(string address, string countryFilter, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentNullException(nameof(address));

            if (address.Trim().Length < 2)
                return null;

            // An ISO 3166-1 alpha-2 code restricts the search to that country; anything else is added to the query.
            string text = address.Trim().ReplaceLineEndings(", ");
            string query = ToCountryCode(countryFilter) == null && !string.IsNullOrWhiteSpace(countryFilter) ? $"{text}, {countryFilter}" : text;

            List<OpenCageResult> results = await SearchAsync(query, countryFilter, cancellationToken);

            // OpenCage returns a match even for gibberish.
            OpenCageResult result = results.Where(r => r.Matches(address)).OrderBy(r => r.Type == "postcode").FirstOrDefault();
            if (result == null)
                return results.Count == 0 ? null : ToResult(results[0], GeocodeConfidence.Low);

            return ToResult(result, ToConfidence(result));
        }

        private async Task<List<OpenCageResult>> SearchAsync(string query, string country, CancellationToken cancellationToken)
        {
            Dictionary<string, string> parameters = new()
            {
                ["q"] = query,
                ["key"] = _options.ApiKey,
                ["limit"] = "5", // the right match is not always ranked first
                ["no_annotations"] = "1"
            };

            string countryCode = ToCountryCode(country);
            if (countryCode != null)
                parameters["countrycode"] = countryCode;

            if (!string.IsNullOrWhiteSpace(_options.Language))
                parameters["language"] = _options.Language;

            string queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
            Uri requestUri = new(_options.BaseAddress, $"json?{queryString}");

            using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
            request.Headers.UserAgent.Add(UserAgent);

            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

            // 400 means OpenCage cannot read the query, e.g. a URL: nothing to find, not an outage.
            if (response.StatusCode == HttpStatusCode.BadRequest)
                return [];

            if (!response.IsSuccessStatusCode)
            {
                // 401 invalid key, 402 quota exceeded, 403 key disabled, 429 rate limited, 503 unavailable.
                OpenCageResponse error = await TryReadAsync(response, cancellationToken);
                throw new HttpRequestException($"OpenCage returned {(int)response.StatusCode}: {error?.Status?.Message ?? response.ReasonPhrase}", null, response.StatusCode);
            }

            OpenCageResponse body = await response.Content.ReadFromJsonAsync<OpenCageResponse>(cancellationToken);
            return [.. (body?.Results ?? []).Where(r => r?.Geometry != null)];
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
            => new(new GeoCoordinate(result.Geometry.Lat, result.Geometry.Lng), confidence, ProviderName, result.Formatted);

        // Thresholds are a first estimate and should be tuned against real addresses.
        private static GeocodeConfidence ToConfidence(OpenCageResult result)
        {
            // A postcode area is never the address itself.
            // A UK or Dutch postcode covers a few houses (score 10), a German or Belgian one a whole town (score 7).
            if (result.Type == "postcode")
                return result.Confidence >= 9 ? GeocodeConfidence.Medium : GeocodeConfidence.Low;

            if (result.Type is "partial_postcode" or "terminated_postcode")
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

        // The English name for an ISO code, e.g. "Belgium" for "BE", or null when unknown.
        private static string ToCountryName(string country)
        {
            string code = ToCountryCode(country);
            if (code == null)
                return country;

            try
            {
                return new RegionInfo(code).EnglishName;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }



        private static string Join(string separator, params string[] parts)
            => string.Join(separator, parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }
}
