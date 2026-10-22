using System;
using System.Collections.Generic;

namespace TurtleRoute
{
    /// <summary>
    /// Thrown when every layer of a <see cref="GeocodingPipeline"/> failed, so no layer could say whether the address exists.
    /// </summary>
    public class GeocodingException : AggregateException
    {
        public GeocodingException(string message, IEnumerable<Exception> innerExceptions)
            : base(message, innerExceptions)
        {
        }
    }
}
