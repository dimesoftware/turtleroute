namespace TurtleRoute
{
    /// <summary>
    /// How confident a provider is that a result matches the requested address.
    /// Providers map their own scoring onto this scale so results can be compared across vendors.
    /// </summary>
    public enum GeocodeConfidence
    {
        Low = 0,
        Medium = 1,
        High = 2
    }
}
