using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TurtleRoute.Tests
{
    [TestClass]
    public class RoutingPipelineTests
    {
        private static readonly GeoCoordinate Antwerp = new(51.219501, 4.359231);
        private static readonly GeoCoordinate Ghent = new(51.054342, 3.717424);

        [TestMethod]
        public async Task GetRouteAsync_FirstLayerSucceeds_ShouldNotCallSecondLayer()
        {
            FakeRouting primary = FakeRouting.Returning(1000);
            FakeRouting secondary = FakeRouting.Returning(2000);

            Route route = await new RoutePlannerPipeline([primary, secondary]).GetRouteAsync(Antwerp, Ghent);

            Assert.AreEqual(1000, route.Distance);
            Assert.AreEqual(0, secondary.Calls);
        }

        [TestMethod]
        public async Task GetRouteAsync_FirstLayerThrows_ShouldFallBackWithTheSameStopsAndOptions()
        {
            FakeRouting primary = FakeRouting.Throwing(new HttpRequestException("429 rate limited"));
            FakeRouting secondary = FakeRouting.Returning(2000);
            RoutePlanOptions options = new() { RouteType = RouteType.Shortest };

            Route route = await new RoutePlannerPipeline([primary, secondary]).GetRouteAsync(options, Antwerp, Ghent);

            Assert.AreEqual(2000, route.Distance);
            Assert.AreSame(options, secondary.LastOptions);
            CollectionAssert.AreEqual(new[] { Antwerp, Ghent }, secondary.LastCoordinates);
        }

        [TestMethod]
        public async Task GetRouteAsync_EveryLayerThrows_ShouldThrowRoutingException()
        {
            FakeRouting primary = FakeRouting.Throwing(new HttpRequestException("503"));
            FakeRouting secondary = FakeRouting.Throwing(new InvalidOperationException("no route"));

            RoutingException ex = await Assert.ThrowsAsync<RoutingException>(() => new RoutePlannerPipeline([primary, secondary]).GetRouteAsync(Antwerp, Ghent));

            Assert.HasCount(2, ex.InnerExceptions);
        }

        [TestMethod]
        public async Task GetRouteAsync_InvalidInput_ShouldNotFallBack()
        {
            FakeRouting primary = FakeRouting.Throwing(new ArgumentException("A route needs at least an origin and a destination."));
            FakeRouting secondary = FakeRouting.Returning(2000);

            await Assert.ThrowsAsync<ArgumentException>(() => new RoutePlannerPipeline([primary, secondary]).GetRouteAsync(Antwerp));

            Assert.AreEqual(0, secondary.Calls);
        }

        [TestMethod]
        public async Task GetRouteAsync_Cancelled_ShouldNotFallBack()
        {
            using CancellationTokenSource cts = new();
            cts.Cancel();
            FakeRouting primary = FakeRouting.Throwing(new OperationCanceledException(cts.Token));
            FakeRouting secondary = FakeRouting.Returning(2000);

            await Assert.ThrowsAsync<OperationCanceledException>(() => new RoutePlannerPipeline([primary, secondary]).GetRouteAsync([Antwerp, Ghent], null, cts.Token));

            Assert.AreEqual(0, secondary.Calls);
        }

        [TestMethod]
        public async Task GetRouteMatrixAsync_FirstLayerThrows_ShouldFallBack()
        {
            FakeRouting primary = FakeRouting.Throwing(new HttpRequestException("401"));
            FakeRouting secondary = FakeRouting.Returning(2000);

            Route[,] matrix = await new RouteMatrixPipeline([primary, secondary]).GetRouteMatrixAsync(Antwerp, Ghent);

            Assert.AreEqual(2000, matrix[0, 1].Distance);
        }

        [TestMethod]
        public void AddRoutePlanner_ShouldResolvePipelineWithLayersInRegistrationOrder()
        {
            ServiceCollection services = new();
            services.AddTurtleRoute()
                .AddRoutePlanner(_ => FakeRouting.Returning(1))
                .AddRoutePlanner<FakeRouting>()
                .AddRouteMatrix<FakeRouting>();

            using ServiceProvider provider = services.BuildServiceProvider();

            RoutePlannerPipeline planner = (RoutePlannerPipeline)provider.GetRequiredService<IRoutePlanner>();
            Assert.HasCount(2, planner.Layers);
            Assert.AreSame(provider.GetRequiredService<FakeRouting>(), planner.Layers[1]);

            RouteMatrixPipeline matrix = (RouteMatrixPipeline)provider.GetRequiredService<IRouteMatrix>();
            Assert.HasCount(1, matrix.Layers);
        }

        [TestMethod]
        public void AddTurtleRoute_NoRoutePlanners_ShouldThrowOnResolve()
        {
            ServiceCollection services = new();
            services.AddTurtleRoute();

            using ServiceProvider provider = services.BuildServiceProvider();

            Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IRoutePlanner>());
            Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IRouteMatrix>());
        }

        [TestMethod]
        public void AddRoutePlanner_OnShadow_ShouldThrow()
        {
            ServiceCollection services = new();

            Assert.Throws<InvalidOperationException>(() => services.AddTurtleRoute()
                .AddGeocoder<FakeGeocoder>()
                .AddShadowGeocoder(shadow => shadow.AddGeocoder<OtherFakeGeocoder>().AddRoutePlanner<FakeRouting>()));
        }

        internal class FakeRouting : IRoutePlanner, IRouteMatrix
        {
            private readonly Func<int> _distance;

            public FakeRouting() : this(() => 0)
            {
            }

            private FakeRouting(Func<int> distance)
            {
                _distance = distance;
            }

            public int Calls { get; private set; }

            public List<GeoCoordinate> LastCoordinates { get; private set; }

            public RoutePlanOptions LastOptions { get; private set; }

            public static FakeRouting Returning(int distance) => new(() => distance);

            public static FakeRouting Throwing(Exception exception) => new(() => throw exception);

            public Task<Route> GetRouteAsync(IEnumerable<GeoCoordinate> coordinates, RoutePlanOptions options = null, CancellationToken cancellationToken = default)
            {
                Calls++;
                LastCoordinates = [.. coordinates];
                LastOptions = options;
                return Task.FromResult(new Route { Distance = _distance() });
            }

            public Task<Route[,]> GetRouteMatrixAsync(IEnumerable<GeoCoordinate> coordinates, CancellationToken cancellationToken = default)
            {
                Calls++;
                int distance = _distance();
                return Task.FromResult(new Route[,] { { new Route(), new Route { Distance = distance } }, { new Route { Distance = distance }, new Route() } });
            }
        }
    }
}
