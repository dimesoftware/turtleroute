using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TurtleRoute.Tests
{
    /// <summary>
    /// Runs the same live geocoding cases against every provider, so their results can be compared.
    /// Each provider's test project derives from this class.
    /// </summary>
    public abstract class GeocoderContractTests
    {
        protected abstract IGeocoder CreateGeocoder();

        [TestMethod]
        public async Task Geocoding_GetAddress_CountryInISO2_ShouldReturnCorrectCoordinates()
        {
            GeocodeResult result = await CreateGeocoder().GeocodeAsync(new Address("Katwilgweg", "2", "2050", "Antwerpen", "", "BE"));

            AssertAccepted(result, 4.359231, 51.219501);
        }

        [TestMethod]
        public async Task Geocoding_GetAddress_CountryInISO3_ShouldReturnCorrectCoordinates()
        {
            GeocodeResult result = await CreateGeocoder().GeocodeAsync(new Address("Katwilgweg", "2", "2050", "Antwerpen", "", "BEL"));

            AssertAccepted(result, 4.359231, 51.219501);
        }

        [TestMethod]
        public async Task Geocoding_GetAddress_CountryInEnglish_ShouldReturnCorrectCoordinates()
        {
            GeocodeResult result = await CreateGeocoder().GeocodeAsync(new Address("Katwilgweg", "2", "2050", "Antwerpen", "", "Belgium"));

            AssertAccepted(result, 4.359231, 51.219501);
        }

        [TestMethod]
        [DataRow("1 Avenue du Château, 62124, Vélu", "FR", 2.972791, 50.104375)]
        [DataRow("Vélu, 1 Avenue du Château, 62124", "FR", 2.972791, 50.104375)]
        [DataRow("1 Avenue du Château, Vélu, 62124", "FR", 2.972791, 50.104375)]
        public async Task Geocoding_GetAddressByText_ShouldReturnCorrectCoordinates(string address, string country, double x, double y)
        {
            GeocodeResult result = await CreateGeocoder().GeocodeAsync(address, country);

            AssertAccepted(result, x, y);
        }

        [TestMethod]
        [DataRow("Eenstraatdienietbestaat", "56", "9000", "Sjakkamakka", "BE")]
        [DataRow("Heilig Sakramentsraat", "26", "10008", "Zakkemakke", "DE")]
        public async Task GeocodeAsync_WrongAddress_ShouldNotBeAccepted(string street, string streetNo, string zipCode, string city, string country)
        {
            GeocodeResult result = await CreateGeocoder().GeocodeAsync(new Address(street, streetNo, zipCode, city, string.Empty, country));

            AssertRejected(result);
        }

        [TestMethod]
        public async Task GeocodeAsyncRaw_WrongAddress_ShouldNotBeAccepted()
        {
            GeocodeResult result = await CreateGeocoder().GeocodeAsync("Eenstraatdienietbestaat 56, , 9000, Sjakkamakka", "GB");

            AssertRejected(result);
        }

        // Mirrors the default GeocodingPipelineOptions.MinimumConfidence.
        protected static void AssertAccepted(GeocodeResult result, double x, double y)
        {
            Assert.IsNotNull(result);
            Assert.IsGreaterThanOrEqualTo((int)GeocodeConfidence.Medium, (int)result.Confidence, result.ToString());
            Assert.IsLessThanOrEqualTo(1, GetDistance(result.Coordinate.Longitude, result.Coordinate.Latitude, x, y), result.ToString());
        }

        protected static void AssertRejected(GeocodeResult result)
            => Assert.IsTrue(result == null || result.Confidence < GeocodeConfidence.Medium, result?.ToString());

        private static double GetDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const int radius = 6371; // Radius of the earth in km
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double d = radius * c;  // Distance in km
            return d;
        }

        private static double ToRadians(double deg) => deg * (Math.PI / 180);
    }
}
