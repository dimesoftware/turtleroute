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
        public void AddAzureMapsRouting_ShouldAddRoutePlannerAndMatrixLayers()
        {
            ServiceCollection services = new();
            services.AddTurtleRoute().AddAzureMapsRouting(o => o.ApiKey = "azure");

            using ServiceProvider provider = services.BuildServiceProvider();

            RoutePlannerPipeline planner = (RoutePlannerPipeline)provider.GetRequiredService<IRoutePlanner>();
            Assert.HasCount(1, planner.Layers);
            Assert.IsInstanceOfType<AzureRoutePlanner>(planner.Layers[0]);

            RouteMatrixPipeline matrix = (RouteMatrixPipeline)provider.GetRequiredService<IRouteMatrix>();
            Assert.HasCount(1, matrix.Layers);
            Assert.IsInstanceOfType<AzureRouteMatrix>(matrix.Layers[0]);
        }
    }
}
