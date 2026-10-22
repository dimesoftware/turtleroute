using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TurtleRoute.Tests
{
    [TestClass]
    public class ServiceCollectionTests
    {
        [TestMethod]
        public void AddTurtleRoute_ShouldResolvePipelineWithLayersInRegistrationOrder()
        {
            ServiceCollection services = new();
            services.AddTurtleRoute()
                .AddGeocoder<FakeGeocoder>()
                .AddGeocoder<OtherFakeGeocoder>();

            using ServiceProvider provider = services.BuildServiceProvider();

            IGeocoder geocoder = provider.GetRequiredService<IGeocoder>();
            Assert.IsInstanceOfType<GeocodingPipeline>(geocoder);

            GeocodingPipeline pipeline = (GeocodingPipeline)geocoder;
            Assert.HasCount(2, pipeline.Layers);
            Assert.AreEqual(typeof(FakeGeocoder), pipeline.Layers[0].GetType());
            Assert.AreEqual(typeof(OtherFakeGeocoder), pipeline.Layers[1].GetType());
        }

        [TestMethod]
        public void AddTurtleRoute_CalledTwice_ShouldShareLayers()
        {
            ServiceCollection services = new();
            services.AddTurtleRoute().AddGeocoder<FakeGeocoder>();
            services.AddTurtleRoute(builder => builder.AddGeocoder<OtherFakeGeocoder>());

            using ServiceProvider provider = services.BuildServiceProvider();

            Assert.HasCount(2, ((GeocodingPipeline)provider.GetRequiredService<IGeocoder>()).Layers);
        }

        [TestMethod]
        public void AddTurtleRoute_NoGeocoders_ShouldThrowOnResolve()
        {
            ServiceCollection services = new();
            services.AddTurtleRoute();

            using ServiceProvider provider = services.BuildServiceProvider();

            Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IGeocoder>());
        }

        [TestMethod]
        public async Task ConfigureGeocoding_ShouldApplyMinimumConfidence()
        {
            ServiceCollection services = new();
            services.AddTurtleRoute()
                .AddGeocoder(_ => FakeGeocoder.Returning(GeocodeConfidence.Medium, "fake"))
                .ConfigureGeocoding(o => o.MinimumConfidence = GeocodeConfidence.High);

            using ServiceProvider provider = services.BuildServiceProvider();

            Assert.IsNull(await provider.GetRequiredService<IGeocoder>().GeocodeAsync("Katwilgweg 2", "BE"));
        }
    }
}
