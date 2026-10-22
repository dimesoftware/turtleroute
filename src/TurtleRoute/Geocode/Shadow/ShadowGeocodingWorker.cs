using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TurtleRoute
{
    /// <summary>
    /// Repeats queued requests against the shadow geocoder, one at a time and within its rate limit, and writes each comparison to the sink.
    /// </summary>
    internal class ShadowGeocodingWorker : BackgroundService
    {
        private readonly ShadowQueue _queue;
        private readonly IServiceProvider _serviceProvider;
        private readonly TurtleRouteBuilder _builder;
        private readonly IGeocodingComparisonSink _sink;
        private readonly ShadowGeocodingOptions _options;
        private readonly GeocodingPipelineOptions _pipelineOptions;
        private readonly ILogger<ShadowGeocodingWorker> _logger;

        public ShadowGeocodingWorker(
            ShadowQueue queue,
            IServiceProvider serviceProvider,
            TurtleRouteBuilder builder,
            IGeocodingComparisonSink sink,
            IOptions<ShadowGeocodingOptions> options,
            IOptions<GeocodingPipelineOptions> pipelineOptions,
            ILogger<ShadowGeocodingWorker> logger)
        {
            _queue = queue;
            _serviceProvider = serviceProvider;
            _builder = builder;
            _sink = sink;
            _options = options.Value;
            _pipelineOptions = pipelineOptions.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            TimeSpan spacing = _options.MaxRequestsPerSecond > 0 ? TimeSpan.FromSeconds(1 / _options.MaxRequestsPerSecond) : TimeSpan.Zero;

            await foreach (ShadowRequest request in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                long started = Stopwatch.GetTimestamp();
                await CompareAsync(request, stoppingToken);

                TimeSpan remaining = spacing - Stopwatch.GetElapsedTime(started);
                if (remaining > TimeSpan.Zero)
                    await Task.Delay(remaining, stoppingToken);
            }
        }

        private async Task CompareAsync(ShadowRequest request, CancellationToken stoppingToken)
        {
            GeocodeResult result = null;
            string error = null;
            long started = Stopwatch.GetTimestamp();

            using (CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken))
            {
                timeout.CancelAfter(_options.Timeout);

                try
                {
                    result = await request.Geocode(CreateShadowGeocoder(), timeout.Token);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    // Only the type: exception messages can contain request details.
                    error = ex is OperationCanceledException ? "Timeout" : ex.GetType().Name;
                }
            }

            GeocodeResult primary = request.Primary.Result;
            GeocodingComparison comparison = new()
            {
                Timestamp = request.Timestamp,
                Query = request.Query,
                Country = request.Country,
                Primary = request.Primary,
                Shadow = new GeocodingAttempt { Provider = result?.Provider, Result = result, Duration = Stopwatch.GetElapsedTime(started), Error = error },
                DistanceInMeters = primary != null && result != null ? GeoMath.DistanceInMeters(primary.Coordinate, result.Coordinate) : null,
                ShadowAccepted = result != null && result.Confidence >= _pipelineOptions.MinimumConfidence
            };

            try
            {
                await _sink.WriteAsync(comparison, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Writing a geocoding comparison failed.");
            }
        }

        // Resolved per request, so layers built on typed HttpClients get a fresh handler from IHttpClientFactory.
        private IGeocoder CreateShadowGeocoder()
        {
            List<IGeocoder> layers = [.. _builder.Shadow.Geocoders.Select(factory => factory(_serviceProvider))];

            // The shadow's raw result is compared, so nothing is filtered on confidence here.
            return layers.Count == 1 ? layers[0] : new GeocodingPipeline(layers, new GeocodingPipelineOptions { MinimumConfidence = GeocodeConfidence.Low });
        }
    }
}
