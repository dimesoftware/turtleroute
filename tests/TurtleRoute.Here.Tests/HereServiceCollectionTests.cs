using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TurtleRoute.Here.Tests
{
    [TestClass]
    public class HereServiceCollectionTests
    {
        [TestMethod]
        public void AddHereRouting_ShouldAddRoutePlannerAndMatrixLayers()
        {
            ServiceCollection services = new();
            services.AddTurtleRoute().AddHereRouting(o => o.ApiKey = "here");

            using ServiceProvider provider = services.BuildServiceProvider();

            RoutePlannerPipeline planner = (RoutePlannerPipeline)provider.GetRequiredService<IRoutePlanner>();
            Assert.HasCount(1, planner.Layers);
            Assert.IsInstanceOfType<HereRoutePlanner>(planner.Layers[0]);

            RouteMatrixPipeline matrix = (RouteMatrixPipeline)provider.GetRequiredService<IRouteMatrix>();
            Assert.HasCount(1, matrix.Layers);
            Assert.IsInstanceOfType<HereRouteMatrix>(matrix.Layers[0]);
        }

        [TestMethod]
        public void AddHereRouting_ShouldApplyHttpClientConfiguration()
        {
            int configured = 0;

            ServiceCollection services = new();
            services.AddTurtleRoute().AddHereRouting(o => o.ApiKey = "here", http => configured++);

            Assert.AreEqual(2, configured);
        }
    }
}
