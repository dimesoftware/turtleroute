using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TurtleRoute
{
    /// <summary>
    /// Runs route matrices in order and returns the matrix of the first one that does not throw.
    /// </summary>
    /// <remarks>
    /// A layer is skipped when it throws, e.g. during an outage, with an invalid key or when the quota is exceeded.
    /// Invalid input is not retried. The pipeline throws a <see cref="RoutingException"/> when every layer threw.
    /// </remarks>
    public class RouteMatrixPipeline : IRouteMatrix
    {
        private readonly IReadOnlyList<IRouteMatrix> _layers;
        private readonly ILogger<RouteMatrixPipeline> _logger;
        private readonly LayerFailureLog _failureLog;

        public RouteMatrixPipeline(IEnumerable<IRouteMatrix> layers, ILogger<RouteMatrixPipeline> logger = null, LayerFailureLog failureLog = null)
        {
            ArgumentNullException.ThrowIfNull(layers);

            _layers = [.. layers];
            if (_layers.Count == 0)
                throw new ArgumentException("Provide at least one route matrix.", nameof(layers));

            _logger = logger ?? NullLogger<RouteMatrixPipeline>.Instance;
            _failureLog = failureLog ?? new LayerFailureLog();
        }

        /// <summary>
        /// The route matrices in the order they are tried.
        /// </summary>
        public IReadOnlyList<IRouteMatrix> Layers => _layers;

        public Task<Route[,]> GetRouteMatrixAsync(IEnumerable<GeoCoordinate> coordinates, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(coordinates);

            List<GeoCoordinate> points = [.. coordinates];
            return RoutingLayers.RunAsync(_layers, layer => layer.GetRouteMatrixAsync(points, cancellationToken), _logger, _failureLog, cancellationToken);
        }
    }
}
