using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Core.GeoJson;
using Azure.Maps.Search;
using Azure.Maps.Search.Models;

namespace TurtleRoute
{
    public class ConfidenceComparer : IComparer<ConfidenceEnum?>
    {
        public int Compare(ConfidenceEnum? x, ConfidenceEnum? y)
        {
            if (x == ConfidenceEnum.High && y != ConfidenceEnum.High)
                return -1;

            if (x == ConfidenceEnum.Medium && y == ConfidenceEnum.Low)
                return -1;

            if (x == y)
                return 0;

            return 1;
        }
    }

    public class Geocoder
    {
        public Geocoder(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token));

            Token = token;
        }

        private string Token { get; }

        public async Task<GeoCoordinate?> GeocodeAsync(string street, string streetNo, string zipCode, string city, string state, string country)
        {
            AzureKeyCredential credential = new(Token);
            MapsSearchClient client = new(credential);

            Response<GeocodingResponse> searchResult = await client.GetGeocodingAsync($"{street} {streetNo}, {zipCode} {city}, {state}, {country}");

            GeoPosition resp = searchResult.Value.Features[0].Geometry.Coordinates;
            return new(resp.Latitude, resp.Longitude);
        }

        public async Task<GeoCoordinate?> GeocodeAsync(string address, string countryFilter)
        {
            AzureKeyCredential credential = new(Token);
            MapsSearchClient client = new(credential);

            Response<GeocodingResponse> searchResult = await client.GetGeocodingAsync(address + ", " + countryFilter);

            if (searchResult.Value.Features.Count == 0)
                return new();

            GeoPosition resp = searchResult.Value.Features.OrderBy(x => x.Properties.Confidence, new ConfidenceComparer()).ElementAt(0).Geometry.Coordinates;
            return new(resp.Latitude, resp.Longitude);
        }
    }
}