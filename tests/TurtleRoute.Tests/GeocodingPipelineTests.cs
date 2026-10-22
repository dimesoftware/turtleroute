using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TurtleRoute.Tests
{
    [TestClass]
    public class GeocodingPipelineTests
    {
        private static readonly Address Katwilgweg = new("Katwilgweg", "2", "2050", "Antwerpen", "", "BE");

        [TestMethod]
        public async Task GeocodeAsync_FirstLayerAccepted_ShouldNotCallSecondLayer()
        {
            FakeGeocoder primary = FakeGeocoder.Returning(GeocodeConfidence.High, "primary");
            FakeGeocoder secondary = FakeGeocoder.Returning(GeocodeConfidence.High, "secondary");

            GeocodeResult result = await new GeocodingPipeline([primary, secondary]).GeocodeAsync(Katwilgweg);

            Assert.AreEqual("primary", result.Provider);
            Assert.AreEqual(0, secondary.Calls);
        }

        [TestMethod]
        public async Task GeocodeAsync_FirstLayerThrows_ShouldFallBack()
        {
            FakeGeocoder primary = FakeGeocoder.Throwing(new HttpRequestException("402 quota exceeded"));
            FakeGeocoder secondary = FakeGeocoder.Returning(GeocodeConfidence.High, "secondary");

            GeocodeResult result = await new GeocodingPipeline([primary, secondary]).GeocodeAsync(Katwilgweg);

            Assert.AreEqual("secondary", result.Provider);
        }

        [TestMethod]
        public async Task GeocodeAsync_FirstLayerFindsNothing_ShouldFallBack()
        {
            FakeGeocoder primary = FakeGeocoder.Returning(null);
            FakeGeocoder secondary = FakeGeocoder.Returning(GeocodeConfidence.Medium, "secondary");

            GeocodeResult result = await new GeocodingPipeline([primary, secondary]).GeocodeAsync("Katwilgweg 2, Antwerpen", "BE");

            Assert.AreEqual("secondary", result.Provider);
        }

        [TestMethod]
        public async Task GeocodeAsync_FirstLayerBelowMinimumConfidence_ShouldFallBack()
        {
            FakeGeocoder primary = FakeGeocoder.Returning(GeocodeConfidence.Medium, "primary");
            FakeGeocoder secondary = FakeGeocoder.Returning(GeocodeConfidence.High, "secondary");
            GeocodingPipelineOptions options = new() { MinimumConfidence = GeocodeConfidence.High };

            GeocodeResult result = await new GeocodingPipeline([primary, secondary], options).GeocodeAsync(Katwilgweg);

            Assert.AreEqual("secondary", result.Provider);
        }

        [TestMethod]
        public async Task GeocodeAsync_NoLayerAccepted_ShouldReturnNull()
        {
            FakeGeocoder primary = FakeGeocoder.Returning(GeocodeConfidence.Low, "primary");
            FakeGeocoder secondary = FakeGeocoder.Returning(null);

            GeocodeResult result = await new GeocodingPipeline([primary, secondary]).GeocodeAsync(Katwilgweg);

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GeocodeAsync_OneLayerThrowsOtherFindsNothing_ShouldReturnNull()
        {
            FakeGeocoder primary = FakeGeocoder.Throwing(new HttpRequestException("503"));
            FakeGeocoder secondary = FakeGeocoder.Returning(null);

            GeocodeResult result = await new GeocodingPipeline([primary, secondary]).GeocodeAsync(Katwilgweg);

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GeocodeAsync_AllLayersThrow_ShouldThrowGeocodingException()
        {
            FakeGeocoder primary = FakeGeocoder.Throwing(new HttpRequestException("401"));
            FakeGeocoder secondary = FakeGeocoder.Throwing(new HttpRequestException("503"));

            GeocodingException ex = await Assert.ThrowsAsync<GeocodingException>(() => new GeocodingPipeline([primary, secondary]).GeocodeAsync(Katwilgweg));

            Assert.HasCount(2, ex.InnerExceptions);
        }

        [TestMethod]
        public async Task GeocodeAsync_Cancelled_ShouldNotFallBack()
        {
            using CancellationTokenSource cts = new();
            cts.Cancel();

            FakeGeocoder primary = FakeGeocoder.Throwing(new OperationCanceledException(cts.Token));
            FakeGeocoder secondary = FakeGeocoder.Returning(GeocodeConfidence.High, "secondary");

            await Assert.ThrowsAsync<OperationCanceledException>(() => new GeocodingPipeline([primary, secondary]).GeocodeAsync(Katwilgweg, cts.Token));
            Assert.AreEqual(0, secondary.Calls);
        }

        [TestMethod]
        public void Constructor_NoLayers_ThrowsArgumentException()
            => Assert.Throws<ArgumentException>(() => new GeocodingPipeline([]));
    }
}
