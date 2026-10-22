using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace TurtleRoute
{
    internal class ShadowRequest
    {
        public DateTimeOffset Timestamp { get; init; }
        public string Query { get; init; }
        public string Country { get; init; }
        public GeocodingAttempt Primary { get; init; }
        public Func<IGeocoder, CancellationToken, Task<GeocodeResult>> Geocode { get; init; }
    }

    /// <summary>
    /// Hands sampled requests from <see cref="ShadowGeocoder"/> to <see cref="ShadowGeocodingWorker"/> without ever blocking the caller.
    /// </summary>
    internal class ShadowQueue
    {
        private readonly Channel<ShadowRequest> _channel;
        private readonly IGeocodingComparisonSink _sink;

        public ShadowQueue(IOptions<ShadowGeocodingOptions> options, IGeocodingComparisonSink sink)
        {
            _channel = Channel.CreateBounded<ShadowRequest>(new BoundedChannelOptions(Math.Max(1, options.Value.QueueCapacity))
            {
                FullMode = BoundedChannelFullMode.Wait, // With Wait, TryWrite returns false when full instead of dropping silently.
                SingleReader = true
            });
            _sink = sink;
        }

        public ChannelReader<ShadowRequest> Reader => _channel.Reader;

        public void Enqueue(ShadowRequest request)
        {
            if (!_channel.Writer.TryWrite(request))
                _sink.OnDropped();
        }
    }
}
