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
    }
}