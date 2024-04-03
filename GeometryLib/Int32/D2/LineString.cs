using BigMath;
using GeometryLib.Interfaces;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

using static System.Math;


namespace GeometryLib.Int32.D2
{
    public readonly struct LineString : IReadOnlyList<Vector>//, IGeometryOgc2<int>
    {
        private static readonly LineString empty = new LineString(
            BBox.Empty, ImmutableArray<Vector>.Empty);

        public int GeometricDimension => 1;

        public static ref readonly LineString Empty => ref empty;

        public BBox BBox { get; }

        public ImmutableArray<Vector> Vertices { get; }

        public bool IsClosed { get; }

        public int Count => Vertices.Length;

        public long Area2 { get; }

        public bool IsLinearRing => Area2 != 0;

        public Vector this[int index] => Vertices[index];

        private static bool isClosed(in ImmutableArray<Vector> vertices) => vertices.Length > 2 && vertices[0].Equals(vertices[^1]);

        private static long GetArea2(bool isClosed, in ImmutableArray<Vector> vertices)
        {
            if (isClosed && vertices.Length > 3 
                && (ImmutableHashSet.CreateRange(vertices).Count + 1) == vertices.Length)
            {
                var area = Math.BigMul(vertices[0].x, (vertices[1].y - vertices[^2].y));
                var prev = vertices[0];
                for (var i = vertices.Length - 2; i > 0; i--)
                {
                    area += Math.BigMul(vertices[i].x, (prev.y - vertices[i - 1].y));
                    prev = vertices[i];
                }
                return area;
            }
            return 0;
        }

        internal LineString(in BBox bBox, in ImmutableArray<Vector> vertices, in bool isClosed, in long area2)
        {
            BBox = bBox;
            Vertices = vertices;
            IsClosed = isClosed;
            Area2 = area2;
        }

        internal LineString(in BBox bBox, in ImmutableArray<Vector> vertices, bool zeroArea = false)
        {
            BBox = bBox;
            Vertices = vertices;
            IsClosed = isClosed(Vertices);
            Area2 = zeroArea ? 0 : GetArea2(IsClosed, Vertices);
        }

        public LineString(in Vector first)
        {
            BBox = new BBox(first);
            Vertices = ImmutableArray.Create(first);
            IsClosed = false;
            Area2 = 0;
        }

        public LineString(in ImmutableArray<Vector> vertices, bool isLinearRing)
        {
            if (vertices.Length < 1)
            {
                Vertices = ImmutableArray<Vector>.Empty;
                BBox = BBox.Empty;
                IsClosed = false;
                Area2 = 0;
            }
            else
            {
                Vertices = vertices;
                BBox = BBox.FromVectors(Vertices);
                IsClosed = isClosed(Vertices);
                Area2 = isLinearRing ? GetArea2(IsClosed, Vertices) : 0;
            }
        }

        public LineString(in IReadOnlyList<Vector>? vertices, bool isLinearRing)
            :this(vertices?.ToImmutableArray() ?? ImmutableArray<Vector>.Empty, isLinearRing)
        {}

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
            return new LineString(BBox, vertices.ToImmutableArray(), IsClosed, -Area2);
        }


        public LineString MakeInvalidLinearRing() => Area2 != 0 ? new LineString(BBox, Vertices, IsClosed, 0) : this;

 
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

        public static bool TryParse(in string input, bool isLinearRing, out LineString lineString, out int end, char separator = ',', char vectorSeparator = ' ')
        {
            if (string.IsNullOrEmpty(input) || separator == vectorSeparator)
            {
                lineString = default;
                end = -1;
                return false;
            }
            var vertices = ImmutableArray.CreateBuilder<Vector>();
            var start = Min(input.Length, input.IndexOf('(') + 1);
            var closedEnd = input.IndexOf(')', start);
            end = input.IndexOf(separator, start);
            if (end < 0 || end > closedEnd)
            {
                end = closedEnd;
            }
            while (end > (start + 2))
            {
                if (!Vector.TryParse(input[start..end], out var vector, vectorSeparator))
                {
                    lineString = default;
                    end = -1;
                    return false;
                }
                vertices.Add(vector);
                if (end == closedEnd)
                {
                    break;
                }
                start = Min(input.Length, end + 1);
                end = input.IndexOf(separator, start);
                if (end < 0 || end > closedEnd)
                {
                    end = closedEnd;
                }
            }
            if (end != closedEnd)
            {
                lineString = default;
                end = -1;
                return false;
            }
            lineString = new LineString(vertices, isLinearRing);
            return true;
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
