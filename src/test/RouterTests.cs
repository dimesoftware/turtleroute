using System.Threading.Tasks;
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

            // Empire State Building
            GeoCoordinate start = new(40.748515, -73.9848141);

            // Flatiron building
            GeoCoordinate end = new(40.741443, -73.989464);

            Route route = await api.GetRouteAsync(start, end);
            Assert.IsTrue(route.Distance < 1500);
        }

        [TestMethod]
        public async Task Router_Trip_ShouldGetDrivingDirections()
        {
            Router api = new(_token);

            GeoCoordinate empireState = new(40.748515, -73.9848141);
            GeoCoordinate flatIron = new(40.741443, -73.989464);
            GeoCoordinate unionSquare = new(40.736151, -73.989365);

            Trip trip = await api.GetTrip(null, empireState, flatIron, unionSquare);
            Assert.IsTrue(trip.Distance < 2300);
        }
    }
}