using System.Threading.Tasks;
using Azure.Maps.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TurtleRoute.Tests
{
    [TestClass]
    public class RouterTests
    {
        private string _token;

        [TestInitialize]
        public void Setup()
        {
            IConfigurationRoot settings = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile("appsettings.development.json", true)
                .AddJsonFile("appsettings.test.json", true)
                .Build();

            foreach ((string key, string value) in settings.AsEnumerable())
            {
                switch (key)
                {
                    case "token":
                        _token = value;
                        break;

                    default:
                        continue;
                }
            }
        }

        [TestMethod]
        public async Task Router_Directions_ShouldGetDrivingDirections()
        {
            Router api = new(_token);

            GeoCoordinate barcelona = new(41.404090, 2.174984);
            GeoCoordinate madrid = new(40.415415, -3.707295);
            GeoCoordinate segovia = new(40.948679, -4.119111);
            GeoCoordinate valencia = new(39.473815, -0.375563);
            GeoCoordinate pamplona = new(42.812803, -1.643674);

            Route route = await api.GetRouteAsync(new RouteDirectionOptions() { ComputeBestWaypointOrder = true, RouteType = RouteType.Shortest }, barcelona, madrid, segovia, valencia, pamplona);

            int distance = 1200 * 1000;
            Assert.IsTrue(route.Distance < distance);
        }

        [TestMethod]
        public async Task Router_Optimize()
        {
            Router api = new(_token);

            GeoCoordinate stop1 = new(50.83612892540386, 4.004047490828118);
            GeoCoordinate stop2 = new(50.9651, 5.5006);
            GeoCoordinate stop3 = new(51.075029, 3.071611);
            Route route = await api.GetRouteAsync(new RouteDirectionOptions() { ComputeBestWaypointOrder = true, RouteType = RouteType.Shortest }, stop1, stop2, stop3);

            Assert.AreEqual(2, route.Legs.Count);
        }
    }
}