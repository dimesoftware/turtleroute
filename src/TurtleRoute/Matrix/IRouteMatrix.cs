using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TurtleRoute
{
    public interface IRouteMatrix
    {
        /// <summary>
        /// Calculates the route from every coordinate to every other coordinate.
        /// </summary>
        Task<Route[,]> GetRouteMatrixAsync(IEnumerable<GeoCoordinate> coordinates, CancellationToken cancellationToken = default);
    }
}
