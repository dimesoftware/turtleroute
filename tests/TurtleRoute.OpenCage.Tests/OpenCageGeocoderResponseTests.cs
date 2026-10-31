using System;
using System.Globalization;
using System.Linq;
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
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(10, """{"_type":"building","road":"Katwilgweg","house_number":"2","city":"Antwerpen","postcode":"2050"}"""));

            GeocodeResult result = await Create(handler).GeocodeAsync(Katwilgweg);

            Assert.AreEqual(GeocodeConfidence.High, result.Confidence);
            Assert.AreEqual(51.219501, result.Coordinate.Latitude);
            Assert.AreEqual(4.359231, result.Coordinate.Longitude);
            Assert.AreEqual(OpenCageGeocoder.ProviderName, result.Provider);
        }

        [TestMethod]
        public async Task GeocodeAsync_ShouldReturnTheMatchedAddress()
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, """{"results":[{"confidence":9,"formatted":"Katwilgweg 2, 2050 Antwerp, Belgium","geometry":{"lat":51.219501,"lng":4.359231},"components":{"road":"Katwilgweg","house_number":"2","city":"Antwerpen"}}],"status":{"code":200,"message":"OK"}}""");

            GeocodeResult result = await Create(handler).GeocodeAsync(Katwilgweg);

            Assert.AreEqual("Katwilgweg 2, 2050 Antwerp, Belgium", result.MatchedAddress);
        }

        [TestMethod]
        public async Task GeocodeAsync_ShouldSendCommaSeparatedPartsAndCountryCode()
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(10, """{"road":"Katwilgweg","house_number":"2","city":"Antwerpen"}"""));

            await Create(handler).GeocodeAsync(Katwilgweg);

            string query = Uri.UnescapeDataString(handler.LastRequest.RequestUri.Query);
            StringAssert.Contains(query, "q=Katwilgweg 2, 2050, Antwerpen, Belgium&");
            StringAssert.Contains(query, "countrycode=be");
            StringAssert.Contains(query, "language=native");
            Assert.AreEqual("TurtleRoute", handler.LastRequest.Headers.UserAgent.Single().Product.Name);
        }

        [TestMethod]
        public async Task GeocodeAsync_HouseNumberUnderAlias_ShouldBeHighConfidence()
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(10, """{"_type":"building","road":"Katwilgweg","street_number":"2","city":"Antwerpen"}"""));

            GeocodeResult result = await Create(handler).GeocodeAsync(Katwilgweg);

            Assert.AreEqual(GeocodeConfidence.High, result.Confidence);
        }

        [TestMethod]
        public async Task GeocodeAsync_SingleCharacter_ShouldNotCallOpenCage()
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(10, "{}"));

            Assert.IsNull(await Create(handler).GeocodeAsync("B", "BE"));
            Assert.IsNull(handler.LastRequest);
        }

        [TestMethod]
        public async Task GeocodeAsync_Placeholders_ShouldBeLeftOut()
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(10, """{"road":"Katwilgweg","house_number":"2","city":"Antwerpen"}"""));

            GeocodeResult result = await Create(handler).GeocodeAsync(new Address("Katwilgweg", "2", "XXXX", "Antwerpen", "undefined", "BE"));

            StringAssert.Contains(Uri.UnescapeDataString(handler.LastRequest.RequestUri.Query), "q=Katwilgweg 2, Antwerpen, Belgium&");
            Assert.AreEqual(GeocodeConfidence.High, result.Confidence);
        }

        [TestMethod]
        public async Task GeocodeAsync_FreeformLineBreaks_ShouldBeCommaSeparated()
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(10, """{"road":"Katwilgweg","house_number":"2","city":"Antwerpen"}"""));

            await Create(handler).GeocodeAsync("Katwilgweg 2\r\n2050 Antwerpen\n", "BE");

            StringAssert.Contains(Uri.UnescapeDataString(handler.LastRequest.RequestUri.Query), "q=Katwilgweg 2, 2050 Antwerpen&");
        }

        [TestMethod]
        public async Task GeocodeAsync_OnlyPlaceholders_ShouldNotCallOpenCage()
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(10, "{}"));

            GeocodeResult result = await Create(handler).GeocodeAsync(new Address("-", "", "", "none", "", "undefined"));

            Assert.IsNull(result);
            Assert.IsNull(handler.LastRequest);
        }

        [TestMethod]
        public async Task GeocodeAsync_SameStreetInAnotherCityRankedFirst_ShouldTakeTheOneInTheRequestedCity()
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Results(
                (10, -22.345, -48.784, """{"_type":"building","road":"Avenida Paulista","city":"Pederneiras"}"""),
                (10, -23.565, -46.652, """{"_type":"building","road":"Avenida Paulista","city":"São Paulo"}""")));

            GeocodeResult result = await Create(handler).GeocodeAsync(new Address("Avenida Paulista", "1000", "", "Sao Paulo", "", "BR"));

            Assert.AreEqual(-23.565, result.Coordinate.Latitude);
            Assert.AreEqual(GeocodeConfidence.Medium, result.Confidence);
        }

        [TestMethod]
        public async Task GeocodeAsync_FreeformSameStreetInAnotherCityRankedFirst_ShouldTakeTheOneInTheRequestedCity()
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Results(
                (9, 51.135, 3.411, """{"_type":"road","road":"Kerkstraat","town":"Aalter","postcode":"9910"}"""),
                (9, 51.021, 3.637, """{"_type":"road","road":"Kerkstraat","village":"Sint-Martens-Latem","postcode":"9830"}"""),
                (9, 51.045, 3.754, """{"_type":"road","road":"Kerkstraat","city":"Gent","postcode":"9050"}""")));

            GeocodeResult result = await Create(handler).GeocodeAsync("Kerkstraat 999, Gent", "BE");

            Assert.AreEqual(51.045, result.Coordinate.Latitude);
            Assert.AreEqual(GeocodeConfidence.High, result.Confidence);
        }

        [TestMethod]
        public async Task GeocodeAsync_OnlyInAnotherCity_ShouldBeLowConfidence()
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(10, """{"_type":"building","road":"Broadway","city":"City of Rochester","state":"New York"}"""));

            GeocodeResult result = await Create(handler).GeocodeAsync(new Address("Broadway", "1000", "", "New York", "", "US"));

            Assert.AreEqual(GeocodeConfidence.Low, result.Confidence);
        }

        [TestMethod]
        [DataRow("asdfghjkl nowhere 99999", "NL", """{"_type":"building","road":"John M. Keynesplein","city":"Amsterdam","postcode":"1066 EP"}""")]
        [DataRow("qqqqzzzz xxxx 0000", "US", """{"_type":"road","road":"County Road 0000","county":"Apache County","postcode":"86544"}""")]
        [DataRow("zzzzzz yyyyyy 000", "FR", """{"_type":"road","road":"Rue d'en Haut","village":"Godenvillers","postcode":"60420"}""")]
        public async Task GeocodeAsync_Gibberish_ShouldBeLowConfidence(string address, string country, string components)
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(10, components));

            GeocodeResult result = await Create(handler).GeocodeAsync(address, country);

            Assert.AreEqual(GeocodeConfidence.Low, result.Confidence);
        }

        [TestMethod]
        public async Task GeocodeAsync_StreetFiledAsSquare_ShouldBeHighConfidence()
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(10, """{"_type":"building","square":"Alexanderplatz","house_number":"7","city":"Berlin","postcode":"10178"}"""));

            GeocodeResult result = await Create(handler).GeocodeAsync(new Address("Alexanderplatz", "7", "", "Berlin", "", "DE"));

            Assert.AreEqual(GeocodeConfidence.High, result.Confidence);
        }

        [TestMethod]
        [DataRow("Rue Neuve 1, Bruxelles", """{"_type":"building","road":"Rue Neuve - Nieuwstraat","house_number":"1","city":"Bruxelles - Brussel","postcode":"1000"}""")]
        [DataRow("Nieuwstraat 1, Brussel", """{"_type":"building","road":"Rue Neuve - Nieuwstraat","house_number":"1","city":"Bruxelles - Brussel","postcode":"1000"}""")]
        public async Task GeocodeAsync_FreeformCityInEitherLanguage_ShouldMatch(string address, string components)
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(10, components));

            GeocodeResult result = await Create(handler).GeocodeAsync(address, "BE");

            Assert.AreEqual(GeocodeConfidence.High, result.Confidence);
        }

        [TestMethod]
        public async Task GeocodeAsync_OtherStreetInTheRequestedCity_ShouldBeLowConfidence()
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(10, """{"_type":"building","road":"Hoofdfrontweg","city":"Antwerpen","postcode":"2660"}"""));

            GeocodeResult result = await Create(handler).GeocodeAsync("Grote Mrkt 1, Antwerpen", "BE");

            Assert.AreEqual(GeocodeConfidence.Low, result.Confidence);
        }

        [TestMethod]
        [DataRow("Suite 400, 1000 Broadway", "", "10001", "New York", """{"_type":"building","road":"Broadway","house_number":"1000","city":"New York","postcode":"10001"}""")]
        [DataRow("Rue Neuve 1 (galerie)", "", "1000", "Bruxelles", """{"_type":"building","road":"Rue Neuve - Nieuwstraat","house_number":"1","city":"Bruxelles - Brussel","postcode":"1000"}""")]
        [DataRow("Duenninghausen", "10", "59269", "Beckum", """{"_type":"building","hamlet":"Dünninghausen","house_number":"10","town":"Beckum","postcode":"59269"}""")]
        [DataRow("Calle de Alcala", "1", "", "Madrid", """{"_type":"building","road":"Calle de Alcalá","house_number":"1","city":"Madrid"}""")]
        [DataRow("Sint-Bernardsesteenweg", "1", "2660", "Wilrijk", """{"_type":"building","road":"Sint-Bernardsesteenweg","house_number":"1","city":"Antwerpen","postcode":"2660"}""")]
        [DataRow("Damrak", "1", "1012", "Amsterdam", """{"_type":"building","road":"Damrak","house_number":"1","city":"Amsterdam","postcode":"1012 LG"}""")]
        [DataRow("Kerkstraat", "1", "", "", """{"_type":"building","road":"Kerkstraat","house_number":"1","city":"Gent"}""")]
        public async Task GeocodeAsync_SameAddress_ShouldMatch(string street, string streetNo, string zipCode, string city, string components)
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(10, components));

            GeocodeResult result = await Create(handler).GeocodeAsync(new Address(street, streetNo, zipCode, city, "", "BE"));

            Assert.AreEqual(GeocodeConfidence.High, result.Confidence);
        }

        // Only accents, case and punctuation are ignored, so these fall back to the next layer.
        [TestMethod]
        [DataRow("Av. Paulista", "1", "", "São Paulo", """{"_type":"building","road":"Avenida Paulista","house_number":"1","city":"São Paulo"}""")]
        [DataRow("Main St", "1", "", "Springfield", """{"_type":"building","road":"Main Street","house_number":"1","city":"Springfield"}""")]
        [DataRow("Avenue du Château", "1", "", "Vélu", """{"_type":"building","road":"Rue du Château","house_number":"1","village":"Vélu"}""")]
        [DataRow("Kerkstraat", "1", "9000", "Gent", """{"_type":"building","road":"Kerkstraat","house_number":"1","city":"Gent","postcode":"9050"}""")]
        [DataRow("Ermou", "10", "", "Athina", """{"_type":"building","road":"Ερμού","house_number":"10","city":"Αθήνα"}""")]
        public async Task GeocodeAsync_WrittenDifferently_ShouldBeLowConfidence(string street, string streetNo, string zipCode, string city, string components)
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(10, components));

            GeocodeResult result = await Create(handler).GeocodeAsync(new Address(street, streetNo, zipCode, city, "", "BE"));

            Assert.AreEqual(GeocodeConfidence.Low, result.Confidence);
        }

        [TestMethod]
        [DataRow("Genk", "", """{"_type":"building","road":"Kerkstraat","house_number":"1","city":"Gent"}""")]
        [DataRow("Mechelen", "", """{"_type":"building","road":"Kerkstraat","house_number":"1","village":"Mechelen-aan-de-Maas"}""")]
        [DataRow("Asse", "1730", """{"_type":"building","road":"Kerkstraat","house_number":"1","town":"Assenede","postcode":"9960"}""")]
        [DataRow("Gent", "9000", """{"_type":"building","road":"Kerkstraat","house_number":"1","village":"Gentinnes","postcode":"1450"}""")]
        [DataRow("Tournai", "7500", """{"_type":"building","road":"Kerkstraat","house_number":"1","city":"Paris","postcode":"75008"}""")]
        [DataRow("Springfield", "62701", """{"_type":"building","road":"Kerkstraat","house_number":"1","city":"Springfield","postcode":"01103"}""")]
        public async Task GeocodeAsync_OtherCityOrPostcode_ShouldBeLowConfidence(string city, string zipCode, string components)
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(10, components));

            GeocodeResult result = await Create(handler).GeocodeAsync(new Address("Kerkstraat", "1", zipCode, city, "", "BE"));

            Assert.AreEqual(GeocodeConfidence.Low, result.Confidence);
        }

        [TestMethod]
        [DataRow("Grote Markt", """{"_type":"building","road":"Grote Steenweg","house_number":"1","city":"Antwerpen"}""")]
        [DataRow("Sint-Jansstraat", """{"_type":"building","road":"Sint-Pietersstraat","house_number":"1","city":"Antwerpen"}""")]
        [DataRow("Broadway", """{"_type":"building","road":"5th Avenue","house_number":"1","city":"Antwerpen"}""")]
        [DataRow("Kerkstraat", """{"_type":"neighbourhood","residential":"Kerkstraat","city":"Antwerpen"}""")]
        public async Task GeocodeAsync_OtherStreet_ShouldBeLowConfidence(string street, string components)
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(10, components));

            GeocodeResult result = await Create(handler).GeocodeAsync(new Address(street, "1", "", "Antwerpen", "", "BE"));

            Assert.AreEqual(GeocodeConfidence.Low, result.Confidence);
        }

        [TestMethod]
        public async Task GeocodeAsync_StreetlessResultRankedFirst_ShouldTakeTheStreet()
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Results(
                (7, 51.217, 4.417, """{"_type":"postcode","postcode":"2050"}"""),
                (10, 51.219, 4.359, """{"_type":"building","road":"Katwilgweg","house_number":"2","city":"Antwerpen","postcode":"2050"}""")));

            GeocodeResult result = await Create(handler).GeocodeAsync(Katwilgweg);

            Assert.AreEqual(51.219, result.Coordinate.Latitude);
            Assert.AreEqual(GeocodeConfidence.High, result.Confidence);
        }

        [TestMethod]
        [DataRow("partial_postcode")]
        [DataRow("terminated_postcode")]
        public async Task GeocodeAsync_PostcodeCentroid_ShouldBeLowConfidence(string type)
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(9, $$"""{"_type":"{{type}}","postcode":"2050","city":"Antwerpen"}"""));

            GeocodeResult result = await Create(handler).GeocodeAsync("Antwerpen", "BE");

            Assert.AreEqual(GeocodeConfidence.Low, result.Confidence);
        }

        [TestMethod]
        [DataRow("Kerkstraat 1000, Gent", """{"_type":"building","road":"Kerkstraat","house_number":"1000","city":"Bruxelles - Brussel","postcode":"1000"}""")]
        [DataRow("Main St 1000, Gent", """{"_type":"building","road":"Main Street","house_number":"1000","city":"Bruxelles - Brussel","postcode":"1000"}""")]
        [DataRow("10001 Main Street, Houston", """{"_type":"building","road":"Main Street","house_number":"10001","city":"New York","postcode":"10001"}""")]
        [DataRow("Avenida Paulista 1000, Sao Paulo", """{"_type":"town","town":"Paulista","state":"Pernambuco"}""")]
        [DataRow("Bogusstraat 12, Antwerpen", """{"_type":"city","city":"Antwerpen"}""")]
        [DataRow("Rue Neuve 1, Bruxelles", """{"_type":"building","road":"Rue de Bruxelles","house_number":"1","city":"Namur"}""")]
        public async Task GeocodeAsync_FreeformElsewhere_ShouldBeLowConfidence(string address, string components)
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(10, components));

            GeocodeResult result = await Create(handler).GeocodeAsync(address, "BE");

            Assert.AreEqual(GeocodeConfidence.Low, result.Confidence);
        }

        [TestMethod]
        [DataRow("Antwerpen, België", """{"_type":"city","city":"Antwerpen","country":"België / Belgique / Belgien"}""")]
        [DataRow("Damrak 1, 1012 LG", """{"_type":"building","road":"Damrak","house_number":"1","city":"Amsterdam","postcode":"1012 LG"}""")]
        [DataRow("1 Macquarie Street, Sydney NSW", """{"_type":"building","road":"Macquarie Street","house_number":"1","city":"Sydney","state_code":"NSW"}""")]
        public async Task GeocodeAsync_FreeformInRequestedPlace_ShouldBeHighConfidence(string address, string components)
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(10, components));

            GeocodeResult result = await Create(handler).GeocodeAsync(address, "BE");

            Assert.AreEqual(GeocodeConfidence.High, result.Confidence);
        }

        [TestMethod]
        public async Task GeocodeAsync_PreciseUkPostcode_ShouldBeMediumConfidence()
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(10, """{"_type":"postcode","postcode":"SW20 8LD","city":"London"}"""));

            GeocodeResult structured = await Create(handler).GeocodeAsync(new Address("Quinton Road", "1", "SW20 8LD", "Wimbledon", "", "GB"));
            GeocodeResult freeform = await Create(handler).GeocodeAsync("1 Quinton Road, Wimbledon, SW208LD, Wimbledon", "GB");

            Assert.AreEqual(GeocodeConfidence.Medium, structured.Confidence);
            Assert.AreEqual(GeocodeConfidence.Medium, freeform.Confidence);
        }

        [TestMethod]
        [DataRow("Rethemer Weg, 31867, Hülsede", """{"_type":"postcode","postcode":"31867"}""", 7)]
        [DataRow("Mansfield Road, , S44 5GA, Bramley Vale", """{"_type":"building","road":"Mansfield Road","house_number":"3","village":"Doe Lea","postcode":"S44 5PN"}""", 10)]
        [DataRow("Long Lane, EX5 2LL, Exeter", """{"_type":"postcode","postcode":"EX5 2LP","city":"Exeter"}""", 10)]
        public async Task GeocodeAsync_PostcodeAreaOrOtherPostcode_ShouldBeLowConfidence(string address, string components, int confidence)
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(confidence, components));

            GeocodeResult result = await Create(handler).GeocodeAsync(address, "GB");

            Assert.AreEqual(GeocodeConfidence.Low, result.Confidence);
        }

        [TestMethod]
        [DataRow("Moststrasse 14, , 90762, Fuerth", """{"_type":"building","road":"Moststraße","house_number":"14","city":"Fürth","postcode":"90762"}""")]
        [DataRow("Duenninghausen 10, 59269 Beckum", """{"_type":"building","hamlet":"Dünninghausen","house_number":"10","_normalized_city":"Beckum","town":"Beckum","postcode":"59269"}""")]
        [DataRow("Sophienstraße 1, Baden-Baden", """{"_type":"building","road":"Sophienstraße","house_number":"1","city":"Baden-Baden"}""")]
        public async Task GeocodeAsync_GermanSpellings_ShouldMatch(string address, string components)
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(10, components));

            GeocodeResult result = await Create(handler).GeocodeAsync(address, "DE");

            Assert.AreEqual(GeocodeConfidence.High, result.Confidence);
        }

        [TestMethod]
        public async Task GeocodeAsync_UnreadableQuery_ShouldReturnNull()
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.BadRequest, """{"results":[],"status":{"code":400,"message":"missing or bad query"}}""");

            Assert.IsNull(await Create(handler).GeocodeAsync("https://maps.app.goo.gl/ezCyhvFvMDfwQDnU8", "DE"));
        }

        [TestMethod]
        public async Task GeocodeAsync_InvalidCharacters_ShouldNotThrow()
        {
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(10, """{"_type":"building","road":"Katwilgweg","house_number":"2","city":"Antwerpen"}"""));

            GeocodeResult result = await Create(handler).GeocodeAsync("Katwilgweg 2\uD800, Antwerpen", "BE");

            Assert.AreEqual(GeocodeConfidence.High, result.Confidence);
        }

        [TestMethod]
        public async Task GeocodeAsync_StreetNotFound_ShouldBeLowConfidence()
        {
            // OpenCage falls back to the postcode when the street does not exist.
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(7, """{"_type":"postcode","postcode":"9000","city":"Gent"}"""));

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
            StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Result(10, """{"_type":"road","road":"Katwilgweg","city":"Antwerpen"}"""));

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

        private static string Results(params (int Confidence, double Lat, double Lng, string Components)[] results)
            => $$$"""{"results":[{{{string.Join(",", results.Select(r => $$$"""{"confidence":{{{r.Confidence}}},"geometry":{"lat":{{{r.Lat.ToString(CultureInfo.InvariantCulture)}}},"lng":{{{r.Lng.ToString(CultureInfo.InvariantCulture)}}}},"components":{{{r.Components}}}}"""))}}}],"status":{"code":200,"message":"OK"}}""";

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
