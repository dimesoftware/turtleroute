using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TurtleRoute.Tests
{
    [TestClass]
    public class MatrixTests
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
        public async Task Matrix_Trip_ShouldGetDrivingDirections()
        {
            Matrix api = new(_token);

            GeoCoordinate empireState = new(40.748515, -73.9848141);
            GeoCoordinate flatIron = new(40.741443, -73.989464);
            GeoCoordinate unionSquare = new(40.736151, -73.989365);

            var trip = await api.GetTrip(empireState, flatIron, unionSquare);
        }
    }
}