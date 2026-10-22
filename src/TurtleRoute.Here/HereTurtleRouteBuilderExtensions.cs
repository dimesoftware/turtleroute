using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using TurtleRoute;
using TurtleRoute.Here;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HereTurtleRouteBuilderExtensions
    {
        /// <summary>
        /// Uses HERE for <see cref="IRoutePlanner"/> and <see cref="IRouteMatrix"/>.
        /// </summary>
        /// <param name="configureHttpClient">Optionally configures the underlying HttpClients, e.g. to add a resilience handler.</param>
        public static TurtleRouteBuilder UseHereRouting(this TurtleRouteBuilder builder, Action<HereOptions> configure, Action<IHttpClientBuilder> configureHttpClient = null)
        {
            builder.Services.AddOptions<HereOptions>().Configure(configure);

            IHttpClientBuilder plannerClient = builder.Services.AddHttpClient<HereRoutePlanner, HereRoutePlanner>((httpClient, sp) => new HereRoutePlanner(httpClient, sp.GetRequiredService<IOptions<HereOptions>>().Value));
            IHttpClientBuilder matrixClient = builder.Services.AddHttpClient<HereRouteMatrix, HereRouteMatrix>((httpClient, sp) => new HereRouteMatrix(httpClient, sp.GetRequiredService<IOptions<HereOptions>>().Value));
            configureHttpClient?.Invoke(plannerClient);
            configureHttpClient?.Invoke(matrixClient);

            // Transient rather than UseRoutePlanner's singleton, so the typed HttpClients get a fresh handler from IHttpClientFactory.
            builder.Services.Replace(ServiceDescriptor.Transient<IRoutePlanner>(sp => sp.GetRequiredService<HereRoutePlanner>()));
            builder.Services.Replace(ServiceDescriptor.Transient<IRouteMatrix>(sp => sp.GetRequiredService<HereRouteMatrix>()));

            return builder;
        }
    }
}
