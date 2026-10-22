using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TurtleRoute.OpenCage.Tests
{
    /// <summary>
    /// Offline tests for how OpenCage responses are mapped, using canned JSON instead of the live API.
    /// </summary>
    [TestClass]
    public class OpenCageGeocoderResponseTests
    {
        private static readonly Address Katwilgweg = new("Katwilgweg", "2", "2050", "Antwerpen", "", "BE");

        [TestMethod]
        public async Task GeocodeAsync_HouseLevelMatch_ShouldBeHighConfidence()
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(10, """{"_type":"building","road":"Katwilgweg","house_number":"2"}"""));

            GeocodeResult result = await Create(handler).GeocodeAsync(Katwilgweg);

            Assert.AreEqual(GeocodeConfidence.High, result.Confidence);
            Assert.AreEqual(51.219501, result.Coordinate.Latitude);
            Assert.AreEqual(4.359231, result.Coordinate.Longitude);
            Assert.AreEqual(OpenCageGeocoder.ProviderName, result.Provider);
        }

        [TestMethod]
        public async Task GeocodeAsync_ShouldSendLocalAddressFormatAndCountryCode()
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(10, """{"road":"Katwilgweg","house_number":"2"}"""));

            await Create(handler).GeocodeAsync(Katwilgweg);

            string query = Uri.UnescapeDataString(handler.LastRequest.RequestUri.Query);
            StringAssert.Contains(query, "q=Katwilgweg 2, 2050 Antwerpen, BE");
            StringAssert.Contains(query, "countrycode=be");
        }

        [TestMethod]
        public async Task GeocodeAsync_StreetNotFound_ShouldBeLowConfidence()
        {
            // OpenCage falls back to the postcode when the street does not exist.
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(9, """{"_type":"postcode","postcode":"9000","city":"Gent"}"""));

            GeocodeResult result = await Create(handler).GeocodeAsync(new Address("Eenstraatdienietbestaat", "56", "9000", "Sjakkamakka", "", "BE"));

            Assert.AreEqual(GeocodeConfidence.Low, result.Confidence);
        }

        [TestMethod]
        public async Task GeocodeAsync_FreeformPostcodeFallback_ShouldBeLowConfidence()
        {
            // OpenCage scores a postcode centroid 7 (under 5 km), which would otherwise map to Medium.
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(7, """{"_type":"postcode","postcode":"32120"}"""));

            GeocodeResult result = await Create(handler).GeocodeAsync("Maschstraße - K36, P. Nr. 1718067, 32120, Hiddenhausen", "DE");

            Assert.AreEqual(GeocodeConfidence.Low, result.Confidence);
        }

        [TestMethod]
        public async Task GeocodeAsync_HouseNumberNotFound_ShouldBeMediumConfidence()
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(10, """{"_type":"road","road":"Katwilgweg"}"""));

            GeocodeResult result = await Create(handler).GeocodeAsync(Katwilgweg);

            Assert.AreEqual(GeocodeConfidence.Medium, result.Confidence);
        }

        [TestMethod]
        public async Task GeocodeAsync_NoResults_ShouldReturnNull()
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, """{"results":[],"status":{"code":200,"message":"OK"}}""");

            GeocodeResult result = await Create(handler).GeocodeAsync("Nowhere", "BE");

            Assert.IsNull(result);
        }

        [TestMethod]
        [DataRow(HttpStatusCode.Unauthorized)]
        [DataRow(HttpStatusCode.PaymentRequired)]
        [DataRow(HttpStatusCode.TooManyRequests)]
        [DataRow(HttpStatusCode.ServiceUnavailable)]
        public async Task GeocodeAsync_ErrorStatus_ShouldThrowSoPipelineFallsBack(HttpStatusCode status)
        {
            StubHandler handler = StubHandler.Json(status, """{"results":[],"status":{"code":402,"message":"quota exceeded"}}""");

            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => Create(handler).GeocodeAsync(Katwilgweg));

            Assert.AreEqual(status, ex.StatusCode);
        }

        [TestMethod]
        public async Task GeocodeAsync_FreeformWithCountryName_ShouldAddCountryToQuery()
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(10, "{}"));

            await Create(handler).GeocodeAsync("Katwilgweg 2, Antwerpen", "Belgium");

            string query = Uri.UnescapeDataString(handler.LastRequest.RequestUri.Query);
            StringAssert.Contains(query, "q=Katwilgweg 2, Antwerpen, Belgium");
            Assert.DoesNotContain("countrycode", query);
        }

        [TestMethod]
        public void Constructor_MissingApiKey_ThrowsArgumentNullException()
            => Assert.Throws<ArgumentNullException>(() => new OpenCageGeocoder(new HttpClient(), new OpenCageOptions()));

        private static OpenCageGeocoder Create(StubHandler handler)
            => new(new HttpClient(handler), new OpenCageOptions { ApiKey = "test" });

        private static string Result(int confidence, string components)
            => $$$"""{"results":[{"confidence":{{{confidence}}},"geometry":{"lat":51.219501,"lng":4.359231},"components":{{{components}}}}],"status":{"code":200,"message":"OK"}}""";

        private class StubHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _status;
            private readonly string _body;

            private StubHandler(HttpStatusCode status, string body)
            {
                _status = status;
                _body = body;
            }

            public HttpRequestMessage LastRequest { get; private set; }

            public static StubHandler Json(HttpStatusCode status, string body) => new(status, body);

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                return Task.FromResult(new HttpResponseMessage(_status) { Content = new StringContent(_body, Encoding.UTF8, "application/json") });
            }
        }
    }
}
