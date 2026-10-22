using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TurtleRoute.Tests
{
    [TestClass]
    public class ShadowSummarySinkTests
    {
        private static readonly GeoCoordinate Origin = new(51.219501, 4.359231);

        private CapturingLogger<ShadowSummarySink> _logger;

        [TestInitialize]
        public void Setup() => _logger = new CapturingLogger<ShadowSummarySink>();

        [TestMethod]
        public void Flush_NoTraffic_ShouldLogNothing()
        {
            using ShadowSummarySink sink = Create();

            sink.Flush();

            Assert.IsEmpty(_logger.Entries);
        }

        [TestMethod]
        public async Task Flush_ShouldLogOneSummaryWithRatesAndDistances()
        {
            using ShadowSummarySink sink = Create();

            foreach (double meters in new[] { 5d, 30, 200, 1500 })
                await sink.WriteAsync(Comparison(meters, accepted: meters < 1000), default);
            await sink.WriteAsync(NotFound(), default);
            await sink.WriteAsync(Failed(), default);

            sink.Flush();

            LogEntry summary = _logger.Entries.Single(e => e.Level == LogLevel.Information);
            Assert.AreEqual(6, summary.Properties["Comparisons"]);
            Assert.AreEqual(3d / 6, (double)summary.Properties["AcceptedRate"], 0.0001);
            Assert.AreEqual(2d / 4, (double)summary.Properties["Within50m"], 0.0001);
            Assert.AreEqual(3d / 4, (double)summary.Properties["Within250m"], 0.0001);
            Assert.AreEqual(3d / 4, (double)summary.Properties["Within1km"], 0.0001);
            Assert.AreEqual(50d, summary.Properties["P50Meters"]);
            Assert.AreEqual(1500d, (double)summary.Properties["P90Meters"], 1);
            Assert.AreEqual(1500d, (double)summary.Properties["MaxMeters"], 1);
            Assert.AreEqual(1d / 6, (double)summary.Properties["ShadowNotFoundRate"], 0.0001);
            Assert.AreEqual(1d / 6, (double)summary.Properties["ShadowErrorRate"], 0.0001);
            Assert.AreEqual("OpenCage", summary.Properties["ShadowProvider"]);
            Assert.AreEqual("AzureMaps", summary.Properties["PrimaryProvider"]);
        }

        [TestMethod]
        public async Task Flush_ShouldStartANewInterval()
        {
            using ShadowSummarySink sink = Create();
            await sink.WriteAsync(Comparison(5, accepted: true), default);

            sink.Flush();
            sink.Flush();

            Assert.AreEqual(1, _logger.Entries.Count(e => e.Level == LogLevel.Information));
        }

        [TestMethod]
        public async Task WriteAsync_Disagreements_ShouldLogOnlyUpToTheCapButCountAll()
        {
            using ShadowSummarySink sink = Create(o => o.MaxDisagreementsPerInterval = 2);

            for (int i = 0; i < 3; i++)
                await sink.WriteAsync(Comparison(1200, accepted: true), default);
            sink.Flush();

            LogEntry[] warnings = [.. _logger.Entries.Where(e => e.Level == LogLevel.Warning)];
            Assert.HasCount(2, warnings);
            StringAssert.Contains(warnings[0].Message, "Katwilgweg");
            Assert.AreEqual(3, _logger.Entries.Single(e => e.Level == LogLevel.Information).Properties["Disagreements"]);
        }

        [TestMethod]
        public async Task WriteAsync_FarButRejectedShadowResult_ShouldNotBeADisagreement()
        {
            using ShadowSummarySink sink = Create();

            await sink.WriteAsync(Comparison(1200, accepted: false), default);
            sink.Flush();

            Assert.IsFalse(_logger.Entries.Any(e => e.Level == LogLevel.Warning));
            Assert.AreEqual(0, _logger.Entries.Single().Properties["Disagreements"]);
        }

        [TestMethod]
        public void OnDropped_ShouldBeCountedInSummary()
        {
            using ShadowSummarySink sink = Create();

            sink.OnDropped();
            sink.OnDropped();
            sink.Flush();

            Assert.AreEqual(2, _logger.Entries.Single().Properties["Dropped"]);
        }

        [TestMethod]
        public async Task Flush_ShouldBreakDownTopCountriesAndGroupTheRest()
        {
            using ShadowSummarySink sink = Create(o => o.MaxCountries = 2);

            foreach (string country in new[] { "BE", "be", "BE", "DE", "DE", "FR", "NL" })
                await sink.WriteAsync(Comparison(5, accepted: true, country), default);
            sink.Flush();

            Assert.AreEqual("BE 3 (100% accepted), DE 2 (100% accepted), other 2 (100% accepted)", _logger.Entries.Single().Properties["Countries"]);
        }

        [TestMethod]
        public async Task Dispose_ShouldFlushTheCurrentInterval()
        {
            ShadowSummarySink sink = Create();
            await sink.WriteAsync(Comparison(5, accepted: true), default);

            sink.Dispose();

            Assert.HasCount(1, _logger.Entries);
        }

        private ShadowSummarySink Create(Action<ShadowGeocodingOptions> configure = null)
        {
            ShadowGeocodingOptions options = new() { SummaryInterval = Timeout.InfiniteTimeSpan };
            configure?.Invoke(options);
            return new ShadowSummarySink(_logger, Options.Create(options));
        }

        private static GeocodingComparison Comparison(double meters, bool accepted, string country = "BE")
        {
            // Roughly 111,320 m per degree of latitude.
            GeoCoordinate shadow = new(Origin.Latitude + meters / 111320, Origin.Longitude);

            return new GeocodingComparison
            {
                Query = "Katwilgweg, 2, 2050, Antwerpen, BE",
                Country = country,
                Primary = Attempt("AzureMaps", Origin, GeocodeConfidence.High),
                Shadow = Attempt("OpenCage", shadow, accepted ? GeocodeConfidence.High : GeocodeConfidence.Low),
                DistanceInMeters = meters,
                ShadowAccepted = accepted
            };
        }

        private static GeocodingComparison NotFound() => new()
        {
            Country = "BE",
            Primary = Attempt("AzureMaps", Origin, GeocodeConfidence.High),
            Shadow = new GeocodingAttempt()
        };

        private static GeocodingComparison Failed() => new()
        {
            Country = "BE",
            Primary = Attempt("AzureMaps", Origin, GeocodeConfidence.High),
            Shadow = new GeocodingAttempt { Error = "HttpRequestException" }
        };

        private static GeocodingAttempt Attempt(string provider, GeoCoordinate coordinate, GeocodeConfidence confidence)
            => new() { Provider = provider, Result = new GeocodeResult(coordinate, confidence, provider), Duration = TimeSpan.FromMilliseconds(100) };
    }
}
