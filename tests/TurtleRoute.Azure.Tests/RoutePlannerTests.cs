using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TurtleRoute.Tests;

namespace TurtleRoute.Azure.Tests
{
    [TestClass]
    [TestCategory("Integration")]
    public class RoutePlannerTests
    {
        [TestMethod]
        public async Task Router_Directions_ShouldGetDrivingDirections()
        {
            AzureRoutePlanner api = new(TestSettings.AzureMapsToken);

            GeoCoordinate barcelona = new(41.404090, 2.174984);
            GeoCoordinate madrid = new(40.415415, -3.707295);
            GeoCoordinate segovia = new(40.948679, -4.119111);
            GeoCoordinate valencia = new(39.473815, -0.375563);
            GeoCoordinate pamplona = new(42.812803, -1.643674);

            Route route = await api.GetRouteAsync(new RoutePlanOptions() { OptimizeWaypointOrder = true, RouteType = RouteType.Shortest }, barcelona, madrid, segovia, valencia, pamplona);

            int distance = 1200 * 1000;
            Assert.IsLessThan(distance, route.Distance);
        }

        [TestMethod]
        public async Task Router_Optimize()
        {
            AzureRoutePlanner api = new(TestSettings.AzureMapsToken);

            GeoCoordinate stop1 = new(50.83612892540386, 4.004047490828118);
            GeoCoordinate stop2 = new(50.9651, 5.5006);
            GeoCoordinate stop3 = new(51.075029, 3.071611);
            Route route = await api.GetRouteAsync(new RoutePlanOptions() { OptimizeWaypointOrder = true, RouteType = RouteType.Shortest }, stop1, stop2, stop3);

            Assert.HasCount(2, route.Legs);
        }
    }
}