using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TurtleRoute.Tests;

namespace TurtleRoute.Azure.Tests
{
    [TestClass]
    [TestCategory("Integration")]
    public class AzureGeocoderTests : GeocoderContractTests
    {
        protected override IGeocoder CreateGeocoder() => new AzureGeocoder(TestSettings.AzureMapsToken);

        // OpenCage only finds the postcode for this address, see OpenCageGeocoderTests.
        [TestMethod]
        public async Task Geocoding_GetAddressByText_RuralAddress_ShouldReturnCorrectCoordinates()
        {
            GeocodeResult result = await CreateGeocoder().GeocodeAsync("Maschstraße - K36, P. Nr. 1718067, 32120, Hiddenhausen", "DE");

            AssertAccepted(result, 8.615481, 52.169262);
        }

        [TestMethod]
        public void Geocoding_Constructor_InvalidParameter_Token_ThrowsArgumentNullException()
            => Assert.Throws<ArgumentNullException>(() => new AzureGeocoder(string.Empty));
    }
}
