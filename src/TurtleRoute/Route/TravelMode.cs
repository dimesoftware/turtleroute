namespace TurtleRoute
{
    /// <summary>
    /// The vehicle the route is planned for. Not every provider supports every mode: a planner throws <see cref="System.NotSupportedException"/>
    /// for a mode it cannot route, so a <see cref="RoutePlannerPipeline"/> falls back to the next layer.
    /// </summary>
    public enum TravelMode
    {
        Car,
        Van,
        Truck,
        Motorcycle,
        Bicycle,
        Pedestrian
    }
}
