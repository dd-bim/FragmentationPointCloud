using Autodesk.Revit.DB;
using Autodesk.Revit.DB.DirectContext3D;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using static Revit.Extensions;

namespace Revit.Data;

/// <summary>
/// Represents a Triangulated Irregular Network (TIN), a data structure used to model a surface as a set of
/// non-overlapping triangles in 2D space.
/// </summary>
/// <remarks>A TIN is defined by its vertices, triangles, opposites (adjacency relationships between triangles),
/// convex hull, and a flag indicating whether each triangle is interior or exterior. It is commonly used in
/// computational geometry, geographic information systems (GIS), and 3D modeling.</remarks>
/// <param name="Vertices"></param>
/// <param name="Triangles"></param>
/// <param name="Opposites"></param>
/// <param name="Hull"></param>
/// <param name="IsInterior"></param>
/// <param name="Start"></param>
public sealed record Tin(
    ImmutableArray<UV> Vertices,
    ImmutableArray<int> Triangles,
    ImmutableArray<int> Opposites,
    ImmutableArray<int> Hull,
    BitArray IsInterior,
    int Start)
{
    private static readonly Random Random = new();
    private static bool RandomNext => Random.Next(0, 2) == 0;

    private static readonly JsonSerializerOptions writeOptions = new()
    {
        WriteIndented = false,
        Converters = { new BitArrayJsonConverter() }
    };


    private static readonly JsonSerializerOptions readOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new BitArrayJsonConverter() }
    };

    public static bool Create(Mesh mesh, Plane plane, Transform transform,
        out double maxDistance, out XYZ min3D, out XYZ max3D, out UV min2D, out UV max2D, out Tin? tin)
    {
        min3D = Extensions.MaxXYZ;
        max3D = Extensions.MinXYZ;
        min2D = Extensions.MaxUV;
        max2D = Extensions.MinUV;
        maxDistance = 0.0;
        if (mesh.Vertices.Count < 3 || mesh.NumberOfNormals > 1)
        {
            tin = null;
            return false;
        }

        // Set mesh structure
        var vertices = new List<UV>(mesh.Vertices.Count);
        var triangles = new List<int>(mesh.NumTriangles * 3);
        var opposites = new List<int>(mesh.NumTriangles * 3);
        var bits = new List<bool>(mesh.NumTriangles);
        var center = UV.Zero;

        for (int i = 0; i < mesh.Vertices.Count; i++)
        {
            var xyz = mesh.Vertices[i];
            xyz = transform.OfPoint(xyz) * Constants.feet2Meter; // apply transformation
            plane.Project(xyz, out var uv, out double dist);
            vertices.Add(uv);
            center += uv; // accumulate center
            maxDistance = double.Max(maxDistance, double.Abs(dist));
            min2D = min2D.Min(uv);
            max2D = max2D.Max(uv);
            min3D = min3D.Min(xyz);
            max3D = max3D.Max(xyz);
        }
        center /= mesh.Vertices.Count; // average center

        var oppositeEdges = new Dictionary<(int s, int t), int>();
        for (int i = 0; i < mesh.NumTriangles; i++)
        {
            var triangle = mesh.get_Triangle(i);
            int a = (int)triangle.get_Index(0);
            int b = (int)triangle.get_Index(1);
            int c = (int)triangle.get_Index(2);
            // ensure tringle is counter-clockwise
            if (vertices[a].SideSign(vertices[b], vertices[c]) < 0)
            {
                (a, b) = (b, a); // swap a and b
            }
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
            opposites.Add(-1);
            opposites.Add(-1);
            opposites.Add(-1);
            oppositeEdges[(a, b)] = (i * 3) + 2;
            oppositeEdges[(b, c)] = i * 3;
            oppositeEdges[(c, a)] = (i * 3) + 1;
            bits.Add(true); // all triangles are interior by default
        }

        // search the edge closest to the center point
        int start = -1;
        double minDist = double.MaxValue;
        foreach (var (edge, idx) in oppositeEdges)
        {
            if (opposites[idx] != -1)
                continue; // already set

            if (oppositeEdges.TryGetValue((edge.t, edge.s), out int oidx))
            {
                opposites[idx] = oidx;
                opposites[oidx] = idx;

                var s = vertices[edge.s];
                var t = vertices[edge.t];
                double dU = ((s.U + t.U) * 0.5) - center.U;
                double dV = ((s.V + t.V) * 0.5) - center.V;
                double dist = (dU * dU) + (dV * dV);
                if (dist < minDist)
                {
                    minDist = dist;
                    start = idx; // remember the edge with the smallest dist
                }
            }
        }

        // get hull
        var hull = new List<int>();
        int curr = 0;
        while (opposites[curr] >= 0) curr++;
        do
        {
            hull.Add(curr);
            curr = Prev(curr);
            while (opposites[curr] >= 0)
                curr = Prev(opposites[curr]);
        } while (curr != hull[0]);
        // make convex
        for (int i = 0; i < hull.Count; i++)
        {
            curr = hull[i];
            var (prev, next) = PrevNext(curr);
            int a = triangles[prev];
            int b = triangles[next];
            int neighbor = hull[(i + 1) % hull.Count];
            int neighborNext = Next(neighbor);
            int c = triangles[neighborNext];
            if (vertices[a].SideSign(vertices[b], vertices[c]) > 0)
            { // make new triangle
                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(c);
                opposites[neighbor] = opposites.Count;
                opposites.Add(neighbor);
                opposites.Add(-1);
                opposites[curr] = opposites.Count;
                opposites.Add(curr);
                bits.Add(false); // new triangle is exterior
                hull[i] = triangles.Count - 2;
                hull.RemoveAt((i + 1) % hull.Count);
                i = i == hull.Count ? i - 2 : i - 1; // repeat on new hull edge
            }
        }
        tin = new Tin(
            [.. vertices],
            [.. triangles],
            [.. opposites],
            [.. hull],
            new BitArray([.. bits]),
            start
        );
        return true;
    }

    public static Tin Create(MeshTriangle triangle, Transform transform, out Plane plane,
       out XYZ min3D, out XYZ max3D, out UV min2D, out UV max2D)
    {
        var a = transform.OfPoint(triangle.get_Vertex(0)) * Constants.feet2Meter;
        var b = transform.OfPoint(triangle.get_Vertex(1)) * Constants.feet2Meter;
        var c = transform.OfPoint(triangle.get_Vertex(2)) * Constants.feet2Meter;

        plane = Plane.CreateByThreePoints(a, b, c);

        plane.Project(a, out var uvA, out _);
        plane.Project(b, out var uvB, out _);
        plane.Project(c, out var uvC, out _);

        min3D = Extensions.Min(a, Extensions.Min(a, c));
        max3D = Extensions.Max(a, Extensions.Max(b, c));
        min2D = Extensions.Min(uvA, Extensions.Min(uvB, uvC));
        max2D = Extensions.Max(uvA, Extensions.Max(uvB, uvC));

        // ensure tringle is counter-clockwise
        //if (uvA.SideSign(uvB, uvC) < 0)
        //{
        //    (uvA, uvB) = (uvB, uvA); // swap uvA and uvB
        //}

        return new Tin(
            [uvA, uvB, uvC],
            [0, 1, 2],
            [-1, -1, -1],
            [0, 2, 1], // triangle hull (clockwise)
            new BitArray(1, true), // single triangle is interior by default
            1
        );
    }

    private int SideSign(int apexIndex, in UV point)
    {
        var (prev, next) = PrevNext(apexIndex);
        var a = Vertices[Triangles[next]];
        var b = Vertices[Triangles[prev]];
        return point.SideSign(a, b);
    }

    private static int Next(int apexIndex) => (Math.DivRem(apexIndex, 3, out int ti) * 3) + ((ti + 1) % 3);

    private static int Prev(int apexIndex) => (Math.DivRem(apexIndex, 3, out int ti) * 3) + ((ti + 2) % 3);

    private static (int prev, int next) PrevNext(int apexIndex)
    {
        int tri0 = Math.DivRem(apexIndex, 3, out int ti) * 3;
        return (tri0 + ((ti + 2) % 3), tri0 + ((ti + 1) % 3));
    }

    /// <summary>
    /// Determines whether the specified point lies within the convex hull or triangulated interior of the structure.
    /// </summary>
    /// <remarks>This method checks if the given point is contained within the convex hull of the structure.
    /// If the point lies on the boundary of the hull, it further determines whether the boundary is considered
    /// interior. For points inside the hull, the method identifies the triangle containing the point and evaluates its
    /// interior status.</remarks>
    /// <param name="point">The point to test for intersection, represented as a <see cref="UV"/> instance.</param>
    /// <returns><see langword="true"/> if the point lies inside the convex hull or within an interior triangle; otherwise, <see
    /// langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the method fails to locate a triangle containing the point within the triangulated interior.</exception>
    public bool Intersects(in UV point)
    {
        // Check if point lies inside the convex hull
        int zeroH = -1;
        foreach (int h in Hull)
        {
            int sign = SideSign(h, point);
            if (sign < 0)
            { // Point lies outside the convex hull
                return false;
            }
            if (sign == 0)
            { // Point lies on the hull edge
                zeroH = h; // remember
            }
        }
        if (zeroH >= 0)
        { // check if hull edge is interior
            return IsInterior[zeroH / 3];
        }

        // Point lies inside the convex hull, now we need to find the triangle containing the point
        int ab = Start;
        int abSign = SideSign(ab, point);
        int count = Triangles.Length;
        do
        { // Point is inside the triangulation
            var (ca, bc) = PrevNext(ab);
            var a = Vertices[Triangles[bc]];
            var b = Vertices[Triangles[ca]];
            var c = Vertices[Triangles[ab]];
            int bcSign = point.SideSign(b, c);
            int caSign = point.SideSign(c, a);
            switch (abSign, bcSign, caSign)
            {
                case (_, 0, 0):
                case (0, _, 0):
                case (0, 0, _): return true; // vertices are always interior
                case (1, 1, 1): return IsInterior[ab / 3];
                case (0, 1, 1): return IsInterior[ab / 3] || (Opposites[ab] > 0 && IsInterior[Opposites[ab] / 3]);
                case (1, 0, 1): return IsInterior[bc / 3] || (Opposites[bc] > 0 && IsInterior[Opposites[bc] / 3]);
                case (1, 1, 0): return IsInterior[ca / 3] || (Opposites[ca] > 0 && IsInterior[Opposites[ca] / 3]);
                case (_, 1, 1): ab = Opposites[ab]; abSign = -abSign; break;
                case (1, _, 1): ab = Opposites[bc]; abSign = -bcSign; break;
                case (1, 1, _): ab = Opposites[ca]; abSign = -caSign; break;
                case (1, _, _): (ab, abSign) = RandomNext ? (Opposites[bc], -bcSign) : (Opposites[ca], -caSign); break;
                case (_, 1, _): (ab, abSign) = RandomNext ? (Opposites[ca], -caSign) : (Opposites[ab], -abSign); break;
                case (_, _, 1): (ab, abSign) = RandomNext ? (Opposites[ab], -abSign) : (Opposites[bc], -bcSign); break;
            }

        } while (count-- > 0);
        throw new InvalidOperationException("Failed to find triangle containing point in TIN.");
    }

    public override string ToString() => JsonSerializer.Serialize(this, writeOptions);

    public static bool TryParse(ReadOnlySpan<char> input, out Tin? tin)
    {
        try
        {
            tin = JsonSerializer.Deserialize<Tin>(input, readOptions);
            return tin != null;
        }
        catch
        {
            tin = null;
            return false;
        }
    }

}
