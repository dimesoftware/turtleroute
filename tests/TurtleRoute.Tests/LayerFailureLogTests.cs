using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TurtleRoute.Tests
{
    [TestClass]
    public class LayerFailureLogTests
    {
        private CapturingLogger<LayerFailureLogTests> _logger;
        private ManualTimeProvider _time;
        private LayerFailureLog _log;

        [TestInitialize]
        public void Setup()
        {
            _logger = new CapturingLogger<LayerFailureLogTests>();
            _time = new ManualTimeProvider();
            _log = new LayerFailureLog(TimeSpan.FromMinutes(5), _time);
        }

        [TestMethod]
        public void Failed_First_ShouldLogWarningWithException()
        {
            _log.Failed(_logger, "OpenCageGeocoder", 0, new HttpRequestException("503"));

            LogEntry entry = _logger.Entries.Single();
            Assert.AreEqual(LogLevel.Warning, entry.Level);
            Assert.AreEqual("OpenCageGeocoder", entry.Properties["Layer"]);
        }

        [TestMethod]
        public void Failed_RepeatedWithinInterval_ShouldLogOnce()
        {
            for (int i = 0; i < 100; i++)
            {
                _log.Failed(_logger, "OpenCageGeocoder", 0, new HttpRequestException("503"));
                _time.Advance(TimeSpan.FromSeconds(1));
            }

            Assert.HasCount(1, _logger.Entries);
        }

        [TestMethod]
        public void Failed_AfterInterval_ShouldLogOneSummaryWithCountAndLastError()
        {
            _log.Failed(_logger, "OpenCageGeocoder", 0, new HttpRequestException("503"));
            for (int i = 0; i < 12; i++)
            {
                _time.Advance(TimeSpan.FromMinutes(1));
                _log.Failed(_logger, "OpenCageGeocoder", 0, new HttpRequestException(i == 9 ? "429 rate limited" : "503"));
            }

            // Summaries after 5 and 10 minutes; the 2 failures since are not logged yet.
            LogEntry[] entries = [.. _logger.Entries];
            Assert.HasCount(3, entries);
            Assert.AreEqual(5, entries[1].Properties["Failures"]);
            Assert.AreEqual(5, entries[2].Properties["Failures"]);
            Assert.AreEqual("429 rate limited", entries[2].Properties["LastError"]);
        }

        [TestMethod]
        public void Succeeded_AfterFailures_ShouldLogRecoveryOnceAndStartOver()
        {
            _log.Failed(_logger, "OpenCageGeocoder", 0, new HttpRequestException("503"));
            _log.Failed(_logger, "OpenCageGeocoder", 0, new HttpRequestException("503"));
            _time.Advance(TimeSpan.FromMinutes(2));

            _log.Succeeded(_logger, "OpenCageGeocoder", 0);
            _log.Succeeded(_logger, "OpenCageGeocoder", 0);

            LogEntry recovery = _logger.Entries.Single(e => e.Level == LogLevel.Information);
            Assert.AreEqual(2, recovery.Properties["Failures"]);
            Assert.AreEqual(TimeSpan.FromMinutes(2), recovery.Properties["Duration"]);

            _log.Failed(_logger, "OpenCageGeocoder", 0, new HttpRequestException("503"));
            Assert.AreEqual(2, _logger.Entries.Count(e => e.Level == LogLevel.Warning && e.Properties.ContainsKey("Interval")));
        }

        [TestMethod]
        public void Succeeded_WithoutFailures_ShouldLogNothing()
        {
            _log.Succeeded(_logger, "OpenCageGeocoder", 0);

            Assert.IsEmpty(_logger.Entries);
        }

        [TestMethod]
        public void Succeeded_SameTypeAtAnotherPosition_ShouldNotRecoverTheFailingLayer()
        {
            _log.Failed(_logger, "OpenCageGeocoder", 0, new HttpRequestException("401"));
            _log.Succeeded(_logger, "OpenCageGeocoder", 1);
            _log.Failed(_logger, "OpenCageGeocoder", 0, new HttpRequestException("401"));

            Assert.HasCount(1, _logger.Entries);
        }

        [TestMethod]
        public void Failed_DifferentLayers_ShouldBeTrackedSeparately()
        {
            _log.Failed(_logger, "OpenCageGeocoder", 0, new HttpRequestException("503"));
            _log.Failed(_logger, "AzureGeocoder", 1, new HttpRequestException("503"));

            Assert.HasCount(2, _logger.Entries);
        }

        [TestMethod]
        public async Task GeocodingPipeline_Outage_ShouldLogOneWarningAndARecovery()
        {
            CapturingLogger<GeocodingPipeline> logger = new();
            ToggleGeocoder primary = new() { Failing = true };
            IGeocoder fallback = FakeGeocoder.Returning(GeocodeConfidence.High, "fallback");

            for (int i = 0; i < 50; i++)
                await new GeocodingPipeline([primary, fallback], logger: logger, failureLog: _log).GeocodeAsync("Katwilgweg 2", "BE");

            primary.Failing = false;
            await new GeocodingPipeline([primary, fallback], logger: logger, failureLog: _log).GeocodeAsync("Katwilgweg 2", "BE");

            Assert.AreEqual(1, logger.Entries.Count(e => e.Level == LogLevel.Warning));
            Assert.AreEqual(50, logger.Entries.Single(e => e.Level == LogLevel.Information).Properties["Failures"]);
        }

        [TestMethod]
        public async Task RoutePlannerPipeline_Outage_ShouldLogOneWarning()
        {
            CapturingLogger<RoutePlannerPipeline> logger = new();
            RoutingPipelineTests.FakeRouting primary = RoutingPipelineTests.FakeRouting.Throwing(new HttpRequestException("503"));
            RoutingPipelineTests.FakeRouting fallback = RoutingPipelineTests.FakeRouting.Returning(1000);
            GeoCoordinate antwerp = new(51.219501, 4.359231);
            GeoCoordinate ghent = new(51.054342, 3.717424);

            for (int i = 0; i < 50; i++)
                await new RoutePlannerPipeline([primary, fallback], logger, _log).GetRouteAsync(antwerp, ghent);

            Assert.AreEqual(1, logger.Entries.Count(e => e.Level == LogLevel.Warning));
            Assert.AreEqual(50, fallback.Calls);
        }

        [TestMethod]
        public void AddTurtleRoute_ShouldShareOneFailureLogAndKeepARegisteredOne()
        {
            LayerFailureLog custom = new(TimeSpan.FromMinutes(1));

            ServiceCollection services = new();
            services.AddSingleton(custom);
            services.AddTurtleRoute().AddGeocoder<FakeGeocoder>();

            using ServiceProvider provider = services.BuildServiceProvider();

            Assert.AreSame(custom, provider.GetRequiredService<LayerFailureLog>());
        }

        private class ToggleGeocoder : IGeocoder
        {
            public bool Failing { get; set; }

            public Task<GeocodeResult> GeocodeAsync(Address address, CancellationToken cancellationToken = default)
                => GeocodeAsync(address.ToString(), null, cancellationToken);

            public Task<GeocodeResult> GeocodeAsync(string address, string countryFilter, CancellationToken cancellationToken = default)
                => Failing ? throw new HttpRequestException("503") : Task.FromResult<GeocodeResult>(null);
        }

        private class ManualTimeProvider : TimeProvider
        {
            private DateTimeOffset _now = new(2026, 10, 22, 8, 0, 0, TimeSpan.Zero);

            public override DateTimeOffset GetUtcNow() => _now;

            public void Advance(TimeSpan by) => _now += by;
        }
    }
}
