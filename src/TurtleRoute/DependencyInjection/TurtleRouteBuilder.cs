using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TurtleRoute;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Configures the Turtle Route services. Geocoders, route planners and route matrices are added as pipeline layers,
    /// tried in the order they are registered.
    /// </summary>
    public class TurtleRouteBuilder
    {
        private readonly List<Func<IServiceProvider, IGeocoder>> _geocoders = [];
        private readonly List<Func<IServiceProvider, IRoutePlanner>> _routePlanners = [];
        private readonly List<Func<IServiceProvider, IRouteMatrix>> _routeMatrices = [];

        internal TurtleRouteBuilder(IServiceCollection services, bool isShadow = false)
        {
            Services = services;
            IsShadow = isShadow;
        }

        public IServiceCollection Services { get; }

        internal IReadOnlyList<Func<IServiceProvider, IGeocoder>> Geocoders => _geocoders;

        internal IReadOnlyList<Func<IServiceProvider, IRoutePlanner>> RoutePlanners => _routePlanners;

        internal IReadOnlyList<Func<IServiceProvider, IRouteMatrix>> RouteMatrices => _routeMatrices;

        internal bool IsShadow { get; }

        internal TurtleRouteBuilder Shadow { get; private set; }

        /// <summary>
        /// Adds a geocoder as the next layer of the pipeline. It is registered as a singleton unless already registered.
        /// </summary>
        public TurtleRouteBuilder AddGeocoder<TGeocoder>() where TGeocoder : class, IGeocoder
        {
            Services.TryAddSingleton<TGeocoder>();
            return AddGeocoder(sp => sp.GetRequiredService<TGeocoder>());
        }

        /// <summary>
        /// Adds a geocoder as the next layer of the pipeline.
        /// </summary>
        public TurtleRouteBuilder AddGeocoder(Func<IServiceProvider, IGeocoder> factory)
        {
            ArgumentNullException.ThrowIfNull(factory);
            _geocoders.Add(factory);
            return this;
        }

        public TurtleRouteBuilder ConfigureGeocoding(Action<GeocodingPipelineOptions> configure)
        {
            Services.Configure(configure);
            return this;
        }

        /// <summary>
        /// Runs a sample of requests through a second geocoder in the background to compare it with the configured pipeline,
        /// without changing the results returned. Comparisons go to <see cref="IGeocodingComparisonSink"/>, by default a
        /// <see cref="ShadowSummarySink"/> that logs one summary per interval. Requires a host to run the background worker.
        /// </summary>
        /// <example>
        /// <code>
        /// services.AddTurtleRoute()
        ///     .AddAzureMapsGeocoder(o => o.ApiKey = azureKey)
        ///     .AddShadowGeocoder(shadow => shadow.AddOpenCageGeocoder(o => o.ApiKey = openCageKey), o => o.SampleRate = 0.1);
        /// </code>
        /// </example>
        public TurtleRouteBuilder AddShadowGeocoder(Action<TurtleRouteBuilder> configureShadow, Action<ShadowGeocodingOptions> configure = null)
        {
            ArgumentNullException.ThrowIfNull(configureShadow);

            if (IsShadow)
                throw new InvalidOperationException("A shadow geocoder cannot have a shadow geocoder of its own.");
            if (Shadow != null)
                throw new InvalidOperationException("A shadow geocoder is already configured.");

            Shadow = new TurtleRouteBuilder(Services, isShadow: true);
            configureShadow(Shadow);

            if (Shadow.Geocoders.Count == 0)
                throw new InvalidOperationException("Add at least one geocoder to the shadow, e.g. shadow => shadow.AddOpenCageGeocoder(...).");

            OptionsBuilder<ShadowGeocodingOptions> options = Services.AddOptions<ShadowGeocodingOptions>();
            if (configure != null)
                options.Configure(configure);

            Services.TryAddSingleton<IGeocodingComparisonSink, ShadowSummarySink>();
            Services.TryAddSingleton<ShadowQueue>();
            Services.AddHostedService<ShadowGeocodingWorker>();

            return this;
        }

        /// <summary>
        /// Adds a route planner as the next layer of the pipeline. It is registered as a singleton unless already registered.
        /// </summary>
        public TurtleRouteBuilder AddRoutePlanner<TRoutePlanner>() where TRoutePlanner : class, IRoutePlanner
        {
            Services.TryAddSingleton<TRoutePlanner>();
            return AddRoutePlanner(sp => sp.GetRequiredService<TRoutePlanner>());
        }

        /// <summary>
        /// Adds a route planner as the next layer of the pipeline.
        /// </summary>
        public TurtleRouteBuilder AddRoutePlanner(Func<IServiceProvider, IRoutePlanner> factory)
        {
            ArgumentNullException.ThrowIfNull(factory);
            ThrowIfShadow();
            _routePlanners.Add(factory);
            return this;
        }

        /// <summary>
        /// Adds a route matrix as the next layer of the pipeline. It is registered as a singleton unless already registered.
        /// </summary>
        public TurtleRouteBuilder AddRouteMatrix<TRouteMatrix>() where TRouteMatrix : class, IRouteMatrix
        {
            Services.TryAddSingleton<TRouteMatrix>();
            return AddRouteMatrix(sp => sp.GetRequiredService<TRouteMatrix>());
        }

        /// <summary>
        /// Adds a route matrix as the next layer of the pipeline.
        /// </summary>
        public TurtleRouteBuilder AddRouteMatrix(Func<IServiceProvider, IRouteMatrix> factory)
        {
            ArgumentNullException.ThrowIfNull(factory);
            ThrowIfShadow();
            _routeMatrices.Add(factory);
            return this;
        }

        private void ThrowIfShadow()
        {
            if (IsShadow)
                throw new InvalidOperationException("Shadow mode compares geocoders only. Add routing providers to the main builder.");
        }
    }
}
