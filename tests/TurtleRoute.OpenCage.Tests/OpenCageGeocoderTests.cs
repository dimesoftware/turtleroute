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
    }
}
