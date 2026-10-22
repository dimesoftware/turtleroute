using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TurtleRoute.OpenCage.Tests
{
    [TestClass]
    public class OpenCageServiceCollectionTests
    {
        [TestMethod]
        public void AddOpenCageGeocoder_ShouldAddOpenCageGeocoderLayer()
        {
            ServiceCollection services = new();
            services.AddTurtleRoute().AddOpenCageGeocoder(o => o.ApiKey = "opencage");

            using ServiceProvider provider = services.BuildServiceProvider();

            GeocodingPipeline pipeline = (GeocodingPipeline)provider.GetRequiredService<IGeocoder>();
            Assert.HasCount(1, pipeline.Layers);
            Assert.IsInstanceOfType<OpenCageGeocoder>(pipeline.Layers[0]);
        }

        [TestMethod]
        public void AddOpenCageGeocoder_ShouldSetTimeout()
        {
            ServiceCollection services = new();
            services.AddTurtleRoute().AddOpenCageGeocoder(o => o.ApiKey = "opencage");
            services.AddTurtleRoute().AddOpenCageGeocoder(o => o.Timeout = TimeSpan.FromSeconds(3));

            using ServiceProvider provider = services.BuildServiceProvider();

            Assert.AreEqual(TimeSpan.FromSeconds(3), provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(OpenCageGeocoder)).Timeout);
        }

        [TestMethod]
        public void AddOpenCageGeocoder_DefaultTimeout_ShouldBeTenSeconds()
        {
            ServiceCollection services = new();
            services.AddTurtleRoute().AddOpenCageGeocoder(o => o.ApiKey = "opencage");

            using ServiceProvider provider = services.BuildServiceProvider();

            Assert.AreEqual(TimeSpan.FromSeconds(10), provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(OpenCageGeocoder)).Timeout);
        }

        [TestMethod]
        public void AddOpenCageGeocoder_ShouldApplyHttpClientConfiguration()
        {
            bool configured = false;

            ServiceCollection services = new();
            services.AddTurtleRoute().AddOpenCageGeocoder(o => o.ApiKey = "opencage", http => configured = http != null);

            Assert.IsTrue(configured);
        }
    }
}
