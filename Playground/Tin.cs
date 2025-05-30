using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace Playground;

public sealed record Tin(
    ImmutableArray<(double x, double y)> Vertices,
    ImmutableArray<int> Triangles,
    ImmutableArray<int> Opposites,
    ImmutableArray<int> Hull,
    BitArray IsInterior,
    int Start)
{
    private static readonly Random Random = new();
    private static bool RandomNext => Random.Next(0, 2) == 0;


    public static bool Create((double x, double y)[] vertices, int[] intriangles, out Tin? tin)
    {
        if (vertices.Length < 3)
        {
            tin = null;
            return false;
        }

        // Set mesh structure
        var triangles = new List<int>(intriangles.Length);
        var opposites = new List<int>(intriangles.Length);
        var bits = new List<bool>(intriangles.Length / 3);

        var oppositeEdges = new Dictionary<(int s, int t), int>();
        for (int i = 0; i < intriangles.Length; i+=3)
        {
            int a = intriangles[i + 0];
            int b = intriangles[i + 1];
            int c = intriangles[i + 2];
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
            opposites.Add(-1);
            opposites.Add(-1);
            opposites.Add(-1);
            oppositeEdges[(a, b)] = i + 2;
            oppositeEdges[(b, c)] = i;
            oppositeEdges[(c, a)] = i + 1;
            bits.Add(true); // all triangles are interior by default
        }

        // get the center point
        double centerX = 0, centerY = 0;
        foreach (var v in vertices)
        {
            centerX += v.x;
            centerY += v.y;
        }
        centerX /= vertices.Length;
        centerY /= vertices.Length;
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

                var (sx, sy) = vertices[edge.s];
                var (tx, ty) = vertices[edge.t];
                double dx = ((sx + tx) * 0.5) - centerX;
                double dy = ((sy + ty) * 0.5) - centerY;
                double dist = (dx * dx) + (dy * dy);
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
            if (SideSign(vertices[a], vertices[b], vertices[c]) > 0)
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

    public static Tin Create((double x, double y) a, (double x, double y) b, (double x, double y) c)
    {
        int sign = SideSign(a, b, c);
        if (sign == 0)
        { // points are collinear
            throw new ArgumentException("Points must not be collinear.", nameof(a));
        }
        if (SideSign(a, b, c) < 0)
        { // ensure counter-clockwise order
            (a, c) = (c, a);
        }
        return new Tin(
            [a, b, c],
            [0, 1, 2],
            [-1, -1, -1],
            [0, 2, 1], // triangle hull (clockwise)
            new BitArray(1, true), // single triangle is interior by default
            1
        );
    }

    private static int SideSign(in (double x, double y) p, in (double x, double y) a, in (double x, double y) b)
    {
        return double.Sign(crossProduct(p, a, b));
    }

    private static double crossProduct(in (double x, double y) p, in (double x, double y) a, in (double x, double y) b)
    {
        return (a.x - p.x) * (b.y - p.y) - (a.y - p.y) * (b.x - p.x);
    }

    private int SideSign(int apexIndex, in (double x, double y) point)
    {
        var (prev, next) = PrevNext(apexIndex);
        var a = Vertices[Triangles[next]];
        var b = Vertices[Triangles[prev]];
        return SideSign(point, a, b);
    }

    private static int Next(int apexIndex) => (Math.DivRem(apexIndex, 3, out int ti) * 3) + ((ti + 1) % 3);

    private static int Prev(int apexIndex) => (Math.DivRem(apexIndex, 3, out int ti) * 3) + ((ti + 2) % 3);

    private static (int prev, int next) PrevNext(int apexIndex)
    {
        int tri0 = Math.DivRem(apexIndex, 3, out int ti) * 3;
        return (tri0 + ((ti + 2) % 3), tri0 + ((ti + 1) % 3));
    }

    public bool Intersects(in (double x, double y) point)
    {
        int zeroH = -1;
        // 1. Hull-Test: Punkt muss innerhalb aller Hull-Kanten liegen
        foreach (int h in Hull)
        {
            int sign = SideSign(h, point);
            if (sign < 0) return false;
            if (sign == 0) zeroH = h;
        }
        if (zeroH >= 0)
        { // zero sign found
            return IsInterior[zeroH / 3];
        }

        // 2. Suche im Inneren
        int ab = Start;
        int abSign= SideSign(Start, point) // use start edge if no zero sign found
        do
        {
            var (ca, bc) = PrevNext(ab);
            var a = Vertices[Triangles[bc]];
            var b = Vertices[Triangles[ca]];
            var c = Vertices[Triangles[ab]];
            int bcSign = SideSign(point, b, c);
            int caSign = SideSign(point, c, a);
            switch (abSign, bcSign, caSign)
            {
                case (_, 0, 0):
                case (0, _, 0):
                case (0, 0, _): return true;
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
        } while (true);
    }


}
