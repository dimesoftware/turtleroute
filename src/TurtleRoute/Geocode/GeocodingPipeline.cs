using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TurtleRoute
{
    /// <summary>
    /// Runs geocoders in order and returns the first result that meets <see cref="GeocodingPipelineOptions.MinimumConfidence"/>.
    /// </summary>
    /// <remarks>
    /// A layer is skipped when it throws, finds nothing, or returns a result below the minimum confidence.
    /// The pipeline returns null when no layer produced an acceptable result, and throws a
    /// <see cref="GeocodingException"/> only when every layer threw, because then no layer could tell
    /// whether the address exists.
    /// </remarks>
    public class GeocodingPipeline : IGeocoder
    {
        private readonly IReadOnlyList<IGeocoder> _layers;
        private readonly GeocodingPipelineOptions _options;
        private readonly ILogger<GeocodingPipeline> _logger;

        public GeocodingPipeline(IEnumerable<IGeocoder> layers, GeocodingPipelineOptions options = null, ILogger<GeocodingPipeline> logger = null)
        {
            ArgumentNullException.ThrowIfNull(layers);

            _layers = [.. layers];
            if (_layers.Count == 0)
                throw new ArgumentException("Provide at least one geocoder.", nameof(layers));

            _options = options ?? new GeocodingPipelineOptions();
            _logger = logger ?? NullLogger<GeocodingPipeline>.Instance;
        }

        /// <summary>
        /// The geocoders in the order they are tried.
        /// </summary>
        public IReadOnlyList<IGeocoder> Layers => _layers;

        public Task<GeocodeResult> GeocodeAsync(Address address, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(address);
            return RunAsync(layer => layer.GeocodeAsync(address, cancellationToken), cancellationToken);
        }

        public Task<GeocodeResult> GeocodeAsync(string address, string countryFilter, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentNullException(nameof(address));

            return RunAsync(layer => layer.GeocodeAsync(address, countryFilter, cancellationToken), cancellationToken);
        }

        private async Task<GeocodeResult> RunAsync(Func<IGeocoder, Task<GeocodeResult>> geocode, CancellationToken cancellationToken)
        {
            List<Exception> errors = [];

            foreach (IGeocoder layer in _layers)
            {
                string name = layer.GetType().Name;
                GeocodeResult result;

                try
                {
                    result = await geocode(layer);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
                {
                    _logger.LogWarning(ex, "Geocoder {Geocoder} failed, trying the next layer.", name);
                    errors.Add(ex);
                    continue;
                }

                if (result != null && result.Confidence >= _options.MinimumConfidence)
                    return result;

                _logger.LogDebug("Geocoder {Geocoder} returned no acceptable result ({Confidence}), trying the next layer.", name, result?.Confidence.ToString() ?? "no match");
            }

            if (errors.Count == _layers.Count)
                throw new GeocodingException("All geocoding layers failed.", errors);

            return null;
        }
    }
}