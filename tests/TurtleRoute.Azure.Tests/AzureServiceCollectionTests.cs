using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TurtleRoute.Azure.Tests
{
    [TestClass]
    public class AzureServiceCollectionTests
    {
        [TestMethod]
        public void AddAzureMapsGeocoder_ShouldAddAzureGeocoderLayer()
        {
            ServiceCollection services = new();
            services.AddTurtleRoute().AddAzureMapsGeocoder(o => o.ApiKey = "azure");

            using ServiceProvider provider = services.BuildServiceProvider();

            GeocodingPipeline pipeline = (GeocodingPipeline)provider.GetRequiredService<IGeocoder>();
            Assert.HasCount(1, pipeline.Layers);
            Assert.IsInstanceOfType<AzureGeocoder>(pipeline.Layers[0]);
        }

        [TestMethod]
        public void UseAzureMapsRouting_ShouldRegisterRoutePlannerAndMatrix()
        {
            ServiceCollection services = new();
            services.AddTurtleRoute().UseAzureMapsRouting(o => o.ApiKey = "azure");

            using ServiceProvider provider = services.BuildServiceProvider();

            Assert.IsInstanceOfType<AzureRoutePlanner>(provider.GetRequiredService<IRoutePlanner>());
            Assert.IsInstanceOfType<AzureRouteMatrix>(provider.GetRequiredService<IRouteMatrix>());
        }
    }
}
