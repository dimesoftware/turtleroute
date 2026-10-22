using System.Threading;
using System.Threading.Tasks;

namespace TurtleRoute
{
    /// <summary>
    /// Receives the comparisons made in shadow mode. The default, <see cref="ShadowSummarySink"/>, logs an aggregated summary per interval.
    /// </summary>
    public interface IGeocodingComparisonSink
    {
        Task WriteAsync(GeocodingComparison comparison, CancellationToken cancellationToken);

        /// <summary>
        /// Called when a sampled request was dropped because the shadow queue was full.
        /// </summary>
        void OnDropped()
        {
        }
    }
}
