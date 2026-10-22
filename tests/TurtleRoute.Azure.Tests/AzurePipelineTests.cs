using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TurtleRoute.Tests;

namespace TurtleRoute.Azure.Tests
{
    [TestClass]
    [TestCategory("Integration")]
    public class AzurePipelineTests : GeocodingPipelineContractTests
    {
        protected override string ProviderName => AzureGeocoder.ProviderName;

        protected override string ApiKey => TestSettings.AzureMapsToken;

        protected override void AddProvider(TurtleRouteBuilder builder, string apiKey)
            => builder.AddAzureMapsGeocoder(o => o.ApiKey = apiKey);

        protected override IGeocoder CreateGeocoder(string apiKey) => new AzureGeocoder(apiKey);
    }
}
