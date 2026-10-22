using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TurtleRoute.Tests
{
    /// <summary>
    /// Runs shadow mode end to end through DI and the real background worker, with fake geocoders.
    /// </summary>
    [TestClass]
    public class ShadowGeocodingTests
    {
        private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);
        private static readonly Address Katwilgweg = new("Katwilgweg", "2", "2050", "Antwerpen", "", "BE");
        private static readonly GeocodeResult Primary = new(new GeoCoordinate(51.219501, 4.359231), GeocodeConfidence.High, "primary");
        private static readonly GeocodeResult NearbyShadow = new(new GeoCoordinate(51.2205, 4.359231), GeocodeConfidence.Medium, "shadow");

        [TestMethod]
        public async Task GeocodeAsync_ShouldReturnPrimaryResultAndCompareInBackground()
        {
            await using Harness harness = await Harness.StartAsync(FakeGeocoder.Returning(Primary), FakeGeocoder.Returning(NearbyShadow));

            GeocodeResult result = await harness.Geocoder.GeocodeAsync(Katwilgweg);

            Assert.AreSame(Primary, result);

            GeocodingComparison comparison = await harness.Sink.NextAsync();
            Assert.AreEqual("Katwilgweg, 2, 2050, Antwerpen, BE", comparison.Query);
            Assert.AreEqual("BE", comparison.Country);
            Assert.AreSame(Primary, comparison.Primary.Result);
            Assert.AreSame(NearbyShadow, comparison.Shadow.Result);
            Assert.AreEqual(111, comparison.DistanceInMeters.Value, 2);
            Assert.IsTrue(comparison.ShadowAccepted);
        }

        [TestMethod]
        public async Task GeocodeAsync_Freeform_ShouldCompareWithSameQuery()
        {
            await using Harness harness = await Harness.StartAsync(FakeGeocoder.Returning(Primary), FakeGeocoder.Returning(NearbyShadow));

            await harness.Geocoder.GeocodeAsync("Katwilgweg 2, Antwerpen", "BE");

            GeocodingComparison comparison = await harness.Sink.NextAsync();
            Assert.AreEqual("Katwilgweg 2, Antwerpen", comparison.Query);
            Assert.AreEqual("BE", comparison.Country);
        }

        [TestMethod]
        public async Task GeocodeAsync_ShadowThrows_ShouldRecordErrorWithoutAffectingCaller()
        {
            await using Harness harness = await Harness.StartAsync(FakeGeocoder.Returning(Primary), FakeGeocoder.Throwing(new HttpRequestException("secret details")));

            GeocodeResult result = await harness.Geocoder.GeocodeAsync(Katwilgweg);

            Assert.AreSame(Primary, result);
            GeocodingComparison comparison = await harness.Sink.NextAsync();
            Assert.AreEqual("HttpRequestException", comparison.Shadow.Error);
            Assert.IsNull(comparison.Shadow.Result);
            Assert.IsNull(comparison.DistanceInMeters);
            Assert.IsFalse(comparison.ShadowAccepted);
        }

        [TestMethod]
        public async Task GeocodeAsync_ShadowTooSlow_ShouldRecordTimeout()
        {
            await using Harness harness = await Harness.StartAsync(FakeGeocoder.Returning(Primary), new SlowGeocoder(), o => o.Timeout = TimeSpan.FromMilliseconds(50));

            await harness.Geocoder.GeocodeAsync(Katwilgweg);

            Assert.AreEqual("Timeout", (await harness.Sink.NextAsync()).Shadow.Error);
        }

        [TestMethod]
        public async Task GeocodeAsync_LowConfidenceShadow_ShouldBeComparedButNotAccepted()
        {
            GeocodeResult low = new(NearbyShadow.Coordinate, GeocodeConfidence.Low, "shadow");
            await using Harness harness = await Harness.StartAsync(FakeGeocoder.Returning(Primary), FakeGeocoder.Returning(low));

            await harness.Geocoder.GeocodeAsync(Katwilgweg);

            GeocodingComparison comparison = await harness.Sink.NextAsync();
            Assert.AreSame(low, comparison.Shadow.Result);
            Assert.IsFalse(comparison.ShadowAccepted);
        }

        [TestMethod]
        public async Task GeocodeAsync_PrimaryThrows_ShouldPropagateAndNotCompare()
        {
            FakeGeocoder shadow = FakeGeocoder.Returning(NearbyShadow);
            await using Harness harness = await Harness.StartAsync(FakeGeocoder.Throwing(new HttpRequestException()), shadow);

            await Assert.ThrowsAsync<GeocodingException>(() => harness.Geocoder.GeocodeAsync(Katwilgweg));

            await Task.Delay(200);
            Assert.AreEqual(0, shadow.Calls);
            Assert.IsEmpty(harness.Sink.Comparisons);
        }

        [TestMethod]
        public async Task GeocodeAsync_SampleRateZero_ShouldNotCallShadow()
        {
            FakeGeocoder shadow = FakeGeocoder.Returning(NearbyShadow);
            await using Harness harness = await Harness.StartAsync(FakeGeocoder.Returning(Primary), shadow, o => o.SampleRate = 0);

            for (int i = 0; i < 10; i++)
                await harness.Geocoder.GeocodeAsync(Katwilgweg);

            await Task.Delay(200);
            Assert.AreEqual(0, shadow.Calls);
        }

        [TestMethod]
        public async Task GeocodeAsync_QueueFull_ShouldNotBlockAndCountDropped()
        {
            // The worker is not started, so nothing drains the queue.
            await using Harness harness = await Harness.StartAsync(FakeGeocoder.Returning(Primary), FakeGeocoder.Returning(NearbyShadow), o => o.QueueCapacity = 1, startWorker: false);

            Task all = Task.WhenAll(Enumerable.Range(0, 3).Select(_ => harness.Geocoder.GeocodeAsync(Katwilgweg)));

            Assert.AreSame(all, await Task.WhenAny(all, Task.Delay(Wait)));
            Assert.AreEqual(2, harness.Sink.Dropped);
        }

        [TestMethod]
        public void AddShadowGeocoder_WithoutGeocoders_Throws()
            => Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddTurtleRoute().AddShadowGeocoder(_ => { }));

        [TestMethod]
        public void AddShadowGeocoder_Nested_Throws()
            => Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddTurtleRoute()
                .AddShadowGeocoder(shadow => shadow.AddGeocoder<FakeGeocoder>().AddShadowGeocoder(inner => inner.AddGeocoder<OtherFakeGeocoder>())));

        [TestMethod]
        public void AddShadowGeocoder_Twice_Throws()
        {
            TurtleRouteBuilder builder = new ServiceCollection().AddTurtleRoute().AddShadowGeocoder(shadow => shadow.AddGeocoder<FakeGeocoder>());

            Assert.Throws<InvalidOperationException>(() => builder.AddShadowGeocoder(shadow => shadow.AddGeocoder<OtherFakeGeocoder>()));
        }

        [TestMethod]
        public void AddTurtleRoute_WithoutShadow_ShouldNotRegisterWorker()
        {
            ServiceCollection services = new();
            services.AddTurtleRoute().AddGeocoder<FakeGeocoder>();

            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(IHostedService)));
        }

        private sealed class Harness : IAsyncDisposable
        {
            private readonly ServiceProvider _provider;
            private readonly IHostedService[] _workers;

            private Harness(ServiceProvider provider, IHostedService[] workers, RecordingSink sink)
            {
                _provider = provider;
                _workers = workers;
                Sink = sink;
            }

            public IGeocoder Geocoder => _provider.GetRequiredService<IGeocoder>();

            public RecordingSink Sink { get; }

            public static async Task<Harness> StartAsync(IGeocoder primary, IGeocoder shadow, Action<ShadowGeocodingOptions> configure = null, bool startWorker = true)
            {
                RecordingSink sink = new();
                ServiceCollection services = new();
                services.AddLogging();
                services.AddSingleton<IGeocodingComparisonSink>(sink);
                services.AddTurtleRoute()
                    .AddGeocoder(_ => primary)
                    .AddShadowGeocoder(builder => builder.AddGeocoder(_ => shadow), o =>
                    {
                        o.SampleRate = 1;
                        o.MaxRequestsPerSecond = 0;
                        configure?.Invoke(o);
                    });

                ServiceProvider provider = services.BuildServiceProvider();
                IHostedService[] workers = startWorker ? [.. provider.GetServices<IHostedService>()] : [];
                foreach (IHostedService worker in workers)
                    await worker.StartAsync(default);

                return new Harness(provider, workers, sink);
            }

            public async ValueTask DisposeAsync()
            {
                foreach (IHostedService worker in _workers)
                    await worker.StopAsync(default);

                await _provider.DisposeAsync();
            }
        }

        private sealed class RecordingSink : IGeocodingComparisonSink
        {
            private readonly Channel<GeocodingComparison> _received = Channel.CreateUnbounded<GeocodingComparison>();
            private int _dropped;

            public List<GeocodingComparison> Comparisons { get; } = [];

            public int Dropped => _dropped;

            public Task WriteAsync(GeocodingComparison comparison, CancellationToken cancellationToken)
            {
                lock (Comparisons)
                    Comparisons.Add(comparison);

                _received.Writer.TryWrite(comparison);
                return Task.CompletedTask;
            }

            public void OnDropped() => Interlocked.Increment(ref _dropped);

            public async Task<GeocodingComparison> NextAsync()
            {
                using CancellationTokenSource timeout = new(Wait);
                return await _received.Reader.ReadAsync(timeout.Token);
            }
        }

        private sealed class SlowGeocoder : IGeocoder
        {
            public async Task<GeocodeResult> GeocodeAsync(Address address, CancellationToken cancellationToken = default)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return null;
            }

            public Task<GeocodeResult> GeocodeAsync(string address, string countryFilter, CancellationToken cancellationToken = default)
                => GeocodeAsync((Address)null, cancellationToken);
        }
    }
}
