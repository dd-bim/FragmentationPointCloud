using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace GeometryLib.Int32.D2
{
    public static class Algorithms
    {
        /// <summary>Calculate the hull of <paramref name="vectors"/>, hull can contain collinear points</summary>
        /// <param name="vectors">The vectors.</param>
        /// <param name="uniqueSorted">The sorted and unique vectors (indices of returned array refer to this).</param>
        /// <returns>Sorted Indices of hull points in <paramref name="uniqueSorted"/>, first != last</returns>
        /// <exception cref="Exception">At least three points needed</exception>
        public static int[] ConvexHull(in IReadOnlyCollection<Vector> vectors, out ImmutableArray<Vector> uniqueSorted)
        {
            // Algorithmus von https://en.wikibooks.org/wiki/Algorithm_Implementation/Geometry/Convex_hull/Monotone_chain
            uniqueSorted = vectors.ToImmutableSortedSet().ToImmutableArray();
            if (uniqueSorted.Length < 3)
            {
                return Array.Empty<int>();
            }
            var k = 0; // Position of last hull point
            var h = new int[2 * uniqueSorted.Length];

            // Build upper hull
            for (var i = 0; i < uniqueSorted.Length; i++)
            {
                while ((k > 1) && (uniqueSorted[i].Det(uniqueSorted[h[k - 2]], uniqueSorted[h[k - 1]]) < 0))
                {
                    k--;
                }
                h[k++] = i;
            }

            // Build lower hull
            for (int i = uniqueSorted.Length - 2, t = k; i >= 0; i--)
            {
                while ((k > t) && (uniqueSorted[i].Det(uniqueSorted[h[k - 2]], uniqueSorted[h[k - 1]]) < 0))
                {
                    k--;
                }
                h[k++] = i;
            }
            k--;
            var hull = new int[k];
            Array.Copy(h, hull, k);
            return hull;
        }

    }
}
