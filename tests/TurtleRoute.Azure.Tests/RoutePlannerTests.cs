using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TurtleRoute.Tests;

namespace TurtleRoute.Azure.Tests
{
    [TestClass]
    [TestCategory("Integration")]
    public class RoutePlannerTests
    {
        private static readonly GeoCoordinate Antwerp = new(51.219501, 4.359231);
        private static readonly GeoCoordinate Mechelen = new(51.025, 4.4776);

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
        [DataRow(TravelMode.Car)]
        [DataRow(TravelMode.Van)]
        [DataRow(TravelMode.Truck)]
        [DataRow(TravelMode.Motorcycle)]
        [DataRow(TravelMode.Bicycle)]
        [DataRow(TravelMode.Pedestrian)]
        public async Task Router_TravelMode_ShouldGetRoute(TravelMode travelMode)
        {
            AzureRoutePlanner api = new(TestSettings.AzureMapsToken);

            Route route = await api.GetRouteAsync(new RoutePlanOptions { TravelMode = travelMode }, Antwerp, Mechelen);

            Assert.IsGreaterThan(0, route.Distance);
            Assert.IsGreaterThan(0, route.Duration);
        }

        [TestMethod]
        public async Task Router_Pedestrian_ShouldTakeLongerThanCar()
        {
            AzureRoutePlanner api = new(TestSettings.AzureMapsToken);

            Route car = await api.GetRouteAsync(new RoutePlanOptions { TravelMode = TravelMode.Car }, Antwerp, Mechelen);
            Route pedestrian = await api.GetRouteAsync(new RoutePlanOptions { TravelMode = TravelMode.Pedestrian }, Antwerp, Mechelen);

            Assert.IsGreaterThan(car.Duration * 2, pedestrian.Duration);
        }

        [TestMethod]
        public async Task Router_FutureAndPastDeparture_ShouldGetRoute()
        {
            AzureRoutePlanner api = new(TestSettings.AzureMapsToken);

            // Azure rejects departure times that are not in the future, so a past one must be sent as "now".
            Route future = await api.GetRouteAsync(new RoutePlanOptions { DepartAt = DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(7) }, Antwerp, Mechelen);
            Route past = await api.GetRouteAsync(new RoutePlanOptions { DepartAt = DateTimeOffset.UtcNow.AddHours(-1) }, Antwerp, Mechelen);

            Assert.IsGreaterThan(0, future.Duration);
            Assert.IsGreaterThan(0, past.Duration);
        }

        [TestMethod]
        public Task Router_Optimize_RevisionsShouldFollowTheLegs()
            => RoutePlannerContract.RevisionsShouldFollowTheLegsAsync(new AzureRoutePlanner(TestSettings.AzureMapsToken));

        [TestMethod]
        public Task Router_OptimizeSingleStop_ShouldHaveOneRevision()
            => RoutePlannerContract.SingleStopShouldHaveOneRevisionAsync(new AzureRoutePlanner(TestSettings.AzureMapsToken));

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