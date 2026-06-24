using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TurtleRoute.Tests
{
    [TestClass]
    public class GeocoderTests
    {
        private string _token;

        [TestInitialize]
        public void Setup()
        {
            IConfigurationRoot settings = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile("appsettings.development.json", true)
                .AddJsonFile("appsettings.test.json", true)
                .Build();

            foreach ((string key, string value) in settings.AsEnumerable())
            {
                switch (key)
                {
                    case "token":
                        _token = value;
                        break;

                    default:
                        continue;
                }
            }
        }

        // ----------------------------------------------------------------------
        // Constructor / argument validation
        // ----------------------------------------------------------------------

        [TestMethod]
        public void Constructor_NullToken_ThrowsArgumentNullException()
            => Assert.Throws<ArgumentNullException>(() => new Geocoder(null));

        [TestMethod]
        public void Constructor_EmptyToken_ThrowsArgumentNullException()
            => Assert.Throws<ArgumentNullException>(() => new Geocoder(string.Empty));

        [TestMethod]
        public void Constructor_WhitespaceToken_ThrowsArgumentNullException()
            => Assert.Throws<ArgumentNullException>(() => new Geocoder("   "));

        [TestMethod]
        public async Task GeocodeAsync_Structured_AllPartsBlank_ThrowsArgumentException()
        {
            Geocoder api = new(_token);
            await Assert.ThrowsExactlyAsync<ArgumentException>(
                () => api.GeocodeAsync("", "", "", "", "", ""));
        }

        [TestMethod]
        public async Task GeocodeAsync_Structured_OnlyZip_ThrowsArgumentException()
        {
            Geocoder api = new(_token);
            await Assert.ThrowsExactlyAsync<ArgumentException>(
                () => api.GeocodeAsync("", "", "2000", "", "", ""));
        }

        [TestMethod]
        public async Task GeocodeAsync_Freeform_NullAddress_ThrowsArgumentNullException()
        {
            Geocoder api = new(_token);
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                () => api.GeocodeAsync(null, "BE"));
        }

        [TestMethod]
        public async Task GeocodeAsync_Freeform_EmptyAddress_ThrowsArgumentNullException()
        {
            Geocoder api = new(_token);
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                () => api.GeocodeAsync(string.Empty, "BE"));
        }

        [TestMethod]
        public async Task GeocodeAsync_Freeform_WhitespaceAddress_ThrowsArgumentNullException()
        {
            Geocoder api = new(_token);
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                () => api.GeocodeAsync("   ", "BE"));
        }

        [TestMethod]
        public async Task GeocodeRawAsync_Structured_AllPartsBlank_ThrowsArgumentException()
        {
            Geocoder api = new(_token);
            await Assert.ThrowsExactlyAsync<ArgumentException>(
                () => api.GeocodeRawAsync("", "", "", "", "", ""));
        }

        [TestMethod]
        public async Task GeocodeRawAsync_Freeform_NullAddress_ThrowsArgumentNullException()
        {
            Geocoder api = new(_token);
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                () => api.GeocodeRawAsync(null, "BE"));
        }

        [TestMethod]
        public async Task GeocodeRawAsync_Freeform_EmptyAddress_ThrowsArgumentNullException()
        {
            Geocoder api = new(_token);
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                () => api.GeocodeRawAsync(string.Empty, "BE"));
        }

        // ----------------------------------------------------------------------
        // Structured GeocodeAsync — known-good addresses across countries
        // ----------------------------------------------------------------------

        [TestMethod]
        public async Task GeocodeAsync_Structured_KatwilgwegBE_ISO2_ReturnsCorrectCoordinates()
        {
            Geocoder api = new(_token);
            GeoCoordinate? address = await api.GeocodeAsync("Katwilgweg", "2", "2050", "Antwerpen", "", "BE");

            Assert.IsNotNull(address);
            AssertCoordinates(address.GetValueOrDefault(), 4.359231, 51.219501);
        }

        [TestMethod]
        public async Task GeocodeAsync_Structured_KatwilgwegBE_ISO3_ReturnsCorrectCoordinates()
        {
            Geocoder api = new(_token);
            GeoCoordinate? address = await api.GeocodeAsync("Katwilgweg", "2", "2050", "Antwerpen", "", "BEL");

            Assert.IsNotNull(address);
            AssertCoordinates(address.GetValueOrDefault(), 4.359231, 51.219501);
        }

        [TestMethod]
        public async Task GeocodeAsync_Structured_KatwilgwegBE_EnglishCountryName_ReturnsCorrectCoordinates()
        {
            Geocoder api = new(_token);
            GeoCoordinate? address = await api.GeocodeAsync("Katwilgweg", "2", "2050", "Antwerpen", "", "Belgium");

            Assert.IsNotNull(address);
            AssertCoordinates(address.GetValueOrDefault(), 4.359231, 51.219501);
        }

        [TestMethod]
        [DataRow("Pariser Platz", "1", "10117", "Berlin", "DE", 13.3777, 52.5163)]
        [DataRow("Damrak", "1", "1012LG", "Amsterdam", "NL", 4.8952, 52.3759)]
        [DataRow("Avenue Anatole France", "5", "75007", "Paris", "FR", 2.2945, 48.8584)]
        [DataRow("Bridge Street", "1", "SW1A 0AA", "London", "GB", -0.1247, 51.5007)]
        [DataRow("Plaza Mayor", "1", "28012", "Madrid", "ES", -3.7074, 40.4155)]
        public async Task GeocodeAsync_Structured_LandmarkAddresses_ReturnCorrectCoordinates(
            string street, string streetNo, string zip, string city, string country, double lon, double lat)
        {
            Geocoder api = new(_token);
            GeoCoordinate? address = await api.GeocodeAsync(street, streetNo, zip, city, string.Empty, country);

            Assert.IsNotNull(address, $"Expected coordinates for {street} {streetNo}, {city} {country}");
            AssertCoordinates(address.GetValueOrDefault(), lon, lat, toleranceKm: 2);
        }

        [TestMethod]
        public async Task GeocodeAsync_Structured_CityAndCountryOnly_ReturnsCityCentroid()
        {
            Geocoder api = new(_token);
            GeoCoordinate? address = await api.GeocodeAsync(string.Empty, string.Empty, string.Empty, "Brussels", string.Empty, "BE");

            Assert.IsNotNull(address);
            AssertCoordinates(address.GetValueOrDefault(), 4.3517, 50.8503, toleranceKm: 10);
        }

        [TestMethod]
        public async Task GeocodeAsync_Structured_MixedCaseStreetAndCity_ReturnsCoordinates()
        {
            Geocoder api = new(_token);
            GeoCoordinate? address = await api.GeocodeAsync("kATwilGweg", "2", "2050", "aNTwerpEN", "", "BE");

            Assert.IsNotNull(address);
            AssertCoordinates(address.GetValueOrDefault(), 4.359231, 51.219501);
        }

        [TestMethod]
        public async Task GeocodeAsync_Structured_ExtraWhitespace_ReturnsCoordinates()
        {
            Geocoder api = new(_token);
            GeoCoordinate? address = await api.GeocodeAsync("  Katwilgweg ", " 2 ", " 2050 ", "  Antwerpen  ", "", "BE");

            Assert.IsNotNull(address);
            AssertCoordinates(address.GetValueOrDefault(), 4.359231, 51.219501);
        }

        // ----------------------------------------------------------------------
        // Structured GeocodeAsync — addresses that should NOT resolve
        // ----------------------------------------------------------------------

        [TestMethod]
        [DataRow("Eenstraatdienietbestaat", "56", "9000", "Sjakkamakka", "BE")]
        [DataRow("Heilig Sakramentsraat", "26", "10008", "Zakkemakke", "DE")]
        [DataRow("Notarealstreet", "999", "00000", "Notarealcity", "GB")]
        [DataRow("Calle Inventada", "1", "00000", "Ciudadinexistente", "ES")]
        public async Task GeocodeAsync_Structured_BogusAddress_ReturnsNull(
            string street, string streetNo, string zip, string city, string country)
        {
            Geocoder api = new(_token);
            GeoCoordinate? address = await api.GeocodeAsync(street, streetNo, zip, city, string.Empty, country);

            Assert.IsNull(address, $"Expected null for bogus address {street}, {city} {country}");
        }

        // ----------------------------------------------------------------------
        // Freeform GeocodeAsync — known good and bad
        // ----------------------------------------------------------------------

        [TestMethod]
        [DataRow("Maschstrasse - K36, P. Nr. 1718067, 32120, Hiddenhausen", "DE", 8.615481, 52.169262)]
        [DataRow("Katwilgweg 2, 2050, Antwerpen", "BE", 4.359231, 51.219501)]
        [DataRow("Damrak 1, 1012LG Amsterdam", "NL", 4.8952, 52.3759)]
        [DataRow("Plaza Mayor, 28012 Madrid", "ES", -3.7074, 40.4155)]
        public async Task GeocodeAsync_Freeform_KnownAddress_ReturnsCorrectCoordinates(string address, string country, double lon, double lat)
        {
            Geocoder api = new(_token);
            GeoCoordinate? coord = await api.GeocodeAsync(address, country);

            Assert.IsNotNull(coord, $"Expected coordinates for '{address}' ({country})");
            AssertCoordinates(coord.GetValueOrDefault(), lon, lat, toleranceKm: 2);
        }

        [TestMethod]
        public async Task GeocodeAsync_Freeform_WithoutCountryFilter_ReturnsCoordinates()
        {
            Geocoder api = new(_token);
            GeoCoordinate? coord = await api.GeocodeAsync("Katwilgweg 2, 2050 Antwerpen, Belgium", null);

            Assert.IsNotNull(coord);
            AssertCoordinates(coord.GetValueOrDefault(), 4.359231, 51.219501);
        }

        [TestMethod]
        public async Task GeocodeAsync_Freeform_EmptyCountryFilter_StillResolves()
        {
            Geocoder api = new(_token);
            GeoCoordinate? coord = await api.GeocodeAsync("Katwilgweg 2, 2050 Antwerpen, Belgium", string.Empty);

            Assert.IsNotNull(coord);
            AssertCoordinates(coord.GetValueOrDefault(), 4.359231, 51.219501);
        }

        [TestMethod]
        public async Task GeocodeAsync_Freeform_BogusAddress_ReturnsNull()
        {
            Geocoder api = new(_token);
            GeoCoordinate? coord = await api.GeocodeAsync("Eenstraatdienietbestaat 56, 9000 Sjakkamakka", "BE");

            Assert.IsNull(coord);
        }

        [TestMethod]
        public async Task GeocodeAsync_Freeform_BogusAddressNoCountry_ReturnsNull()
        {
            Geocoder api = new(_token);
            GeoCoordinate? coord = await api.GeocodeAsync("xyzqwerty12345 nonexistent", null);

            Assert.IsNull(coord);
        }

        // ----------------------------------------------------------------------
        // GeocodeRawAsync — shape and content
        // ----------------------------------------------------------------------

        [TestMethod]
        public async Task GeocodeRawAsync_Structured_KnownAddress_ReturnsHighOrMediumConfidence()
        {
            Geocoder api = new(_token);
            GeocodingRawResult result = await api.GeocodeRawAsync("Katwilgweg", "2", "2050", "Antwerpen", "", "BE");

            Assert.IsNotNull(result);
            Assert.IsGreaterThan(0, result.FeatureCount, "Expected at least one feature");
            Assert.IsNotNull(result.BestConfidence, "Expected BestConfidence to be populated");
            Assert.IsTrue(result.BestConfidence is "High" or "Medium",
                $"Expected High or Medium, got {result.BestConfidence}");
            Assert.IsNotNull(result.BestCoordinate);
            AssertCoordinates(result.BestCoordinate.Value, 4.359231, 51.219501);
        }

        [TestMethod]
        public async Task GeocodeRawAsync_Freeform_KnownAddress_ReturnsHighOrMediumConfidence()
        {
            Geocoder api = new(_token);
            GeocodingRawResult result = await api.GeocodeRawAsync("Katwilgweg 2, 2050 Antwerpen", "BE");

            Assert.IsNotNull(result);
            Assert.IsGreaterThan(0, result.FeatureCount);
            Assert.IsTrue(result.BestConfidence is "High" or "Medium",
                $"Expected High or Medium, got {result.BestConfidence}");
            Assert.IsNotNull(result.BestCoordinate);
            AssertCoordinates(result.BestCoordinate.Value, 4.359231, 51.219501);
        }

        [TestMethod]
        public async Task GeocodeRawAsync_Structured_BogusAddress_ReturnsLowOrZeroFeatures()
        {
            Geocoder api = new(_token);
            GeocodingRawResult result = await api.GeocodeRawAsync("Eenstraatdienietbestaat", "56", "9000", "Sjakkamakka", "", "BE");

            Assert.IsNotNull(result);
            bool acceptable = result.FeatureCount == 0
                || result.BestConfidence is null
                || result.BestConfidence == "Low";
            Assert.IsTrue(acceptable,
                $"Bogus address should yield zero features or Low/null confidence, got count={result.FeatureCount} conf={result.BestConfidence}");
        }

        [TestMethod]
        public async Task GeocodeRawAsync_Freeform_BogusAddress_ReturnsLowOrZeroFeatures()
        {
            Geocoder api = new(_token);
            GeocodingRawResult result = await api.GeocodeRawAsync("Eenstraatdienietbestaat 56, 9000 Sjakkamakka", "BE");

            Assert.IsNotNull(result);
            bool acceptable = result.FeatureCount == 0
                || result.BestConfidence is null
                || result.BestConfidence == "Low";
            Assert.IsTrue(acceptable,
                $"Bogus address should yield zero features or Low/null confidence, got count={result.FeatureCount} conf={result.BestConfidence}");
        }

        [TestMethod]
        public async Task GeocodeRawAsync_Structured_KnownAddress_RawJsonContainsFeatures()
        {
            Geocoder api = new(_token);
            GeocodingRawResult result = await api.GeocodeRawAsync("Katwilgweg", "2", "2050", "Antwerpen", "", "BE");

            Assert.IsNotNull(result);
            Assert.AreNotEqual(System.Text.Json.JsonValueKind.Undefined,
result.Raw.ValueKind, "Expected Raw JSON to be populated");
        }

        // ----------------------------------------------------------------------
        // Cross-overload consistency — same input should agree between typed and raw,
        // and between structured and freeform forms of the same address.
        // ----------------------------------------------------------------------

        [TestMethod]
        public async Task TypedAndRaw_StructuredSameAddress_ReturnConsistentCoordinates()
        {
            Geocoder api = new(_token);
            GeoCoordinate? typed = await api.GeocodeAsync("Katwilgweg", "2", "2050", "Antwerpen", "", "BE");
            GeocodingRawResult raw = await api.GeocodeRawAsync("Katwilgweg", "2", "2050", "Antwerpen", "", "BE");

            Assert.IsNotNull(typed);
            Assert.IsNotNull(raw);
            Assert.IsNotNull(raw.BestCoordinate);
            AssertCoordinates(typed.Value, raw.BestCoordinate.Value.Longitude, raw.BestCoordinate.Value.Latitude, toleranceKm: 0.1);
        }

        [TestMethod]
        public async Task TypedAndRaw_FreeformSameAddress_ReturnConsistentCoordinates()
        {
            Geocoder api = new(_token);
            GeoCoordinate? typed = await api.GeocodeAsync("Katwilgweg 2, 2050 Antwerpen", "BE");
            GeocodingRawResult raw = await api.GeocodeRawAsync("Katwilgweg 2, 2050 Antwerpen", "BE");

            Assert.IsNotNull(typed);
            Assert.IsNotNull(raw);
            Assert.IsNotNull(raw.BestCoordinate);
            AssertCoordinates(typed.Value, raw.BestCoordinate.Value.Longitude, raw.BestCoordinate.Value.Latitude, toleranceKm: 0.1);
        }

        [TestMethod]
        public async Task StructuredAndFreeform_SameAddress_ReturnCoordinatesWithinTolerance()
        {
            Geocoder api = new(_token);
            GeoCoordinate? structured = await api.GeocodeAsync("Katwilgweg", "2", "2050", "Antwerpen", "", "BE");
            GeoCoordinate? freeform = await api.GeocodeAsync("Katwilgweg 2, 2050 Antwerpen", "BE");

            Assert.IsNotNull(structured);
            Assert.IsNotNull(freeform);
            AssertCoordinates(structured.Value, freeform.Value.Longitude, freeform.Value.Latitude, toleranceKm: 0.5);
        }

        [TestMethod]
        public async Task TypedAndRaw_BogusAddress_BothIndicateFailure()
        {
            Geocoder api = new(_token);
            GeoCoordinate? typed = await api.GeocodeAsync("Eenstraatdienietbestaat", "56", "9000", "Sjakkamakka", "", "BE");
            GeocodingRawResult raw = await api.GeocodeRawAsync("Eenstraatdienietbestaat", "56", "9000", "Sjakkamakka", "", "BE");

            Assert.IsNull(typed, "Typed overload must return null for bogus address");
            Assert.IsNotNull(raw);
            bool rawAcceptable = raw.FeatureCount == 0
                || raw.BestConfidence is null
                || raw.BestConfidence == "Low";
            Assert.IsTrue(rawAcceptable,
                "Raw overload must report low quality for bogus address");
        }

        // ----------------------------------------------------------------------
        // Ranking — confirms SelectBest does not blindly take features[0].
        // We can't deterministically force Azure to return Low at index 0, but we
        // can assert: whenever a typed call returns non-null coords, the raw
        // BestConfidence must NOT be Low (proves the ranking + Low-reject pair).
        // ----------------------------------------------------------------------

        [TestMethod]
        public async Task TypedReturnsCoord_RawBestConfidenceIsNeverLow_Structured()
        {
            Geocoder api = new(_token);
            GeoCoordinate? typed = await api.GeocodeAsync("Katwilgweg", "2", "2050", "Antwerpen", "", "BE");
            GeocodingRawResult raw = await api.GeocodeRawAsync("Katwilgweg", "2", "2050", "Antwerpen", "", "BE");

            Assert.IsNotNull(typed);
            Assert.IsNotNull(raw);
            Assert.AreNotEqual("Low", raw.BestConfidence,
                "If typed returns coords, raw BestConfidence must not be Low");
        }

        [TestMethod]
        public async Task TypedReturnsCoord_RawBestConfidenceIsNeverLow_Freeform()
        {
            Geocoder api = new(_token);
            GeoCoordinate? typed = await api.GeocodeAsync("Katwilgweg 2, 2050 Antwerpen", "BE");
            GeocodingRawResult raw = await api.GeocodeRawAsync("Katwilgweg 2, 2050 Antwerpen", "BE");

            Assert.IsNotNull(typed);
            Assert.IsNotNull(raw);
            Assert.AreNotEqual("Low", raw.BestConfidence,
                "If typed returns coords, raw BestConfidence must not be Low");
        }

        // ----------------------------------------------------------------------
        // Distance helpers
        // ----------------------------------------------------------------------

        private static void AssertCoordinates(GeoCoordinate address, double expectedLon, double expectedLat, double toleranceKm = 1)
        {
            double distance = GetDistance(address.Longitude, address.Latitude, expectedLon, expectedLat);
            Assert.IsLessThanOrEqualTo(toleranceKm,
distance, $"Expected within {toleranceKm}km of ({expectedLon}, {expectedLat}) but was ({address.Longitude}, {address.Latitude}) — distance {distance:F2}km");
        }

        private static double GetDistance(double lon1, double lat1, double lon2, double lat2)
        {
            const int radius = 6371;
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return radius * c;
        }

        private static double ToRadians(double deg) => deg * (Math.PI / 180);
    }
}
