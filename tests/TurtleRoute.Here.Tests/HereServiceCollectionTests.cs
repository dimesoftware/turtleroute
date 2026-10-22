using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TurtleRoute.Here.Tests
{
    [TestClass]
    public class HereServiceCollectionTests
    {
        [TestMethod]
        public void UseHereRouting_ShouldRegisterRoutePlannerAndMatrix()
        {
            ServiceCollection services = new();
            services.AddTurtleRoute().UseHereRouting(o => o.ApiKey = "here");

            using ServiceProvider provider = services.BuildServiceProvider();

            Assert.IsInstanceOfType<HereRoutePlanner>(provider.GetRequiredService<IRoutePlanner>());
            Assert.IsInstanceOfType<HereRouteMatrix>(provider.GetRequiredService<IRouteMatrix>());
        }

        [TestMethod]
        public void UseHereRouting_ShouldReplaceEarlierRouting()
        {
            ServiceCollection services = new();
            services.AddTurtleRoute()
                .UseRoutePlanner(_ => null)
                .UseHereRouting(o => o.ApiKey = "here");

            using ServiceProvider provider = services.BuildServiceProvider();

            Assert.IsInstanceOfType<HereRoutePlanner>(provider.GetRequiredService<IRoutePlanner>());
        }

        [TestMethod]
        public void UseHereRouting_ShouldApplyHttpClientConfiguration()
        {
            int configured = 0;

            ServiceCollection services = new();
            services.AddTurtleRoute().UseHereRouting(o => o.ApiKey = "here", http => configured++);

            Assert.AreEqual(2, configured);
        }
    }
}
