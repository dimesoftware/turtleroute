using System.Collections.Generic;
using Azure.Maps.Search.Models;

namespace TurtleRoute
{
    public class ConfidenceComparer : IComparer<ConfidenceEnum?>
    {
        private static int Score(ConfidenceEnum? c)
        {
            if (c == ConfidenceEnum.High) return 0;
            if (c == ConfidenceEnum.Medium) return 1;
            if (c == ConfidenceEnum.Low) return 2;
            return 3;
        }

        public int Compare(ConfidenceEnum? x, ConfidenceEnum? y) => Score(x).CompareTo(Score(y));
    }
}