using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TurtleRoute.Tests;

namespace TurtleRoute.OpenCage.Tests
{
    [TestClass]
    [TestCategory("Integration")]
    public class OpenCagePipelineTests : GeocodingPipelineContractTests
    {
        protected override string ProviderName => OpenCageGeocoder.ProviderName;

        protected override string ApiKey => TestSettings.OpenCageToken;

        protected override void AddProvider(TurtleRouteBuilder builder, string apiKey)
            => builder.AddOpenCageGeocoder(o => o.ApiKey = apiKey);

        protected override IGeocoder CreateGeocoder(string apiKey)
            => new OpenCageGeocoder(new HttpClient(), new OpenCageOptions { ApiKey = apiKey });
    }
}
