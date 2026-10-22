using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core.GeoJson;
using Azure.Maps.Search;
using Azure.Maps.Search.Models;

namespace TurtleRoute.Azure
{
    public class AzureGeocoder : IGeocoder
    {
        public const string ProviderName = "AzureMaps";

        private readonly MapsSearchClient _client;

        public AzureGeocoder(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentNullException(nameof(token));

            _client = new MapsSearchClient(new AzureKeyCredential(token));
        }

        public async Task<GeocodeResult> GeocodeAsync(Address address, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(address);

            Response<GeocodingResponse> searchResult = await _client.GetGeocodingAsync(address.ToString(), cancellationToken: cancellationToken);

            IReadOnlyList<FeaturesItem> features = searchResult?.Value?.Features;
            if (features == null || features.Count == 0)
                return null;

            return ToResult(features[0]);
        }

        public async Task<GeocodeResult> GeocodeAsync(string address, string countryFilter, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentNullException(nameof(address));

            string query = string.IsNullOrWhiteSpace(countryFilter) ? address : $"{address}, {countryFilter}";

            Response<GeocodingResponse> searchResult = await _client.GetGeocodingAsync(query, cancellationToken: cancellationToken);

            IReadOnlyList<FeaturesItem> features = searchResult?.Value?.Features;
            if (features == null || features.Count == 0)
                return null;

            return ToResult(features.OrderBy(f => f?.Properties?.Confidence, new ConfidenceComparer()).FirstOrDefault());
        }

        private static GeocodeResult ToResult(FeaturesItem feature)
        {
            GeoPosition? coords = feature?.Geometry?.Coordinates;
            if (!coords.HasValue)
                return null;

            // Azure.Core.GeoJson.GeoPosition exposes coordinate values by index: [0]=longitude, [1]=latitude.
            double longitude = coords.Value[0];
            double latitude = coords.Value[1];

            return new GeocodeResult(new GeoCoordinate(latitude, longitude), ToConfidence(feature.Properties?.Confidence), ProviderName);
        }

        private static GeocodeConfidence ToConfidence(ConfidenceEnum? confidence)
        {
            if (confidence == ConfidenceEnum.High) return GeocodeConfidence.High;
            if (confidence == ConfidenceEnum.Low) return GeocodeConfidence.Low;

            // Medium, and unscored results, which were accepted before the pipeline existed.
            return GeocodeConfidence.Medium;
        }
    }
}
