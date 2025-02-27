using System;
using System.Threading.Tasks;
using Azure;
using Azure.Core.GeoJson;
using Azure.Maps.Search;
using Azure.Maps.Search.Models;

namespace TurtleRoute
{
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

            GeoPosition resp = searchResult.Value.Features[0].Geometry.Coordinates;
            return new(resp.Latitude, resp.Longitude);
        }
    }
}