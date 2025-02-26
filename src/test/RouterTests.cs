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

            GeoCoordinate start = new(51.219501, 4.359231);
            GeoCoordinate end = new(51.214625, 4.382112);

            Route route = await api.GetDirectionsAsync(start, end);
            Assert.IsTrue(route.Distance < 5000);
        }
    }
}