using System;
using System.Collections.Generic;
using System.Linq;
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

            IEnumerable<Route> trip = await api.GetTrip(empireState, flatIron, unionSquare);

            int expectedDistance1 = 1300; // Approximate expected distance for Empire State → Flatiron
            int expectedDistance2 = 2200; // Approximate expected distance for Empire State → Union Square

            int tolerance = 300; // Allow a variation of ±300 meters

            Assert.IsTrue(Math.Abs(trip.ElementAt(0).Distance - expectedDistance1) <= tolerance,
                $"Distance {trip.ElementAt(0).Distance} is out of range for Trip 1.");

            Assert.IsTrue(Math.Abs(trip.ElementAt(1).Distance - expectedDistance2) <= tolerance,
                $"Distance {trip.ElementAt(1).Distance} is out of range for Trip 2.");
        }
    }
}