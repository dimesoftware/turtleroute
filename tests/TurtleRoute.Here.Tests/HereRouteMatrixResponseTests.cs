using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TurtleRoute.Here.Tests
{
    /// <summary>
    /// Offline tests for how HERE matrix requests are built and responses are mapped, using canned JSON instead of the live API.
    /// </summary>
    [TestClass]
    public class HereRouteMatrixResponseTests
    {
        private static readonly GeoCoordinate Antwerp = new(51.219501, 4.359231);
        private static readonly GeoCoordinate Ghent = new(51.054342, 3.717424);
        private static readonly GeoCoordinate Madrid = new(40.415415, -3.707295);

        [TestMethod]
        public async Task GetRouteMatrixAsync_ShouldMapRowMajorResults()
        {
            StubHandler handler = new StubHandler().On("/matrix", Matrix(2, 2, travelTimes: [0, 3000, 3100, 0], distances: [0, 60000, 61000, 0]));

            Route[,] matrix = await Create(handler).GetRouteMatrixAsync(Antwerp, Ghent);

            Assert.AreEqual(60000, matrix[0, 1].Distance);
            Assert.AreEqual(3000, matrix[0, 1].Duration);
            Assert.AreEqual(61000, matrix[1, 0].Distance);
            Assert.AreEqual(3100, matrix[1, 0].Duration);
        }

        [TestMethod]
        public async Task GetRouteMatrixAsync_PairWithoutRoute_ShouldBeZero()
        {
            StubHandler handler = new StubHandler().On("/matrix", Matrix(2, 2, travelTimes: [0, 3000, 3100, 0], distances: [0, 60000, 61000, 0], errorCodes: [0, 3, 0, 0]));

            Route[,] matrix = await Create(handler).GetRouteMatrixAsync(Antwerp, Ghent);

            Assert.AreEqual(0, matrix[0, 1].Distance);
            Assert.AreEqual(61000, matrix[1, 0].Distance);
        }

        [TestMethod]
        public async Task GetRouteMatrixAsync_NearbyPoints_ShouldUseOneRegionRequest()
        {
            StubHandler handler = new StubHandler().On("/matrix", Matrix(2, 2, [0, 1, 1, 0], [0, 1, 1, 0]));

            await Create(handler).GetRouteMatrixAsync(Antwerp, Ghent);

            Assert.HasCount(1, handler.Requests);
            using JsonDocument body = JsonDocument.Parse(handler.RequestBodies[0]);
            JsonElement region = body.RootElement.GetProperty("regionDefinition");
            Assert.AreEqual("circle", region.GetProperty("type").GetString());
            Assert.IsLessThanOrEqualTo(200_000, region.GetProperty("radius").GetInt32());
            StringAssert.Contains(handler.Requests[0].RequestUri.Query, "async=false");
        }

        [TestMethod]
        public async Task GetRouteMatrixAsync_DistantPoints_ShouldUseWorldRegion()
        {
            StubHandler handler = new StubHandler().On("/matrix", Matrix(2, 2, [0, 1, 1, 0], [0, 1, 1, 0]));

            await Create(handler).GetRouteMatrixAsync(Antwerp, Madrid);

            using JsonDocument body = JsonDocument.Parse(handler.RequestBodies[0]);
            Assert.AreEqual("world", body.RootElement.GetProperty("regionDefinition").GetProperty("type").GetString());
        }

        [TestMethod]
        public async Task GetRouteMatrixAsync_LargeDistantMatrix_ShouldBeSplitInFlexibleChunks()
        {
            // 20 points from Antwerp to Madrid: flexible mode allows 15 origins per request, so two requests of 15 and 5 origins.
            List<GeoCoordinate> points = [.. Enumerable.Range(0, 20).Select(i => new GeoCoordinate(
                Antwerp.Latitude + (Madrid.Latitude - Antwerp.Latitude) * i / 19,
                Antwerp.Longitude + (Madrid.Longitude - Antwerp.Longitude) * i / 19))];

            SequencedHandler handler = new(new Queue<string>([Matrix(15, 20, Distance(15 * 20, 1), Distance(15 * 20, 1)), Matrix(5, 20, Distance(5 * 20, 2), Distance(5 * 20, 2))]));

            Route[,] matrix = await Create(handler).GetRouteMatrixAsync(points);

            Assert.HasCount(2, handler.Requests);
            Assert.AreEqual(1, matrix[14, 19].Distance);
            Assert.AreEqual(2, matrix[15, 0].Distance);
            Assert.AreEqual(2, matrix[19, 19].Distance);
        }

        [TestMethod]
        public async Task GetRouteMatrixAsync_ShouldAvoidFerriesAndDirtRoads()
        {
            StubHandler handler = new StubHandler().On("/matrix", Matrix(2, 2, [0, 1, 1, 0], [0, 1, 1, 0]));

            await Create(handler).GetRouteMatrixAsync(Antwerp, Ghent);

            using JsonDocument body = JsonDocument.Parse(handler.RequestBodies[0]);
            string[] features = [.. body.RootElement.GetProperty("avoid").GetProperty("features").EnumerateArray().Select(f => f.GetString())];
            CollectionAssert.AreEquivalent(new[] { "ferry", "dirtRoad" }, features);
        }

        [TestMethod]
        public async Task GetRouteMatrixAsync_NoCoordinates_ShouldReturnEmptyMatrixWithoutRequest()
        {
            StubHandler handler = new();

            Route[,] matrix = await Create(handler).GetRouteMatrixAsync([]);

            Assert.IsEmpty(matrix);
            Assert.IsEmpty(handler.Requests);
        }

        [TestMethod]
        public void Constructor_MissingApiKey_ThrowsArgumentNullException()
            => Assert.Throws<ArgumentNullException>(() => new HereRouteMatrix(new HttpClient(), new HereOptions()));

        private static HereRouteMatrix Create(StubHandler handler)
            => new(new HttpClient(handler), new HereOptions { ApiKey = "test" });

        private static int[] Distance(int count, int value) => [.. Enumerable.Repeat(value, count)];

        private static string Matrix(int origins, int destinations, int[] travelTimes, int[] distances, int[] errorCodes = null)
            => JsonSerializer.Serialize(new
            {
                matrixId = "m1",
                matrix = new { numOrigins = origins, numDestinations = destinations, travelTimes, distances, errorCodes = errorCodes ?? new int[travelTimes.Length] }
            });

        /// <summary>
        /// Returns the queued responses in order, for requests to the same path.
        /// </summary>
        private class SequencedHandler(Queue<string> responses) : StubHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                On("/matrix", responses.Dequeue());
                return await base.SendAsync(request, cancellationToken);
            }
        }
    }
}
