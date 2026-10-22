using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace TurtleRoute
{
    /// <summary>
    /// Returns the primary geocoder's result unchanged, and queues a sample of requests to be repeated by the shadow geocoder in the background.
    /// </summary>
    internal class ShadowGeocoder : IGeocoder
    {
        private readonly IGeocoder _primary;
        private readonly ShadowQueue _queue;
        private readonly ShadowGeocodingOptions _options;

        public ShadowGeocoder(IGeocoder primary, ShadowQueue queue, ShadowGeocodingOptions options)
        {
            _primary = primary;
            _queue = queue;
            _options = options;
        }

        public async Task<GeocodeResult> GeocodeAsync(Address address, CancellationToken cancellationToken = default)
        {
            long started = Stopwatch.GetTimestamp();
            GeocodeResult result = await _primary.GeocodeAsync(address, cancellationToken);

            Sample(address?.ToString(), address?.Country, (geocoder, ct) => geocoder.GeocodeAsync(address, ct), result, Stopwatch.GetElapsedTime(started));
            return result;
        }

        public async Task<GeocodeResult> GeocodeAsync(string address, string countryFilter, CancellationToken cancellationToken = default)
        {
            long started = Stopwatch.GetTimestamp();
            GeocodeResult result = await _primary.GeocodeAsync(address, countryFilter, cancellationToken);

            Sample(address, countryFilter, (geocoder, ct) => geocoder.GeocodeAsync(address, countryFilter, ct), result, Stopwatch.GetElapsedTime(started));
            return result;
        }

        private void Sample(string query, string country, Func<IGeocoder, CancellationToken, Task<GeocodeResult>> geocode, GeocodeResult result, TimeSpan duration)
        {
            if (_options.SampleRate <= 0 || Random.Shared.NextDouble() >= _options.SampleRate)
                return;

            _queue.Enqueue(new ShadowRequest
            {
                Timestamp = DateTimeOffset.UtcNow,
                Query = query,
                Country = country,
                Primary = new GeocodingAttempt { Provider = result?.Provider, Result = result, Duration = duration },
                Geocode = geocode
            });
        }
    }
}
