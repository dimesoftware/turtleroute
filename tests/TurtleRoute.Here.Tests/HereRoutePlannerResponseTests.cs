using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TurtleRoute.Here.Tests
{
    /// <summary>
    /// Offline tests for how HERE routing requests are built and responses are mapped, using canned JSON instead of the live API.
    /// </summary>
    [TestClass]
    public class HereRoutePlannerResponseTests
    {
        // The example from the flexible polyline specification, at precision 5.
        private const string Polyline = "BFoz5xJ67i1B1B7PzIhaxL7Y";

        private static readonly GeoCoordinate Antwerp = new(51.219501, 4.359231);
        private static readonly GeoCoordinate Ghent = new(51.054342, 3.717424);
        private static readonly GeoCoordinate Brussels = new(50.846557, 4.351697);
        private static readonly GeoCoordinate Leuven = new(50.879844, 4.700518);
        private static readonly GeoCoordinate Hasselt = new(50.930690, 5.332480);

        [TestMethod]
        public async Task GetRouteAsync_ShouldMapSectionsToLegs()
        {
            StubHandler handler = new StubHandler().On("/routes", Routes(Section(1200, 25000), Section(900, 18000)));

            Route route = await Create(handler).GetRouteAsync(Antwerp, Ghent, Brussels);

            Assert.HasCount(2, route.Legs);
            Assert.AreEqual(43000, route.Distance);
            Assert.AreEqual(2100, route.Duration);
            Assert.AreEqual(25000, route.Legs[0].Distance);
            Assert.AreEqual(1200, route.Legs[0].Duration);
            Assert.IsEmpty(route.Revisions);
        }

        [TestMethod]
        public async Task GetRouteAsync_ShouldDecodePolylineIntoWaypoints()
        {
            StubHandler handler = new StubHandler().On("/routes", Routes(Section(60, 1000)));

            Route route = await Create(handler).GetRouteAsync(Antwerp, Ghent);

            GeoCoordinate[] waypoints = [.. route.Legs[0].Waypoints];
            Assert.HasCount(4, waypoints);
            Assert.AreEqual(50.10228, waypoints[0].Latitude, 1e-9);
            Assert.AreEqual(8.69821, waypoints[0].Longitude, 1e-9);
            Assert.AreEqual(50.09878, waypoints[3].Latitude, 1e-9);
            Assert.AreEqual(8.68752, waypoints[3].Longitude, 1e-9);
        }

        [TestMethod]
        public async Task GetRouteAsync_ShouldSendStopsAsOriginViaAndDestination()
        {
            StubHandler handler = new StubHandler().On("/routes", Routes(Section(1, 1), Section(1, 1)));

            await Create(handler).GetRouteAsync(new RoutePlanOptions { RouteType = RouteType.Shortest }, Antwerp, Ghent, Brussels);

            string query = Uri.UnescapeDataString(handler.Last("/routes").RequestUri.Query);
            StringAssert.Contains(query, "origin=51.219501,4.359231");
            StringAssert.Contains(query, "via=51.054342,3.717424");
            StringAssert.Contains(query, "destination=50.846557,4.351697");
            StringAssert.Contains(query, "routingMode=short");
            StringAssert.Contains(query, "return=polyline,summary");
            Assert.DoesNotContain("departureTime", query);
        }

        [TestMethod]
        public async Task GetRouteAsync_WithoutTraffic_ShouldIgnoreDepartureTime()
        {
            StubHandler handler = new StubHandler().On("/routes", Routes(Section(1, 1)));

            await Create(handler).GetRouteAsync(new RoutePlanOptions { UseTrafficData = false }, Antwerp, Ghent);

            StringAssert.Contains(handler.Last("/routes").RequestUri.Query, "departureTime=any");
        }

        [TestMethod]
        [DataRow(TravelMode.Car, "car")]
        [DataRow(TravelMode.Truck, "truck")]
        [DataRow(TravelMode.Bicycle, "bicycle")]
        [DataRow(TravelMode.Pedestrian, "pedestrian")]
        public async Task GetRouteAsync_TravelMode_ShouldSendTransportMode(TravelMode travelMode, string transportMode)
        {
            StubHandler handler = new StubHandler().On("/routes", Routes(Section(1, 1)));

            await Create(handler).GetRouteAsync(new RoutePlanOptions { TravelMode = travelMode }, Antwerp, Ghent);

            StringAssert.Contains(handler.Last("/routes").RequestUri.Query, $"transportMode={transportMode}");
        }

        [TestMethod]
        [DataRow(TravelMode.Van)]
        [DataRow(TravelMode.Motorcycle)]
        public async Task GetRouteAsync_UnsupportedTravelMode_ShouldThrowWithoutCallingHere(TravelMode travelMode)
        {
            StubHandler handler = new();

            await Assert.ThrowsAsync<NotSupportedException>(() => Create(handler).GetRouteAsync(new RoutePlanOptions { TravelMode = travelMode, OptimizeWaypointOrder = true }, Antwerp, Ghent, Brussels, Leuven));

            Assert.IsEmpty(handler.Requests);
        }

        [TestMethod]
        public async Task GetRouteAsync_UnsupportedTravelMode_ShouldFallBackToNextLayer()
        {
            StubHandler handler = new();
            Route fallback = new();
            RoutePlannerPipeline pipeline = new([Create(handler), new FixedRoutePlanner(fallback)]);

            Route route = await pipeline.GetRouteAsync(new RoutePlanOptions { TravelMode = TravelMode.Van }, Antwerp, Ghent);

            Assert.AreSame(fallback, route);
            Assert.IsEmpty(handler.Requests);
        }

        [TestMethod]
        public async Task GetRouteAsync_FutureDeparture_ShouldSendDepartureTime()
        {
            StubHandler handler = new StubHandler().On("/routes", Routes(Section(1, 1)));
            DateTimeOffset departure = new(DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(2).AddHours(8), DateTimeKind.Unspecified), TimeSpan.FromHours(2));

            await Create(handler).GetRouteAsync(new RoutePlanOptions { DepartAt = departure }, Antwerp, Ghent);

            string query = Uri.UnescapeDataString(handler.Last("/routes").RequestUri.Query);
            StringAssert.Contains(query, $"departureTime={departure:yyyy-MM-dd'T'HH:mm:ss}+02:00");
        }

        [TestMethod]
        public async Task GetRouteAsync_PastDeparture_ShouldDepartNow()
        {
            StubHandler handler = new StubHandler().On("/routes", Routes(Section(1, 1)));

            await Create(handler).GetRouteAsync(new RoutePlanOptions { DepartAt = DateTimeOffset.UtcNow.AddHours(-1) }, Antwerp, Ghent);

            Assert.DoesNotContain("departureTime", handler.Last("/routes").RequestUri.Query);
        }

        [TestMethod]
        public async Task GetRouteAsync_DepartureWithoutTraffic_ShouldIgnoreDeparture()
        {
            StubHandler handler = new StubHandler().On("/routes", Routes(Section(1, 1)));

            await Create(handler).GetRouteAsync(new RoutePlanOptions { UseTrafficData = false, DepartAt = DateTimeOffset.UtcNow.AddDays(1) }, Antwerp, Ghent);

            StringAssert.Contains(handler.Last("/routes").RequestUri.Query, "departureTime=any");
        }

        [TestMethod]
        public async Task GetRouteAsync_OptimizeTruckWithDeparture_ShouldSequenceForTruckAtThatTime()
        {
            StubHandler handler = new StubHandler()
                .On("/findsequence2", """{"results":[{"waypoints":[{"id":"start","sequence":0},{"id":"w0","sequence":1},{"id":"w1","sequence":2},{"id":"end","sequence":3}]}]}""")
                .On("/routes", Routes(Section(1, 1), Section(1, 1), Section(1, 1)));
            DateTimeOffset departure = new(DateTime.UtcNow.Date.AddDays(2).AddHours(8), TimeSpan.Zero);

            await Create(handler).GetRouteAsync(new RoutePlanOptions { TravelMode = TravelMode.Truck, DepartAt = departure, OptimizeWaypointOrder = true }, Antwerp, Ghent, Brussels, Leuven);

            string sequenceQuery = Uri.UnescapeDataString(handler.Last("/findsequence2").RequestUri.Query);
            StringAssert.Contains(sequenceQuery, "mode=fastest;truck;traffic:enabled");
            StringAssert.Contains(sequenceQuery, $"departure={departure:yyyy-MM-dd'T'HH:mm:ss}+00:00");
            StringAssert.Contains(handler.Last("/routes").RequestUri.Query, "transportMode=truck");
        }

        [TestMethod]
        public async Task GetRouteAsync_OptimizeWaypointOrder_ShouldRouteInOptimizedOrder()
        {
            // HERE suggests visiting the intermediate stops as Leuven (w2), Ghent (w0), Brussels (w1).
            StubHandler handler = new StubHandler()
                .On("/findsequence2", """{"results":[{"waypoints":[{"id":"start","sequence":0},{"id":"w2","sequence":1},{"id":"w0","sequence":2},{"id":"w1","sequence":3},{"id":"end","sequence":4}]}]}""")
                .On("/routes", Routes(Section(1, 1), Section(1, 1), Section(1, 1), Section(1, 1)));

            Route route = await Create(handler).GetRouteAsync(new RoutePlanOptions { OptimizeWaypointOrder = true }, Antwerp, Ghent, Brussels, Leuven, Hasselt);

            string sequenceQuery = Uri.UnescapeDataString(handler.Last("/findsequence2").RequestUri.Query);
            StringAssert.Contains(sequenceQuery, "start=start;51.219501,4.359231");
            StringAssert.Contains(sequenceQuery, "destination1=w0;51.054342,3.717424");
            StringAssert.Contains(sequenceQuery, "end=end;50.93069,5.33248");

            string routeQuery = Uri.UnescapeDataString(handler.Last("/routes").RequestUri.Query);
            int leuven = routeQuery.IndexOf("via=50.879844,4.700518", StringComparison.Ordinal);
            int ghent = routeQuery.IndexOf("via=51.054342,3.717424", StringComparison.Ordinal);
            int brussels = routeQuery.IndexOf("via=50.846557,4.351697", StringComparison.Ordinal);
            Assert.IsTrue(leuven < ghent && ghent < brussels, routeQuery);

            CollectionAssert.AreEqual(new[] { new RouteRevision(2, 0), new RouteRevision(0, 1), new RouteRevision(1, 2) }, route.Revisions);
        }

        [TestMethod]
        public async Task GetRouteAsync_OptimizeWithOneIntermediateStop_ShouldSkipWaypointsSequence()
        {
            StubHandler handler = new StubHandler().On("/routes", Routes(Section(1, 1), Section(1, 1)));

            await Create(handler).GetRouteAsync(new RoutePlanOptions { OptimizeWaypointOrder = true }, Antwerp, Ghent, Brussels);

            Assert.IsNull(handler.Last("/findsequence2"));
        }

        [TestMethod]
        public async Task GetRouteAsync_NoRoute_ShouldThrowWithNotice()
        {
            StubHandler handler = new StubHandler().On("/routes", """{"routes":[],"notices":[{"title":"Route calculation failed: Couldn't find a route.","code":"noRouteFound"}]}""");

            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Create(handler).GetRouteAsync(Antwerp, Ghent));

            StringAssert.Contains(ex.Message, "Couldn't find a route");
        }

        [TestMethod]
        [DataRow(HttpStatusCode.Unauthorized)]
        [DataRow(HttpStatusCode.TooManyRequests)]
        [DataRow(HttpStatusCode.ServiceUnavailable)]
        public async Task GetRouteAsync_ErrorStatus_ShouldThrowHttpRequestException(HttpStatusCode status)
        {
            StubHandler handler = new StubHandler().On("/routes", """{"title":"Unauthorized","cause":"Invalid apiKey"}""", status);

            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => Create(handler).GetRouteAsync(Antwerp, Ghent));

            Assert.AreEqual(status, ex.StatusCode);
            StringAssert.Contains(ex.Message, "Invalid apiKey");
        }

        [TestMethod]
        public async Task GetRouteAsync_SingleCoordinate_ShouldThrowArgumentException()
            => await Assert.ThrowsAsync<ArgumentException>(() => Create(new StubHandler()).GetRouteAsync(Antwerp));

        [TestMethod]
        public void Constructor_MissingApiKey_ThrowsArgumentNullException()
            => Assert.Throws<ArgumentNullException>(() => new HereRoutePlanner(new HttpClient(), new HereOptions()));

        private class FixedRoutePlanner : IRoutePlanner
        {
            private readonly Route _route;

            public FixedRoutePlanner(Route route)
            {
                _route = route;
            }

            public Task<Route> GetRouteAsync(IEnumerable<GeoCoordinate> coordinates, RoutePlanOptions options = null, CancellationToken cancellationToken = default)
                => Task.FromResult(_route);
        }

        private static HereRoutePlanner Create(StubHandler handler)
            => new(new HttpClient(handler), new HereOptions { ApiKey = "test" });

        private static string Routes(params string[] sections)
            => $$"""{"routes":[{"id":"r1","sections":[{{string.Join(",", sections)}}]}]}""";

        private static string Section(int duration, int length)
            => $$"""{"id":"s","type":"vehicle","summary":{"duration":{{duration}},"length":{{length}},"baseDuration":{{duration}}},"polyline":"{{Polyline}}"}""";
    }
}
