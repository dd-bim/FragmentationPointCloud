using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace GeometryLib.D3;

public readonly struct LineString : IReadOnlyList<Vector>
{
    public BBox BBox { get; }

    private ImmutableArray<Vector> Vertices { get; }

    public bool IsClosed { get; }

    public int Count => Vertices.Length;

    public Vector this[int index] => Vertices[index];

    private static bool CheckIfClosed(in ImmutableArray<Vector> vertices)
    {
        return vertices.Length > 2 && vertices[0].ApproxEquals(vertices[^1]);
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
            Vertices = [..vertices];
            BBox = ((IReadOnlyCollection<Vector>)Vertices).Aggregate(BBox.Empty, (current, v) => current + v);
            IsClosed = CheckIfClosed(Vertices);
        }
    }

    public LineString(in Plane plane, in D2.LineString lineString2)
    {
        BBox box = BBox.Empty;
        var vertices = new Vector[lineString2.Count];
        for (var i = 0; i < vertices.Length; i++) box += vertices[i] = plane.FromPlaneSystem(lineString2[i]);
        BBox = box;
        Vertices = [..vertices];
        IsClosed = CheckIfClosed(Vertices);
    }

    public override string ToString()
    {
        var strings = new string[Vertices.Length];
        for (var i = 0; i < Vertices.Length; i++) strings[i] = Vertices[i].ToString(" ");
        return '(' + string.Join(",", strings) + ')';
    }

    public IEnumerator<Vector> GetEnumerator()
    {
        return ((IEnumerable<Vector>)Vertices).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}