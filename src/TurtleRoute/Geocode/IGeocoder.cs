using System.Threading;
using System.Threading.Tasks;

namespace TurtleRoute
{
    public interface IGeocoder
    {
        /// <summary>
        /// Geocode a structured address. Returns null when nothing was found.
        /// </summary>
        /// <remarks>
        /// Providers return their best candidate with its confidence and leave filtering to the caller,
        /// which is what <see cref="GeocodingPipeline"/> does. Providers throw when the request itself fails
        /// (outage, invalid key, quota exceeded), so that a pipeline can fall back to the next layer.
        /// </remarks>
        Task<GeocodeResult> GeocodeAsync(Address address, CancellationToken cancellationToken = default);

        /// <summary>
        /// Geocode a freeform address with an optional country filter. Returns null when nothing was found.
        /// </summary>
        Task<GeocodeResult> GeocodeAsync(string address, string countryFilter, CancellationToken cancellationToken = default);
    }
}
