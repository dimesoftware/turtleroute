using System;
using Microsoft.Extensions.Options;
using TurtleRoute.OpenCage;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OpenCageTurtleRouteBuilderExtensions
    {
        /// <summary>
        /// Adds OpenCage as the next geocoding layer.
        /// </summary>
        /// <param name="configureHttpClient">Optionally configures the underlying HttpClient, e.g. to add a resilience handler.</param>
        public static TurtleRouteBuilder AddOpenCageGeocoder(this TurtleRouteBuilder builder, Action<OpenCageOptions> configure, Action<IHttpClientBuilder> configureHttpClient = null)
        {
            builder.Services.AddOptions<OpenCageOptions>().Configure(configure);

            IHttpClientBuilder httpClientBuilder = builder.Services.AddHttpClient<OpenCageGeocoder, OpenCageGeocoder>((httpClient, sp) => new OpenCageGeocoder(httpClient, sp.GetRequiredService<IOptions<OpenCageOptions>>().Value));
            configureHttpClient?.Invoke(httpClientBuilder);

            return builder.AddGeocoder(sp => sp.GetRequiredService<OpenCageGeocoder>());
        }
    }
}
