using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TurtleRoute.Tests;

namespace TurtleRoute.Here.Tests
{
    [TestClass]
    [TestCategory("Integration")]
    public class MatrixTests
    {
        [TestMethod]
        public async Task Matrix_Trip_ShouldGetDrivingDirections()
        {
            HereRouteMatrix api = Create();

            GeoCoordinate empireState = new(40.748515, -73.9848141);
            GeoCoordinate flatIron = new(40.741443, -73.989464);
            GeoCoordinate unionSquare = new(40.736151, -73.989365);

            IEnumerable<Route> trip = await api.GetTripAsync(empireState, flatIron, unionSquare);

            int expectedDistance1 = 1300; // Approximate expected distance for Empire State → Flatiron
            int expectedDistance2 = 2200; // Approximate expected distance for Empire State → Union Square

            int tolerance = 300; // Allow a variation of ±300 meters

            Assert.IsLessThanOrEqualTo(tolerance,
Math.Abs(trip.ElementAt(0).Distance - expectedDistance1), $"Distance {trip.ElementAt(0).Distance} is out of range for Trip 1.");

            Assert.IsLessThanOrEqualTo(tolerance,
Math.Abs(trip.ElementAt(1).Distance - expectedDistance2), $"Distance {trip.ElementAt(1).Distance} is out of range for Trip 2.");
        }

        [TestMethod]
        public async Task Matrix_DistantPoints_ShouldUseFlexibleMode()
        {
            HereRouteMatrix api = Create();

            GeoCoordinate antwerp = new(51.219501, 4.359231);
            GeoCoordinate madrid = new(40.415415, -3.707295);

            Route[,] matrix = await api.GetRouteMatrixAsync(antwerp, madrid);

            Assert.IsGreaterThan(1_400_000, matrix[0, 1].Distance);
        }

        private static HereRouteMatrix Create() => new(new HttpClient(), new HereOptions { ApiKey = TestSettings.HereToken });
    }
}
