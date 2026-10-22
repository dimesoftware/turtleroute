using System;
using Microsoft.Extensions.Options;
using TurtleRoute;
using TurtleRoute.Here;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HereTurtleRouteBuilderExtensions
    {
        /// <summary>
        /// Adds HERE as the next routing layer, for both <see cref="IRoutePlanner"/> and <see cref="IRouteMatrix"/>.
        /// </summary>
        /// <param name="configureHttpClient">Optionally configures the underlying HttpClients, e.g. to add a resilience handler.</param>
        public static TurtleRouteBuilder AddHereRouting(this TurtleRouteBuilder builder, Action<HereOptions> configure, Action<IHttpClientBuilder> configureHttpClient = null)
        {
            builder.Services.AddOptions<HereOptions>().Configure(configure);

            IHttpClientBuilder plannerClient = builder.Services.AddHttpClient<HereRoutePlanner, HereRoutePlanner>((httpClient, sp) => new HereRoutePlanner(httpClient, sp.GetRequiredService<IOptions<HereOptions>>().Value));
            IHttpClientBuilder matrixClient = builder.Services.AddHttpClient<HereRouteMatrix, HereRouteMatrix>((httpClient, sp) => new HereRouteMatrix(httpClient, sp.GetRequiredService<IOptions<HereOptions>>().Value));
            configureHttpClient?.Invoke(plannerClient);
            configureHttpClient?.Invoke(matrixClient);

            return builder
                .AddRoutePlanner(sp => sp.GetRequiredService<HereRoutePlanner>())
                .AddRouteMatrix(sp => sp.GetRequiredService<HereRouteMatrix>());
        }
    }
}
