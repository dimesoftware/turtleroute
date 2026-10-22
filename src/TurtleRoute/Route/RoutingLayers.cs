using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TurtleRoute
{
    internal static class RoutingLayers
    {
        /// <summary>
        /// Returns the result of the first layer that does not throw, or throws a <see cref="RoutingException"/> when every layer threw.
        /// </summary>
        /// <remarks>
        /// Invalid input is not passed on to the next layer, as it would reject it too and only cost a transaction.
        /// Neither is a cancellation by the caller.
        /// </remarks>
        public static async Task<TResult> RunAsync<TLayer, TResult>(IReadOnlyList<TLayer> layers, Func<TLayer, Task<TResult>> call, ILogger logger, CancellationToken cancellationToken)
        {
            List<Exception> errors = [];

            foreach (TLayer layer in layers)
            {
                try
                {
                    return await call(layer);
                }
                catch (Exception ex) when (ex is not ArgumentException && !(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
                {
                    logger.LogWarning(ex, "Routing layer {Layer} failed, trying the next layer.", layer.GetType().Name);
                    errors.Add(ex);
                }
            }

            throw new RoutingException("All routing layers failed.", errors);
        }
    }
}
