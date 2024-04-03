using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

using static System.Math;

namespace GeometryLib.Double.D3
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

        public Vector this[int index] => Vertices[index];

        private static bool isClosed(in ImmutableArray<Vector> vertices) => vertices.Length > 2 && vertices[0].ApproxEquals(vertices[^1]);

        private LineString(in BBox bBox, in ImmutableArray<Vector> vertices)
        {
            BBox = bBox;
            Vertices = vertices;
            IsClosed = isClosed(Vertices);
        }

        public LineString(in Vector first)
        {
            BBox = new BBox(first);
            Vertices = ImmutableArray.Create(first);
            IsClosed = false;
        }

        public LineString(in IReadOnlyList<Vector>? vertices)
        {
            if (vertices is null || vertices.Count < 1)
            {
                Vertices = ImmutableArray<Vector>.Empty;
                BBox = BBox.Empty;
                IsClosed = false;
            }
            else
            {
                Vertices = vertices.ToImmutableArray();
                BBox = BBox.FromVectors(Vertices);
                IsClosed = isClosed(Vertices);
            }
        }


        public LineString(in IPlane plane, in D2.LineString lineString2)
        {
            var box = BBox.Empty;
            var vertices = new Vector[lineString2.Count];
            for (var i = 0; i < vertices.Length; i++)
            {
                box = box.Extend(vertices[i] = plane.FromPlaneSystem(lineString2[i]));
            }
            BBox = box;
            Vertices = vertices.ToImmutableArray();
            IsClosed = isClosed(Vertices);
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
            return new LineString(BBox, vertices.ToImmutableArray());
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

        public string ToWktString() => WKTNames.LineStringZ + ToString();

        public static bool TryParse(in string input, out LineString lineString)
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
                    lineString = new LineString(vertices);
                    return true;
                }
            }
            lineString = default;
            return false;
        }

        public static bool TryParseWkt(in string input, out LineString lineString, in bool isLinearRing = false)
        {
            lineString = default;
            var wi = input.IndexOf(WKTNames.LineStringZ, StringComparison.InvariantCultureIgnoreCase);
            return wi >= 0 && TryParse(input[(wi + WKTNames.LineStringZ.Length)..], out lineString);
        }

        public IEnumerator<Vector> GetEnumerator() => ((IEnumerable<Vector>)Vertices).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
