namespace TurtleRoute
{
    public class GeocodingPipelineOptions
    {
        /// <summary>
        /// Results below this confidence are rejected and the next layer is tried. Defaults to <see cref="GeocodeConfidence.Medium"/>.
        /// </summary>
        public GeocodeConfidence MinimumConfidence { get; set; } = GeocodeConfidence.Medium;
    }
}
