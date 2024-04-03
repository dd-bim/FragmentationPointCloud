using BigMath;
using GeometryLib.Interfaces;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace GeometryLib.Int32.D2
{
    public readonly struct Polygon : IReadOnlyList<LineString>//, IGeometryOgc2<int>
    {

        public int GeometricDimension => 2;

        public ImmutableArray<LineString> Rings { get; }

        public BBox BBox => Rings[0].BBox;

        public long Area2 { get; }

        public int Count => Rings.Length;

        public LineString this[int index] => Rings[index];

        internal Polygon(in ImmutableArray<LineString> rings, long area2)
        {
            Rings = rings;
            Area2 = area2;
        }

        private Polygon(in IReadOnlyList<LineString> rings, long area2)
        {
            Rings = rings.ToImmutableArray();
            Area2 = area2;
        }

        public Polygon(in LineString ring)
        {
            if (!ring.IsLinearRing)
                throw new ArgumentException($"Parameter {nameof(ring)} must be LinearRing");
            Rings = ImmutableArray.Create(ring.Area2 < 0 ? ring.Reverse() : ring);
            Area2 = ring.Area2;
        }

        public static bool Create(in IReadOnlyList<Vector> ring, out Polygon polygon)
        {
            var lineString = ring is LineString ls ? ls : new LineString(ring,true);
            if (!lineString.IsLinearRing || lineString.Area2 <= 0)
            {
                polygon = default;
                return false;
            }
            // evtl. drehen
            polygon = new Polygon(lineString);
            return true;
        }

        public static bool Create(in IReadOnlyList<LineString> rings, out Polygon polygon)
        {
            polygon = default;
            if (rings is null || rings.Count < 1)
            {
                return false;
            }
            var ls = rings[0];
            if (!ls.IsLinearRing)
            {
                return false;
            }
            var area2 = ls.Area2;
            var reverse = area2 < 0;
            var lss = new List<LineString>(rings.Count)
            {
                reverse ? ls.Reverse() : ls
            };
            var cumm = new HashSet<Vector>(ls);

            for (var i = 1; i < rings.Count; i++)
            {
                ls = rings[i];
                if (ls.Area2 == 0)
                {
                    continue;
                }
                var cummCount = cumm.Count;
                cumm.UnionWith(ls);
                if (!ls.IsLinearRing // Ring 
                    || ls.Area2 > 0 != reverse // nur negative innere Ringe
                    || !lss[0].BBox.Encloses(ls.BBox) // innerer Ring muss innerhalb des äußeren liegen
                    || (cummCount + ls.Count - 1) != cumm.Count) // Ringe dürfen keine gemeinsamen Punkte haben
                {
                    return false;
                }
                area2 += ls.Area2;
                lss.Add(reverse ? ls.Reverse() : ls);
            }

            area2 = reverse ? -area2 : area2;

            if (area2 <= 0)
            {
                return false;
            }

            polygon = new Polygon(lss, area2);
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
