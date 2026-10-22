using System;
using System.Threading;
using System.Threading.Tasks;

namespace TurtleRoute.Tests
{
    internal class FakeGeocoder : IGeocoder
    {
        private readonly Func<GeocodeResult> _geocode;

        public FakeGeocoder() : this(() => null)
        {
        }

        private FakeGeocoder(Func<GeocodeResult> geocode)
        {
            _geocode = geocode;
        }

        public int Calls { get; private set; }

        public static FakeGeocoder Returning(GeocodeResult result) => new(() => result);

        public static FakeGeocoder Returning(GeocodeConfidence confidence, string provider)
            => new(() => new GeocodeResult(new GeoCoordinate(51.219501, 4.359231), confidence, provider));

        public static FakeGeocoder Throwing(Exception exception) => new(() => throw exception);

        public Task<GeocodeResult> GeocodeAsync(Address address, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(_geocode());
        }

        public Task<GeocodeResult> GeocodeAsync(string address, string countryFilter, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(_geocode());
        }
    }

    internal class OtherFakeGeocoder : FakeGeocoder
    {
    }
}
