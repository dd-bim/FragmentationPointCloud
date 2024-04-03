using System;
using System.Collections;
using System.Collections.Generic;

using System.Collections.Immutable;
using D3 = GeometryLib.Double.D3;
using static System.Math;
using static GeometryLib.Double.Constants;


namespace GeometryLib.Double.D2
{
    public readonly struct LineString : IReadOnlyList<Vector>
    {
        private static readonly LineString empty = new LineString(
            BBox.Empty, ImmutableArray<Vector>.Empty);

        public static ref readonly LineString Empty => ref empty;

        public BBox BBox { get; }

        public ImmutableArray<Vector> Vertices { get; }

        public bool IsClosed { get; }

        public int Count => Vertices.Length;

        public int EdgesCount => Vertices.Length - 1;

        public double Area { get; }

        public bool IsLinearRing => Area != 0.0;

        public Vector this[int index] => Vertices[index];

        public Edge GetEdge(int index) => new Edge(Vertices[index], Vertices[index + 1]);

        private static bool isClosed(in ImmutableArray<Vector> vertices) => vertices.Length > 2 && vertices[0].Equals(vertices[^1]);

        private static (bool isClosed,double area) GetArea(in ImmutableArray<Vector> vertices, bool isClosed = false)
        {
            isClosed = isClosed || LineString.isClosed(vertices);
            if (!isClosed || vertices.Length <= 3) return (isClosed, 0.0);
            var area = vertices[0].x * (vertices[1].y - vertices[^2].y);
            var prev = vertices[0];
            for (var i = vertices.Length - 2; i > 0; i--)
            {
                area += (vertices[i].x * (prev.y - vertices[i - 1].y));
                prev = vertices[i];
            }
            area *= 0.5;
            if(double.IsNaN(area) || double.IsInfinity(area) || Math.Abs(area) < TRIGTOL)
            {
                area = 0.0;
            }
            return (true, area);
        }

        private LineString(in BBox bBox, in ImmutableArray<Vector> vertices, in bool isClosed, in double area)
        {
            BBox = bBox;
            Vertices = vertices;
            IsClosed = isClosed;
            Area = area;
        }

        private LineString(in BBox bBox, in ImmutableArray<Vector> vertices)
        {
            BBox = bBox;
            Vertices = vertices;
            (IsClosed, Area) = GetArea(Vertices);
        }

        public LineString(in Vector first)
        {
            BBox = new BBox(first);
            Vertices = ImmutableArray.Create(first);
            IsClosed = false;
            Area = 0.0;
        }

        public LineString(in IReadOnlyList<Vector> vertices, bool isLinearRing, bool addFirst = false)
        {
            if (vertices.Count < 1)
            {
                Vertices = ImmutableArray<Vector>.Empty;
                BBox = BBox.Empty;
                IsClosed = false;
                Area = 0;
            }
            else
            {
                if (addFirst)
                {
                    var t = ImmutableArray.CreateBuilder<Vector>(vertices.Count + 1);
                    t.AddRange(vertices);
                    t.Add(vertices[0]);
                    Vertices = t.ToImmutable();
                }
                else
                {
                    Vertices = vertices.ToImmutableArray();
                }
                BBox = BBox.FromVectors(Vertices, addFirst);
                if (isLinearRing)
                {
                    (IsClosed, Area) = GetArea(Vertices, addFirst);
                }
                else
                {
                    IsClosed = isClosed(Vertices);
                    Area = 0;
                }
            }
        }

        public static bool Create(in D3.LineString lineString3, out LineString lineString2, out D3.Plane plane)
        {
            if (D3.Plane.Create(lineString3, out plane, true))
            {
                var box = BBox.Empty;
                var vertices = ImmutableArray.CreateBuilder<Vector>(lineString3.Count);
                foreach (var v in lineString3)
                {
                    var v2 = plane.ToPlaneSystem(v, out var z);
                    vertices.Add(v2);
                    box = box.Extend(v2);
                }
                lineString2 = new LineString(box, vertices.ToImmutable());
                return true;
            }
            lineString2 = default;
            return false;
        }


        public LineString(in D3.IPlane plane, in IReadOnlyList<D3.Vector> lineString3, bool addFirst = false)
        {
            var box = BBox.Empty;
            var vertices = ImmutableArray.CreateBuilder<Vector>(lineString3.Count + (addFirst ? 1 : 0));
            foreach (var v in lineString3)
            {
                var v2 = plane.ToPlaneSystem(v, out var z);
                vertices.Add(v2);
                box = box.Extend(v2);
            }
            if (addFirst)
                vertices.Add(vertices[0]);
            BBox = box;
            Vertices = vertices.ToImmutable();
            (IsClosed, Area) = GetArea(Vertices, addFirst);
        }

        public LineString Add(in Vector vector)
        {
            return new LineString(BBox + vector, Vertices.Add(vector));
        }

        public LineString Reverse()
        {
            var vertices = new Vector[Vertices.Length];
            for (int i = 0, j = vertices.Length - 1; i < vertices.Length; i++, j--)
            {
                vertices[i] = Vertices[j];
            }
            return new LineString(BBox, vertices.ToImmutableArray(), IsClosed, -Area);
        }
 
        public string ToString(string separator, string vectorSeparator = " ")
        {
            var strings = new string[Vertices.Length];
            for (var i = 0; i < Vertices.Length; i++)
            {
                strings[i] = Vertices[i].ToString(vectorSeparator);
            }
            return '(' + string.Join(separator, strings) + ')';
        }

        public override string ToString() => ToString(",");

        public IEnumerable<Edge> Edges()
        {
            for (var i = 0; i < EdgesCount; i++)
            {
                yield return GetEdge(i);
            }
        }

        public IEnumerator<Vector> GetEnumerator() => ((IEnumerable<Vector>)Vertices).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public string ToWktString() => WKTNames.LineString + ToString();

        public static bool TryParse(in string input, out LineString lineString, in bool isLinearRing = false)
        {
            var si = input.IndexOf('(') + 1;
            var ei = input.LastIndexOf(')');
            if (si > 0 && (ei - si) > 6)
            {
                var split = input[si..ei].Split(new[] { ',' });
                if (split.Length > 1)
                {
                    var vertices = ImmutableArray.CreateBuilder<Vector>(split.Length);
                    foreach (var data in split)
                    {
                        if (!Vector.TryParse(data, out var vector))
                        {
                            lineString = default;
                            return false;
                        }
                        vertices.Add(vector);
                    }
                    lineString = new LineString(vertices, isLinearRing);
                    return true;
                }
            }
            lineString = default;
            return false;
        }

        public static bool TryParseWkt(in string input, out LineString lineString, in bool isLinearRing = false)
        {
            lineString = default;
            var wi = input.IndexOf(WKTNames.LineString, StringComparison.InvariantCultureIgnoreCase);
            return wi >= 0 && TryParse(input[(wi + WKTNames.LineString.Length)..], out lineString, isLinearRing);
        }

    }
}
