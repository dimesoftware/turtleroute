using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TurtleRoute.Tests
{
    internal static class TestSettings
    {
        private static readonly IConfigurationRoot Settings = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false)
            .AddJsonFile("appsettings.development.json", true)
            .AddJsonFile("appsettings.Development.json", true) // File names are case-sensitive on Linux CI agents
            .AddJsonFile("appsettings.test.json", true)
            .AddEnvironmentVariables("TURTLEROUTE_") // For CI, e.g. TURTLEROUTE_token and TURTLEROUTE_openCageToken
            .Build();

        public static string AzureMapsToken => Require("token");

        public static string OpenCageToken => Require("openCageToken");

        /// <summary>
        /// Integration tests call live APIs; they are skipped when the key is not configured.
        /// </summary>
        private static string Require(string key)
        {
            string value = Settings[key];
            if (string.IsNullOrWhiteSpace(value))
                Assert.Inconclusive($"Set '{key}' in appsettings.development.json to run this integration test.");

            return value;
        }
    }
}
