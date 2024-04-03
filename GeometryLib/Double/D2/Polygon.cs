using System;
using System.Collections;
using System.Collections.Generic;

/* Unmerged change from project 'GeometryLib (netstandard2.1)'
Before:
using System.Collections.Immutable;
using static System.Math;
After:
using System.Collections.Immutable;

using static System.Math;
*/

/* Unmerged change from project 'GeometryLib (netstandard2.0)'
Before:
using System.Collections.Immutable;
using static System.Math;
After:
using System.Collections.Immutable;

using static System.Math;
*/
using System.Collections.Immutable;

namespace GeometryLib.Double.D2
{
    public readonly struct Polygon : IReadOnlyList<LineString>
    {

        public ImmutableArray<LineString> Rings { get; }

        public BBox BBox => Rings[0].BBox;

        public double Area { get; }

        public int Count => Rings.Length;

        public LineString this[int index] => Rings[index];

        private Polygon(IReadOnlyList<LineString> rings, double area)
        {
            Rings = rings.ToImmutableArray();
            Area = area;
        }

        public Polygon(in LineString ring)
        {
            if (!ring.IsLinearRing)
                throw new ArgumentException($"Parameter {nameof(ring)} must be a LinearRing");
            Rings = ImmutableArray.Create(ring.Area < 0 ? ring.Reverse() : ring);
            Area = ring.Area;
        }

        public Polygon ChangePlane(in D3.IPlane oldPlane, in D3.IPlane newPlane)
        {
            var p3 = new List<D3.LineString>(Rings.Length);
            foreach (var ls in Rings)
            {
                p3.Add(new D3.LineString(oldPlane, ls));
            }
            var p2 = new List<LineString>(Rings.Length);
            foreach (var ls in p3)
            {
                p2.Add(new LineString(newPlane, ls));
            }
            return new(p2, Area);
        }

        public static bool Create(in IReadOnlyList<Vector> ring, out Polygon polygon)
        {
            var lineString = ring is LineString ls ? ls : new LineString(ring, true);
            if (!lineString.IsLinearRing)
            {
                polygon = default;
                return false;
            }
            if(lineString.Area < 0)
            {
                lineString = lineString.Reverse();
            }
            polygon = new Polygon(lineString);
            return true;
        }

        public static bool Create(in IReadOnlyList<LineString>? rings, out Polygon polygon)
        {
            if (rings is null || rings.Count < 1)
            {
                polygon = default;
                return false;
            }
            var ls = rings[0];
            if (!ls.IsLinearRing)
            {
                polygon = default;
                return false;
            }
            var area = ls.Area;
            var reverse = area < 0;
            var lss = new List<LineString>(rings.Count)
            {
                reverse ? ls.Reverse() : ls
            };

            for (var i = 1; i < rings.Count; i++)
            {
                ls = rings[i];
                if(ls.Area == 0.0)
                {
                    continue;
                }
                if (!ls.IsLinearRing || ls.Area > 0 != reverse || !lss[0].BBox.Encloses(ls.BBox))
                {
                    polygon = default;
                    return false;
                }
                area += ls.Area;
                lss.Add(reverse ? ls.Reverse() : ls);
            }

            area = reverse ? -area : area;

            if (area <= 0.0)
            {
                polygon = default;
                return false;
            }

            polygon = new Polygon(lss, area);
            return true;
        }

        public static bool Create(in LineString exterior, in IReadOnlyList<LineString>? interiors, out Polygon polygon)
        {
            if (!exterior.IsLinearRing)
            {
                polygon = default;
                return false;
            }
            var lrs = new List<LineString>(1 + (interiors?.Count ?? 0))
            {
                exterior.Area < 0 ? exterior.Reverse() : exterior
            };
            var area = Math.Abs(exterior.Area);
            if (interiors is not null && interiors.Count > 0)
            {
                foreach (var lr in interiors)
                {
                    if (lr.Area == 0.0)
                    {
                        continue;
                    }
                    if (!lr.IsLinearRing || !exterior.BBox.Encloses(lr.BBox))
                    {
                        polygon = default;
                        return false;
                    }
                    area -= Math.Abs(lr.Area);
                    lrs.Add(lr.Area > 0 ? lr.Reverse() : lr);
                }
            }
            if (area <= 0.0)
            {
                polygon = default;
                return false;
            }

            polygon = new Polygon(lrs, area);
            return true;
        }

        public string ToString(string separator, string lineStringSeparator = ",", string vectorSeparator = " ")
        {
            var strings = new string[Rings.Length];
            for (var i = 0; i < Rings.Length; i++)
            {
                strings[i] = Rings[i].ToString(lineStringSeparator, vectorSeparator);
            }
            return '(' + string.Join(separator, strings) + ')';
        }

        public override string ToString() => ToString(",");

        public IEnumerator<LineString> GetEnumerator() => ((IEnumerable<LineString>)Rings).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public string ToWktString() => WKTNames.Polygon + ToString();

        public static bool TryParse(in string input, out Polygon polygon)
        {
            var si = input.IndexOf('(') + 1;
            var lastei = input.LastIndexOf(')');
            if (si > 0 && (lastei - si) > 16)
            {
                si = input.IndexOf('(', si);
                if (si > 0)
                {
                    var rings = ImmutableArray.CreateBuilder<LineString>();
                    var ei = input.IndexOf(')', si) + 1;
                    while (ei > si && ei <= lastei && LineString.TryParse(input[si..ei], out var lineString, true))
                    {
                        rings.Add(lineString);
                        var ci = input.IndexOf(',', ei);
                        if (ci < 0) break;
                        si = input.IndexOf('(', ci);
                        ei = input.IndexOf(')', si) + 1;
                    }
                    return Create(rings, out polygon);
                }
            }
            polygon = default;
            return false;
        }

        public static bool TryParseWkt(in string input, out Polygon polygon)
        {
            polygon = default;
            var wi = input.IndexOf(WKTNames.Polygon, StringComparison.InvariantCultureIgnoreCase);
            return wi >= 0 && TryParse(input[(wi + WKTNames.Polygon.Length)..], out polygon);
        }
    }
}
