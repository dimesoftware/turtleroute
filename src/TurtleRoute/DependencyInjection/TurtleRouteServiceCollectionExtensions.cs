using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TurtleRoute;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TurtleRouteServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="IGeocoder"/>, <see cref="IRoutePlanner"/> and <see cref="IRouteMatrix"/> as pipelines over the
        /// geocoders, route planners and route matrices added to the returned builder.
        /// Calling this more than once returns the same builder.
        /// </summary>
        public static TurtleRouteBuilder AddTurtleRoute(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            ServiceDescriptor existing = services.FirstOrDefault(d => d.ServiceType == typeof(TurtleRouteBuilder));
            if (existing?.ImplementationInstance is TurtleRouteBuilder registered)
                return registered;

            TurtleRouteBuilder builder = new(services);
            services.AddSingleton(builder);
            services.AddOptions<GeocodingPipelineOptions>();

            // Transient, so layers built on typed HttpClients get a fresh handler from IHttpClientFactory.
            services.AddTransient<IGeocoder>(sp =>
            {
                if (builder.Geocoders.Count == 0)
                    throw new InvalidOperationException("No geocoders are registered. Add at least one, e.g. services.AddTurtleRoute().AddOpenCageGeocoder(...).");

                GeocodingPipeline pipeline = new(
                    builder.Geocoders.Select(factory => factory(sp)),
                    sp.GetRequiredService<IOptions<GeocodingPipelineOptions>>().Value,
                    sp.GetService<ILogger<GeocodingPipeline>>());

                if (builder.Shadow == null)
                    return pipeline;

                return new ShadowGeocoder(pipeline, sp.GetRequiredService<ShadowQueue>(), sp.GetRequiredService<IOptions<ShadowGeocodingOptions>>().Value);
            });

            services.AddTransient<IRoutePlanner>(sp =>
            {
                if (builder.RoutePlanners.Count == 0)
                    throw new InvalidOperationException("No route planners are registered. Add at least one, e.g. services.AddTurtleRoute().AddHereRouting(...).");

                return new RoutePlannerPipeline(builder.RoutePlanners.Select(factory => factory(sp)), sp.GetService<ILogger<RoutePlannerPipeline>>());
            });

            services.AddTransient<IRouteMatrix>(sp =>
            {
                if (builder.RouteMatrices.Count == 0)
                    throw new InvalidOperationException("No route matrices are registered. Add at least one, e.g. services.AddTurtleRoute().AddHereRouting(...).");

                return new RouteMatrixPipeline(builder.RouteMatrices.Select(factory => factory(sp)), sp.GetService<ILogger<RouteMatrixPipeline>>());
            });

            return builder;
        }

        public static IServiceCollection AddTurtleRoute(this IServiceCollection services, Action<TurtleRouteBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            configure(services.AddTurtleRoute());
            return services;
        }
    }
}
