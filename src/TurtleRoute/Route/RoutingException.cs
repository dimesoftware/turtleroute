using System;
using System.Collections.Generic;

namespace TurtleRoute
{
    /// <summary>
    /// Thrown when every layer of a <see cref="RoutePlannerPipeline"/> or <see cref="RouteMatrixPipeline"/> failed.
    /// </summary>
    public class RoutingException : AggregateException
    {
        public RoutingException(string message, IEnumerable<Exception> innerExceptions)
            : base(message, innerExceptions)
        {
        }
    }
}
