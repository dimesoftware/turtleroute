using System;
using System.Collections.Generic;
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
    /// Geocodes live through the DI-registered pipeline, including real failures such as an invalid API key.
    /// Each provider's test project derives from this class.
    /// </summary>
    public abstract class GeocodingPipelineContractTests
    {
        private static readonly Address Katwilgweg = new("Katwilgweg", "2", "2050", "Antwerpen", "", "BE");

        protected abstract string ProviderName { get; }

        /// <summary>
        /// The configured API key. Reading it skips the test when no key is available.
        /// </summary>
        protected abstract string ApiKey { get; }

        protected abstract void AddProvider(TurtleRouteBuilder builder, string apiKey);

        protected abstract IGeocoder CreateGeocoder(string apiKey);

        [TestMethod]
        public async Task AddTurtleRoute_ShouldGeocodeThroughPipeline()
        {
            IGeocoder geocoder = Resolve(builder => AddProvider(builder, ApiKey));

            GeocodeResult result = await geocoder.GeocodeAsync(Katwilgweg);

            AssertKatwilgweg(result);
        }

        [TestMethod]
        public async Task AddTurtleRoute_PrimaryLayerFails_ShouldFallBackToProvider()
        {
            string apiKey = ApiKey;
            IGeocoder geocoder = Resolve(builder =>
            {
                builder.AddGeocoder(_ => new FailingGeocoder());
                AddProvider(builder, apiKey);
            });

            GeocodeResult result = await geocoder.GeocodeAsync("Katwilgweg 2, 2050 Antwerpen", "BE");

            AssertKatwilgweg(result);
        }

        [TestMethod]
        public async Task Pipeline_InvalidApiKeyOnPrimary_ShouldFallBackToValidKey()
        {
            GeocodingPipeline pipeline = new([CreateGeocoder("invalid-key"), CreateGeocoder(ApiKey)]);

            GeocodeResult result = await pipeline.GeocodeAsync(Katwilgweg);

            AssertKatwilgweg(result);
        }

        [TestMethod]
        public async Task Pipeline_InvalidApiKeyOnly_ShouldThrowGeocodingException()
        {
            _ = ApiKey; // Only runs when the provider is configured, as it calls the live API.
            GeocodingPipeline pipeline = new([CreateGeocoder("invalid-key")]);

            await Assert.ThrowsAsync<GeocodingException>(() => pipeline.GeocodeAsync(Katwilgweg));
        }

        [TestMethod]
        public async Task AddTurtleRoute_WrongAddress_ShouldReturnNull()
        {
            IGeocoder geocoder = Resolve(builder => AddProvider(builder, ApiKey));

            GeocodeResult result = await geocoder.GeocodeAsync(new Address("Eenstraatdienietbestaat", "56", "9000", "Sjakkamakka", "", "BE"));

            Assert.IsNull(result, result?.ToString());
        }

        [TestMethod]
        public async Task AddShadowGeocoder_ShouldCompareProviderInBackground()
        {
            string apiKey = ApiKey;
            GeocodeResult primary = new(new GeoCoordinate(51.219501, 4.359231), GeocodeConfidence.High, "fixed");
            ChannelSink sink = new();

            ServiceCollection services = new();
            services.AddLogging();
            services.AddSingleton<IGeocodingComparisonSink>(sink);
            services.AddTurtleRoute()
                .AddGeocoder(_ => new FixedGeocoder(primary))
                .AddShadowGeocoder(shadow => AddProvider(shadow, apiKey), o => o.SampleRate = 1);

            await using ServiceProvider provider = services.BuildServiceProvider();
            IHostedService worker = provider.GetRequiredService<IHostedService>();
            await worker.StartAsync(default);

            try
            {
                GeocodeResult result = await provider.GetRequiredService<IGeocoder>().GeocodeAsync(Katwilgweg);
                Assert.AreSame(primary, result);

                using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
                GeocodingComparison comparison = await sink.Received.Reader.ReadAsync(timeout.Token);

                Assert.IsNull(comparison.Shadow.Error);
                AssertKatwilgweg(comparison.Shadow.Result);
                Assert.IsLessThan(250d, comparison.DistanceInMeters.Value);
            }
            finally
            {
                await worker.StopAsync(default);
            }
        }

        private static IGeocoder Resolve(Action<TurtleRouteBuilder> configure)
        {
            ServiceCollection services = new();
            services.AddTurtleRoute(configure);
            return services.BuildServiceProvider().GetRequiredService<IGeocoder>();
        }

        private void AssertKatwilgweg(GeocodeResult result)
        {
            Assert.IsNotNull(result);
            Assert.AreEqual(ProviderName, result.Provider);
            Assert.AreEqual(51.2195, result.Coordinate.Latitude, 0.01, result.ToString());
            Assert.AreEqual(4.3592, result.Coordinate.Longitude, 0.01, result.ToString());
        }

        private class FixedGeocoder : IGeocoder
        {
            private readonly GeocodeResult _result;

            public FixedGeocoder(GeocodeResult result)
            {
                _result = result;
            }

            public Task<GeocodeResult> GeocodeAsync(Address address, CancellationToken cancellationToken = default) => Task.FromResult(_result);

            public Task<GeocodeResult> GeocodeAsync(string address, string countryFilter, CancellationToken cancellationToken = default) => Task.FromResult(_result);
        }

        private class ChannelSink : IGeocodingComparisonSink
        {
            public Channel<GeocodingComparison> Received { get; } = Channel.CreateUnbounded<GeocodingComparison>();

            public Task WriteAsync(GeocodingComparison comparison, CancellationToken cancellationToken)
            {
                Received.Writer.TryWrite(comparison);
                return Task.CompletedTask;
            }
        }

        private class FailingGeocoder : IGeocoder
        {
            public Task<GeocodeResult> GeocodeAsync(Address address, CancellationToken cancellationToken = default)
                => throw new HttpRequestException("Simulated outage");

            public Task<GeocodeResult> GeocodeAsync(string address, string countryFilter, CancellationToken cancellationToken = default)
                => throw new HttpRequestException("Simulated outage");
        }
    }
}
