using System;
using Microsoft.Extensions.Options;
using TurtleRoute.Azure;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AzureMapsTurtleRouteBuilderExtensions
    {
        /// <summary>
        /// Adds Azure Maps as the next geocoding layer.
        /// </summary>
        public static TurtleRouteBuilder AddAzureMapsGeocoder(this TurtleRouteBuilder builder, Action<AzureMapsOptions> configure)
        {
            builder.Services.AddOptions<AzureMapsOptions>().Configure(configure);
            builder.Services.AddSingleton(sp => new AzureGeocoder(sp.GetRequiredService<IOptions<AzureMapsOptions>>().Value.ApiKey));
            return builder.AddGeocoder(sp => sp.GetRequiredService<AzureGeocoder>());
        }

        /// <summary>
        /// Uses Azure Maps for <see cref="TurtleRoute.IRoutePlanner"/> and <see cref="TurtleRoute.IRouteMatrix"/>.
        /// </summary>
        public static TurtleRouteBuilder UseAzureMapsRouting(this TurtleRouteBuilder builder, Action<AzureMapsOptions> configure)
        {
            builder.Services.AddOptions<AzureMapsOptions>().Configure(configure);
            return builder
                .UseRoutePlanner(sp => new AzureRoutePlanner(sp.GetRequiredService<IOptions<AzureMapsOptions>>().Value.ApiKey))
                .UseRouteMatrix(sp => new AzureRouteMatrix(sp.GetRequiredService<IOptions<AzureMapsOptions>>().Value.ApiKey));
        }
    }
}
