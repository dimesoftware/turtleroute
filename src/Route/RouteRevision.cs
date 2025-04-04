using System.Diagnostics;

namespace TurtleRoute
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public struct RouteRevision
    {
        public RouteRevision(int originalIndex, int newIndex)
        {
            OriginalIndex = originalIndex;
            NewIndex = newIndex;
        }

        public int OriginalIndex { get; set; }
        public int NewIndex { get; set; }

        private string DebuggerDisplay => $"From {OriginalIndex} to {NewIndex}";
    }
}