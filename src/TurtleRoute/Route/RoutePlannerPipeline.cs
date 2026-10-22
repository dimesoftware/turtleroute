using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TurtleRoute
{
    /// <summary>
    /// Runs route planners in order and returns the route of the first one that does not throw.
    /// </summary>
    /// <remarks>
    /// A layer is skipped when it throws, e.g. during an outage, with an invalid key, when the quota is exceeded or when it found no route.
    /// Invalid input is not retried. The pipeline throws a <see cref="RoutingException"/> when every layer threw.
    /// </remarks>
    public class RoutePlannerPipeline : IRoutePlanner
    {
        private readonly IReadOnlyList<IRoutePlanner> _layers;
        private readonly ILogger<RoutePlannerPipeline> _logger;

        public RoutePlannerPipeline(IEnumerable<IRoutePlanner> layers, ILogger<RoutePlannerPipeline> logger = null)
        {
            ArgumentNullException.ThrowIfNull(layers);

            _layers = [.. layers];
            if (_layers.Count == 0)
                throw new ArgumentException("Provide at least one route planner.", nameof(layers));

            _logger = logger ?? NullLogger<RoutePlannerPipeline>.Instance;
        }

        /// <summary>
        /// The route planners in the order they are tried.
        /// </summary>
        public IReadOnlyList<IRoutePlanner> Layers => _layers;

        public Task<Route> GetRouteAsync(IEnumerable<GeoCoordinate> coordinates, RoutePlanOptions options = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(coordinates);

            // Materialized once, so every layer gets the same stops even when the sequence is lazy.
            List<GeoCoordinate> stops = [.. coordinates];
            return RoutingLayers.RunAsync(_layers, layer => layer.GetRouteAsync(stops, options, cancellationToken), _logger, cancellationToken);
        }
    }
}
