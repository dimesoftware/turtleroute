using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TurtleRoute.Tests;

namespace TurtleRoute.OpenCage.Tests
{
    [TestClass]
    [TestCategory("Integration")]
    public class OpenCageGeocoderTests : GeocoderContractTests
    {
        protected override IGeocoder CreateGeocoder() => new OpenCageGeocoder(new HttpClient(), new OpenCageOptions { ApiKey = TestSettings.OpenCageToken });

        // OpenCage cannot place this address and returns the centre of postcode 32120, about 1.2 km off.
        // It must not be accepted, so a pipeline falls back to the next layer (Azure Maps finds it, see AzureGeocoderTests).
        [TestMethod]
        public async Task Geocoding_GetAddressByText_RuralAddress_ShouldNotBeAccepted()
        {
            GeocodeResult result = await CreateGeocoder().GeocodeAsync("Maschstraße - K36, P. Nr. 1718067, 32120, Hiddenhausen", "DE");

            AssertRejected(result);
        }

        [TestMethod]
        [DataRow("asdfghjkl nowhere 99999", "NL")]
        [DataRow("qqqqzzzz xxxx 0000", "US")]
        public async Task Geocoding_GetAddressByText_Gibberish_ShouldNotBeAccepted(string address, string country)
        {
            GeocodeResult result = await CreateGeocoder().GeocodeAsync(address, country);

            AssertRejected(result);
        }

        // OpenCage ranks a same-named street in another city first for these.
        [TestMethod]
        [DataRow("Broadway 1000, New York", "US", 40.754, -73.987)]
        [DataRow("Kerkstraat 999, Gent", "BE", 51.045, 3.758)]
        [DataRow("Hoofdstraat 9999, Utrecht", "NL", 52.090, 5.121)]
        public async Task Geocoding_GetAddressByText_HouseNumberNotFound_ShouldNotBeAcceptedInAnotherCity(string address, string country, double latitude, double longitude)
        {
            GeocodeResult result = await CreateGeocoder().GeocodeAsync(address, country);

            if (result != null && result.Confidence >= GeocodeConfidence.Medium)
                AssertNear(result, latitude, longitude, 20);
        }

        [TestMethod]
        [DataRow("Avenida Paulista 1000, Sao Paulo", "BR", -23.565, -46.652)]
        [DataRow("Rue Neuve 1, Bruxelles", "BE", 50.851, 4.354)]
        public async Task Geocoding_GetAddressByText_ShouldBeAcceptedInRequestedCity(string address, string country, double latitude, double longitude)
        {
            GeocodeResult result = await CreateGeocoder().GeocodeAsync(address, country);

            AssertAcceptedNear(result, latitude, longitude);
        }

        [TestMethod]
        [DataRow("Avenida Paulista", "1000", "", "Sao Paulo", "BR", -23.565, -46.652)]
        [DataRow("Alexanderplatz", "7", "", "Berlin", "DE", 52.523, 13.413)]
        public async Task Geocoding_GetAddress_ShouldBeAcceptedInRequestedCity(string street, string streetNo, string zipCode, string city, string country, double latitude, double longitude)
        {
            GeocodeResult result = await CreateGeocoder().GeocodeAsync(new Address(street, streetNo, zipCode, city, "", country));

            AssertAcceptedNear(result, latitude, longitude);
        }

        private static void AssertAcceptedNear(GeocodeResult result, double latitude, double longitude)
        {
            Assert.IsNotNull(result);
            Assert.IsGreaterThanOrEqualTo((int)GeocodeConfidence.Medium, (int)result.Confidence, result.ToString());
            AssertNear(result, latitude, longitude, 2);
        }

        private static void AssertNear(GeocodeResult result, double latitude, double longitude, double kilometres)
        {
            double dLat = (result.Coordinate.Latitude - latitude) * Math.PI / 180;
            double dLon = (result.Coordinate.Longitude - longitude) * Math.PI / 180;
            double a = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Cos(latitude * Math.PI / 180) * Math.Cos(result.Coordinate.Latitude * Math.PI / 180) * Math.Pow(Math.Sin(dLon / 2), 2);
            double distance = 6371 * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            Assert.IsLessThanOrEqualTo(kilometres, distance, result.ToString());
        }
    }
}
