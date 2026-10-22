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
        /// <param name="configureHttpClient">
        /// Optionally configures the underlying HttpClient, e.g. to add a resilience handler. Its timeouts must fit within
        /// <see cref="TurtleRoute.OpenCage.OpenCageOptions.Timeout"/>, which bounds the whole call including retries.
        /// </param>
        public static TurtleRouteBuilder AddOpenCageGeocoder(this TurtleRouteBuilder builder, Action<OpenCageOptions> configure, Action<IHttpClientBuilder> configureHttpClient = null)
        {
            builder.Services.AddOptions<OpenCageOptions>().Configure(configure);

            IHttpClientBuilder httpClientBuilder = builder.Services
                .AddHttpClient<OpenCageGeocoder, OpenCageGeocoder>((httpClient, sp) => new OpenCageGeocoder(httpClient, sp.GetRequiredService<IOptions<OpenCageOptions>>().Value))
                .ConfigureHttpClient((sp, httpClient) => httpClient.Timeout = sp.GetRequiredService<IOptions<OpenCageOptions>>().Value.Timeout);
            configureHttpClient?.Invoke(httpClientBuilder);

            return builder.AddGeocoder(sp => sp.GetRequiredService<OpenCageGeocoder>());
        }
    }
}
