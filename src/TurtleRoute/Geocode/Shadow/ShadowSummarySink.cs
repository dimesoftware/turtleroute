using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TurtleRoute
{
    /// <summary>
    /// Aggregates comparisons in memory and logs one summary per <see cref="ShadowGeocodingOptions.SummaryInterval"/>,
    /// plus at most <see cref="ShadowGeocodingOptions.MaxDisagreementsPerInterval"/> individual disagreements.
    /// Nothing is logged for an interval without traffic. The current interval is lost when the process stops abruptly.
    /// </summary>
    public class ShadowSummarySink : IGeocodingComparisonSink, IDisposable
    {
        // Upper bounds in metres; distances beyond the last one fall into an overflow bucket.
        private static readonly double[] Buckets = [10, 25, 50, 100, 250, 500, 1000, 2500, 5000];

        // Country values are free text, so the number tracked per interval is capped.
        private const int MaxTrackedCountries = 100;
        private const string OtherCountries = "other";

        private readonly object _lock = new();
        private readonly ILogger<ShadowSummarySink> _logger;
        private readonly ShadowGeocodingOptions _options;
        private readonly Timer _timer;
        private Interval _current = new();

        public ShadowSummarySink(ILogger<ShadowSummarySink> logger, IOptions<ShadowGeocodingOptions> options)
        {
            _logger = logger;
            _options = options.Value;

            if (_options.SummaryInterval > TimeSpan.Zero && _options.SummaryInterval != Timeout.InfiniteTimeSpan)
                _timer = new Timer(_ => Flush(), null, _options.SummaryInterval, _options.SummaryInterval);
        }

        public Task WriteAsync(GeocodingComparison comparison, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(comparison);

            bool disagreement = comparison.ShadowAccepted && comparison.DistanceInMeters > _options.DisagreementThresholdInMeters;
            bool logDisagreement;

            lock (_lock)
            {
                _current.Add(comparison);

                if (disagreement)
                    _current.Disagreements++;

                logDisagreement = disagreement && _current.Disagreements <= _options.MaxDisagreementsPerInterval;
            }

            if (logDisagreement)
            {
                _logger.LogWarning(
                    "Geocoding disagreement for {Query}: {PrimaryProvider} {PrimaryCoordinate} ({PrimaryConfidence}) vs {ShadowProvider} {ShadowCoordinate} ({ShadowConfidence}), {DistanceInMeters:F0} m",
                    comparison.Query,
                    comparison.Primary.Provider, comparison.Primary.Result?.Coordinate, comparison.Primary.Result?.Confidence,
                    comparison.Shadow.Provider, comparison.Shadow.Result?.Coordinate, comparison.Shadow.Result?.Confidence,
                    comparison.DistanceInMeters);
            }

            return Task.CompletedTask;
        }

        public void OnDropped()
        {
            lock (_lock)
                _current.Dropped++;
        }

        /// <summary>
        /// Logs the summary of the current interval and starts a new one. Called by the timer; public for tests and for flushing on demand.
        /// </summary>
        public void Flush()
        {
            Interval interval;
            lock (_lock)
            {
                interval = _current;
                _current = new Interval();
            }

            if (interval.Comparisons == 0 && interval.Dropped == 0)
                return;

            int measured = interval.Measured;

            _logger.LogInformation(
                "Geocoding shadow summary {ShadowProvider} vs {PrimaryProvider} over {IntervalMinutes:F0} min: {Comparisons} comparisons, " +
                "accepted {AcceptedRate:P1}, within 50 m {Within50m:P1}, within 250 m {Within250m:P1}, within 1 km {Within1km:P1}, " +
                "p50 {P50Meters:F0} m, p90 {P90Meters:F0} m, max {MaxMeters:F0} m, not found {ShadowNotFoundRate:P1}, errors {ShadowErrorRate:P1}, " +
                "disagreements {Disagreements}, dropped {Dropped}, {PrimaryAverageMs:F0} ms vs {ShadowAverageMs:F0} ms, countries: {Countries}",
                interval.ShadowProvider ?? "shadow", interval.PrimaryProvider ?? "primary", (DateTimeOffset.UtcNow - interval.Started).TotalMinutes, interval.Comparisons,
                Rate(interval.Accepted, interval.Comparisons), Rate(interval.CountWithin(50), measured), Rate(interval.CountWithin(250), measured), Rate(interval.CountWithin(1000), measured),
                interval.Percentile(0.5), interval.Percentile(0.9), interval.MaxDistance, Rate(interval.ShadowNotFound, interval.Comparisons), Rate(interval.ShadowErrors, interval.Comparisons),
                interval.Disagreements, interval.Dropped, Average(interval.PrimaryMilliseconds, interval.Comparisons), Average(interval.ShadowMilliseconds, interval.Comparisons),
                FormatCountries(interval));
        }

        public void Dispose()
        {
            _timer?.Dispose();
            Flush();
            GC.SuppressFinalize(this);
        }

        private string FormatCountries(Interval interval)
        {
            IEnumerable<KeyValuePair<string, CountryStats>> ordered = interval.Countries.OrderByDescending(c => c.Value.Comparisons);
            List<KeyValuePair<string, CountryStats>> top = [.. ordered.Take(_options.MaxCountries)];

            CountryStats rest = new();
            foreach (KeyValuePair<string, CountryStats> country in ordered.Skip(_options.MaxCountries))
            {
                rest.Comparisons += country.Value.Comparisons;
                rest.Accepted += country.Value.Accepted;
            }

            if (rest.Comparisons > 0)
                top.Add(new(OtherCountries, rest));

            return string.Join(", ", top.Select(c => $"{c.Key} {c.Value.Comparisons} ({Rate(c.Value.Accepted, c.Value.Comparisons):P0} accepted)"));
        }

        private static double Rate(int count, int total) => total == 0 ? 0 : (double)count / total;

        private static double Average(double total, int count) => count == 0 ? 0 : total / count;

        private class CountryStats
        {
            public int Comparisons;
            public int Accepted;
        }

        private class Interval
        {
            private readonly int[] _buckets = new int[Buckets.Length + 1];

            public DateTimeOffset Started { get; } = DateTimeOffset.UtcNow;
            public string PrimaryProvider;
            public string ShadowProvider;
            public int Comparisons;
            public int Accepted;
            public int ShadowNotFound;
            public int ShadowErrors;
            public int Measured;
            public int Disagreements;
            public int Dropped;
            public double MaxDistance;
            public double PrimaryMilliseconds;
            public double ShadowMilliseconds;
            public Dictionary<string, CountryStats> Countries { get; } = new(StringComparer.OrdinalIgnoreCase);

            public void Add(GeocodingComparison comparison)
            {
                Comparisons++;
                PrimaryProvider = comparison.Primary.Provider ?? PrimaryProvider;
                ShadowProvider = comparison.Shadow.Provider ?? ShadowProvider;
                PrimaryMilliseconds += comparison.Primary.Duration.TotalMilliseconds;
                ShadowMilliseconds += comparison.Shadow.Duration.TotalMilliseconds;

                if (comparison.ShadowAccepted)
                    Accepted++;

                if (comparison.Shadow.Error != null)
                    ShadowErrors++;
                else if (comparison.Shadow.Result == null)
                    ShadowNotFound++;

                if (comparison.DistanceInMeters is double distance)
                {
                    Measured++;
                    MaxDistance = Math.Max(MaxDistance, distance);

                    int bucket = Array.FindIndex(Buckets, upper => distance <= upper);
                    _buckets[bucket < 0 ? Buckets.Length : bucket]++;
                }

                CountryStats country = GetCountry(comparison.Country);
                country.Comparisons++;
                if (comparison.ShadowAccepted)
                    country.Accepted++;
            }

            public int CountWithin(double meters)
                => Enumerable.Range(0, Buckets.Length).Where(i => Buckets[i] <= meters).Sum(i => _buckets[i]);

            /// <summary>
            /// The upper bound of the bucket holding the percentile, or the maximum distance when it falls beyond the last bucket.
            /// </summary>
            public double Percentile(double percentile)
            {
                if (Measured == 0)
                    return 0;

                int target = (int)Math.Ceiling(percentile * Measured);
                int cumulative = 0;
                for (int i = 0; i < Buckets.Length; i++)
                {
                    cumulative += _buckets[i];
                    if (cumulative >= target)
                        return Math.Min(Buckets[i], MaxDistance);
                }

                return MaxDistance;
            }

            private CountryStats GetCountry(string country)
            {
                string key = string.IsNullOrWhiteSpace(country) ? "unknown" : country.Trim().ToUpperInvariant();
                if (!Countries.ContainsKey(key) && Countries.Count >= MaxTrackedCountries)
                    key = OtherCountries;

                if (!Countries.TryGetValue(key, out CountryStats stats))
                    Countries[key] = stats = new CountryStats();

                return stats;
            }
        }
    }
}
