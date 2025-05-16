using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using GeometryLib.D3;
using static GeometryLib.Constants;
using static System.Math;


namespace GeometryLib.D2;

public readonly struct LineString : IReadOnlyList<Vector>
{
    public BBox BBox { get; }

    private ImmutableArray<Vector> Vertices { get; }

    public bool IsClosed { get; }

    public int Count => Vertices.Length;

    public double Area { get; }

    public bool IsLinearRing => Area != 0.0;

    public Vector this[int index] => Vertices[index];

    private static bool ComputeIsClosed(in ImmutableArray<Vector> vertices)
    {
        return vertices.Length > 2 && vertices[0].Equals(vertices[^1]);
    }

    private static (bool isClosed, double area) GetArea(in ImmutableArray<Vector> vertices, bool isClosed = false)
    {
        isClosed = isClosed || ComputeIsClosed(vertices);
        if (!isClosed || vertices.Length <= 3) return (isClosed, 0.0);
        double area = vertices[0].x * (vertices[1].y - vertices[^2].y);
        Vector prev = vertices[0];
        for (int i = vertices.Length - 2; i > 0; i--)
        {
            area += vertices[i].x * (prev.y - vertices[i - 1].y);
            prev = vertices[i];
        }

        area *= 0.5;
        if (double.IsNaN(area) || double.IsInfinity(area) || Abs(area) < TRIGTOL) area = 0.0;
        return (true, area);
    }

    private LineString(in BBox bBox, in ImmutableArray<Vector> vertices, in bool isClosed, in double area)
    {
        BBox = bBox;
        Vertices = vertices;
        IsClosed = isClosed;
        Area = area;
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
                ImmutableArray<Vector>.Builder t = ImmutableArray.CreateBuilder<Vector>(vertices.Count + 1);
                t.AddRange(vertices);
                t.Add(vertices[0]);
                Vertices = t.ToImmutable();
            }
            else
            {
                Vertices = [..vertices];
            }

            BBox bbox = BBox.Empty;
            for (int i = addFirst ? 1 : 0; i < ((IReadOnlyList<Vector>)Vertices).Count; i++)
            {
                Vector v = ((IReadOnlyList<Vector>)Vertices)[i];
                bbox += v;
            }

            BBox = bbox;
            if (isLinearRing)
            {
                (IsClosed, Area) = GetArea(Vertices, addFirst);
            }
            else
            {
                IsClosed = ComputeIsClosed(Vertices);
                Area = 0;
            }
        }
    }


    public LineString(in Plane plane, in IReadOnlyList<D3.Vector> lineString3, bool addFirst = false)
    {
        BBox box = BBox.Empty;
        ImmutableArray<Vector>.Builder vertices =
            ImmutableArray.CreateBuilder<Vector>(lineString3.Count + (addFirst ? 1 : 0));
        foreach (D3.Vector v in lineString3)
        {
            Vector v2 = plane.ToPlaneSystem(v, out _);
            vertices.Add(v2);
            box += v2;
        }

        if (addFirst)
            vertices.Add(vertices[0]);
        BBox = box;
        Vertices = vertices.ToImmutable();
        (IsClosed, Area) = GetArea(Vertices, addFirst);
    }

    public LineString Reverse()
    {
        var vertices = new Vector[Vertices.Length];
        for (int i = 0, j = vertices.Length - 1; i < vertices.Length; i++, j--) vertices[i] = Vertices[j];
        return new LineString(BBox, [..vertices], IsClosed, -Area);
    }


    public bool IsPointInPolygon(in Vector p)
    {
        (double px, double py) = p;
        if (px < BBox.Min.x || px > BBox.Max.x || py < BBox.Min.y || py > BBox.Max.y) return false;

        // https://wrf.ecse.rpi.edu/Research/Short_Notes/pnpoly.html
        var inside = false;
        for (int i = 0, j = Vertices.Length - 1; i < Vertices.Length; j = i++)
        {
            (double ix, double iy) = Vertices[i];
            (double jx, double jy) = Vertices[j];
            if (iy > py != jy > py &&
                px < (jx - ix) * (py - iy) / (jy - iy) + ix)
                inside = !inside;
        }

        return inside;
    }


    public string ToString(string separator, string vectorSeparator = " ")
    {
        var strings = new string[Vertices.Length];
        for (var i = 0; i < Vertices.Length; i++) strings[i] = Vertices[i].ToString(vectorSeparator);
        return '(' + string.Join(separator, strings) + ')';
    }

    public override string ToString()
    {
        return ToString(",");
    }

    public IEnumerator<Vector> GetEnumerator()
    {
        return ((IEnumerable<Vector>)Vertices).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public static bool TryParse(in string input, out LineString lineString, in bool isLinearRing = false)
    {
        int si = input.IndexOf('(') + 1;
        int ei = input.LastIndexOf(')');
        if (si > 0 && ei - si > 6)
        {
            string[] split = input[si..ei].Split([',']);
            if (split.Length > 1)
            {
                ImmutableArray<Vector>.Builder vertices = ImmutableArray.CreateBuilder<Vector>(split.Length);
                foreach (string data in split)
                {
                    if (!Vector.TryParse(data, out Vector vector))
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
}