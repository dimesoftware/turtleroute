using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TurtleRoute.Tests
{
    /// <summary>
    /// Live checks that every route planner returns <see cref="Route.Revisions"/> the same way, by comparing them with where the legs actually go.
    /// </summary>
    internal static class RoutePlannerContract
    {
        private static readonly GeoCoordinate Home = new(51.219501, 4.359231); // Antwerp

        // Not in a sensible order, so the provider has to reorder them. Both providers pick an order here in which reading the revisions
        // the wrong way round gives a different tour, so a mixed-up mapping fails the test instead of passing by coincidence.
        private static readonly GeoCoordinate[] Stops =
        [
            new(51.2093, 3.2247),     // Bruges
            new(50.879844, 4.700518), // Leuven
            new(51.054342, 3.717424), // Ghent
            new(50.930690, 5.332480), // Hasselt
            new(50.846557, 4.351697)  // Brussels
        ];

        /// <summary>
        /// The n-th revision is the n-th stop visited, and its NewIndex is that stop's index as provided.
        /// </summary>
        public static async Task RevisionsShouldFollowTheLegsAsync(IRoutePlanner planner)
        {
            Route route = await planner.GetRouteAsync([Home, .. Stops, Home], new RoutePlanOptions { OptimizeWaypointOrder = true });

            Assert.HasCount(Stops.Length, route.Revisions);
            CollectionAssert.AreEqual(Enumerable.Range(0, Stops.Length).ToArray(), route.Revisions.Select(r => r.OriginalIndex).ToArray());
            CollectionAssert.AreEquivalent(Enumerable.Range(0, Stops.Length).ToArray(), route.Revisions.Select(r => r.NewIndex).ToArray());

            // Each leg but the last ends at the stop its revision points to.
            for (int n = 0; n < Stops.Length; n++)
            {
                GeoCoordinate legEnd = route.Legs[n].Waypoints.Last();
                int nearest = Enumerable.Range(0, Stops.Length).OrderBy(i => Distance(Stops[i], legEnd)).First();
                Assert.AreEqual(route.Revisions[n].NewIndex, nearest, $"Leg {n} ends at stop {nearest}, but revision {n} points to stop {route.Revisions[n].NewIndex}.");
            }
        }

        /// <summary>
        /// A single intermediate stop has nothing to reorder, but still gets its revision.
        /// </summary>
        public static async Task SingleStopShouldHaveOneRevisionAsync(IRoutePlanner planner)
        {
            Route route = await planner.GetRouteAsync([Home, Stops[0], Home], new RoutePlanOptions { OptimizeWaypointOrder = true });

            CollectionAssert.AreEqual(new[] { new RouteRevision(0, 0) }, route.Revisions);
        }

        private static double Distance(GeoCoordinate a, GeoCoordinate b)
            => Math.Pow(a.Latitude - b.Latitude, 2) + Math.Pow(a.Longitude - b.Longitude, 2);
    }
}
