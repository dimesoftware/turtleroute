using System;
using System.Net.Http;
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
        [DataRow(null, 30)]
        [DataRow(5, 5)]
        public void AddHereRouting_ShouldSetTimeoutOnBothClients(int? configured, int expected)
        {
            ServiceCollection services = new();
            services.AddTurtleRoute().AddHereRouting(o =>
            {
                o.ApiKey = "here";
                if (configured.HasValue)
                    o.Timeout = TimeSpan.FromSeconds(configured.Value);
            });

            using ServiceProvider provider = services.BuildServiceProvider();
            IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();

            Assert.AreEqual(TimeSpan.FromSeconds(expected), factory.CreateClient(nameof(HereRoutePlanner)).Timeout);
            Assert.AreEqual(TimeSpan.FromSeconds(expected), factory.CreateClient(nameof(HereRouteMatrix)).Timeout);
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
