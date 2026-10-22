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
        /// Adds Azure Maps as the next routing layer, for both <see cref="TurtleRoute.IRoutePlanner"/> and <see cref="TurtleRoute.IRouteMatrix"/>.
        /// </summary>
        public static TurtleRouteBuilder AddAzureMapsRouting(this TurtleRouteBuilder builder, Action<AzureMapsOptions> configure)
        {
            builder.Services.AddOptions<AzureMapsOptions>().Configure(configure);
            builder.Services.AddSingleton(sp => new AzureRoutePlanner(sp.GetRequiredService<IOptions<AzureMapsOptions>>().Value.ApiKey));
            builder.Services.AddSingleton(sp => new AzureRouteMatrix(sp.GetRequiredService<IOptions<AzureMapsOptions>>().Value.ApiKey));

            return builder
                .AddRoutePlanner(sp => sp.GetRequiredService<AzureRoutePlanner>())
                .AddRouteMatrix(sp => sp.GetRequiredService<AzureRouteMatrix>());
        }
    }
}
