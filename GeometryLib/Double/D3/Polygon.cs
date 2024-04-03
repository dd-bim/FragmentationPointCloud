 using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

using static System.Math;

namespace GeometryLib.Double.D3
{
    public readonly struct Polygon : IReadOnlyList<LineString>
    {

        public Plane Plane { get; }

        public D2.Polygon Polygon2D { get; }

        public ImmutableArray<LineString> Rings { get; }

        public BBox BBox => Rings[0].BBox;

        public double Area => Polygon2D.Area;

        public int Count => Rings.Length;

        public LineString this[int index] => Rings[index];

        private Polygon(in IPlane plane, in D2.Polygon polygon2D, in ImmutableArray<LineString> rings)
        {
            Plane = plane.GetPlane();
            Polygon2D = polygon2D;
            Rings = rings;
        }

        public Polygon(in IPlane plane, in D2.Polygon polygon)
        {
            Plane = plane.GetPlane();
            Polygon2D = polygon;
            var rings = ImmutableArray.CreateBuilder<LineString>(polygon.Count);
            foreach (var t in polygon)
            {
                rings.Add(new LineString(Plane.System.FromSystem(t)));
            }
            Rings = rings.ToImmutable();
        }

        public static bool Create(in IReadOnlyList<LineString> rings, out Polygon polygon, out double maxPlaneDist)
        {
            maxPlaneDist = 0.0;
            if (rings.Count > 0 && D2.LineString.Create(rings[0], out var exterior, out var plane) && exterior.IsLinearRing)
            {
                var flipp = exterior.Area < 0;
                var rings2d = new D2.LineString[rings.Count];
                rings2d[0] = flipp ? exterior.Reverse() : exterior;
                for (var i = 1; i < rings.Count; i++)
                {
                    var vertices = plane.System.ToSystem(rings[i], out var zs);
                    var interiori = new D2.LineString(vertices, true);
                    rings2d[i] = flipp ? interiori.Reverse() : interiori;
                    if(rings2d[i].Area > 0)
                    {
                        polygon = default;
                        return false;
                    }
                    foreach (var t in zs)
                    {
                        var az = Abs(t);
                        if (az > maxPlaneDist)
                        {
                            maxPlaneDist = az;
                        }
                    }
                }
                if (D2.Polygon.Create(rings2d, out var polgon2d))
                {
                    polygon = new Polygon(plane, polgon2d, rings.ToImmutableArray());
                    return true;
                }
            }
            polygon = default;
            return false;
        }

        public static bool Create(in IPlane plane, in IReadOnlyList<LineString> rings, out Polygon polygon, out double maxPlaneDist)
        {
            var plane_ = plane.GetPlane();
            var rings2d = new D2.LineString[rings.Count];
            maxPlaneDist = 0.0;
            for (var i = 0; i < rings.Count; i++)
            {
                var vertices = plane_.System.ToSystem(rings[i], out var zs);
                rings2d[i] = new D2.LineString(vertices,true);
                foreach (var t in zs)
                {
                    var az = Abs(t);
                    if (az > maxPlaneDist)
                    {
                        maxPlaneDist = az;
                    }
                }
            }
            if (D2.Polygon.Create(rings2d, out var polgon2d))
            {
                polygon = new Polygon(plane_, polgon2d, rings.ToImmutableArray());
                return true;
            }
            polygon = default;
            return false;
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

        public Plane[] BBoxView(in Vector apex)
        {
            var turn = Plane.Dist(apex) > 0;
            var boundary = new[] {
                Plane.FromPlaneSystem(Polygon2D.BBox.Min),
                Plane.FromPlaneSystem(new D2.Vector(Polygon2D.BBox.Max.x, Polygon2D.BBox.Min.y)),
                Plane.FromPlaneSystem(Polygon2D.BBox.Max),
                Plane.FromPlaneSystem(new D2.Vector(Polygon2D.BBox.Min.x, Polygon2D.BBox.Max.y))
            };
            if (turn)
            {
                Array.Reverse(boundary);
            }
            return new[] {
                turn ? Plane.Turn() : Plane,
                new Plane(boundary[0], boundary[1], apex),
                new Plane(boundary[1], boundary[2], apex),
                new Plane(boundary[2], boundary[3], apex),
                new Plane(boundary[3], boundary[0], apex)
            };
        }

        public string ToWktString() => WKTNames.Polygon + ToString();

        public static bool TryParse(in string input, out Polygon polygon, out double maxPlaneDist)
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
                    while (ei > si && ei <= lastei && LineString.TryParse(input[si..ei], out var lineString))
                    {
                        rings.Add(lineString);
                        var ci = input.IndexOf(',', ei);
                        if (ci < 0) break;
                        si = input.IndexOf('(', ci);
                        ei = input.IndexOf(')', si) + 1;
                    }
                    if(Create(rings, out polygon, out maxPlaneDist))
                    {
                        return true;
                    }
                }
            }
            maxPlaneDist = double.NaN;
            polygon = default;
            return false;
        }

        public static bool TryParseWkt(in string input, out Polygon polygon, out double maxPlaneDist)
        {
            polygon = default;
            maxPlaneDist = double.NaN;
            var wi = input.IndexOf(WKTNames.PolygonZ, StringComparison.InvariantCultureIgnoreCase);
            return wi >= 0 && TryParse(input[(wi + WKTNames.PolygonZ.Length)..], out polygon, out maxPlaneDist);
        }

    }
}
