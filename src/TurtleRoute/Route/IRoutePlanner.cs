using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TurtleRoute
{
    public interface IRoutePlanner
    {
        Task<Route> GetRouteAsync(IEnumerable<GeoCoordinate> coordinates, RoutePlanOptions options = null, CancellationToken cancellationToken = default);
    }
}
