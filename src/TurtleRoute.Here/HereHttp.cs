using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TurtleRoute.Here
{
    internal static class HereHttp
    {
        public static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (!response.IsSuccessStatusCode)
            {
                // 400 invalid request, 401 invalid key, 403 not entitled, 429 rate limited, 5xx unavailable.
                HereError error = await TryReadAsync<HereError>(response, cancellationToken);
                string message = error?.Cause ?? error?.Title ?? error?.ErrorDescription ?? response.ReasonPhrase;
                throw new HttpRequestException($"HERE returned {(int)response.StatusCode}: {message}", null, response.StatusCode);
            }

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        }

        /// <summary>
        /// Builds a query string; a key may occur more than once, e.g. HERE's repeated "via" parameter.
        /// </summary>
        public static string Query(IEnumerable<KeyValuePair<string, string>> parameters)
            => string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));

        public static string Format(GeoCoordinate coordinate)
            => string.Create(CultureInfo.InvariantCulture, $"{coordinate.Latitude},{coordinate.Longitude}");

        private static async Task<T> TryReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken) where T : class
        {
            try
            {
                return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
            }
            catch (Exception ex) when (ex is JsonException or NotSupportedException)
            {
                return null;
            }
        }
    }
}
