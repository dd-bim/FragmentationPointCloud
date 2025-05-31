using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Playground;

public sealed record Tin(
    Transformation Transformation,
    ImmutableArray<IntXY> Vertices,
    ImmutableArray<int> Triangles,
    ImmutableArray<int> Opposites,
    ImmutableArray<ImmutableArray<int>> Hulls,
    BitArray IsInterior)
{
    public const byte Digits = 6; // number of decimal places for the UV coordinates

    private static readonly Random Random = new();
    private static bool RandomNext => Random.Next(0, 2) == 0;

    public static bool Create(Mesh mesh, out Tin? tin)
    {
        var min2D = new UV(double.MaxValue, double.MaxValue);
        var max2D = new UV(double.MinValue, double.MinValue);
        if (mesh.Vertices.Length < 3)
        {
   //         Log.Error("Mesh has less than 3 vertices or multiple normals, cannot create TIN.");
            tin = null;
            return false;
        }

        // Set mesh structure
        var triangles = new List<int>(mesh.Triangles.Length * 3);
        var opposites = new List<int>(mesh.Triangles.Length * 3);
        var bits = new List<bool>(mesh.Triangles.Length);

        for (int i = 0; i < mesh.Vertices.Length; i++)
        {
            var uv = mesh.Vertices[i];
            min2D = min2D.Min(uv);
            max2D = max2D.Max(uv);
        }

        if (!Transformation.Create(min2D, max2D, Digits, out var transformation))
        {
            tin = null;
            return false; // Transformation not possible
        }
        // Ensure unique vertices and apply transformation
        var vertexMap = new Dictionary<int, int>(mesh.Vertices.Length);
        var ivertexMap = new Dictionary<IntXY, int>(mesh.Vertices.Length);
        var ivertices = new List<IntXY>(mesh.Vertices.Length);
        for (int i = 0; i < mesh.Vertices.Length; i++)
        {
            var uv = mesh.Vertices[i];
            var xy = transformation.Forward(uv);
            if (!ivertexMap.TryAdd(xy, ivertices.Count))
                vertexMap[i] = ivertexMap[xy]; // reuse existing vertex
            else
            {
                vertexMap[i] = ivertices.Count; // new vertex
                ivertices.Add(xy);
            }
        }

        // Build triangles and opposites
        var oppositeEdges = new Dictionary<(int s, int t), int>();
        var triMap = new HashSet<(int, int, int)>(mesh.Triangles.Length);
        for (int i = 0; i < mesh.Triangles.Length; i++)
        {
            var triangle = mesh.Triangles[i];
            int a = vertexMap[triangle[0]];
            int b = vertexMap[triangle[1]];
            int c = vertexMap[triangle[2]];
            int ma = a, mb = b, mc = c;
            if (ma > mb) (ma, mb) = (mb, ma); // ensure a < b
            if (mb > mc) (mb, mc) = (mc, mb); // ensure b < c
            if (ma > mc) (ma, mc) = (mc, ma); // ensure a < c
            if (!triMap.Add((ma, mb, mc)))
            {
                //Log.Warning($"Triangle {i} is a duplicate after transformation.");
                continue; // duplicate triangle
            }
            int sign = ivertices[a].SideSign(ivertices[b], ivertices[c]);
            if (sign == 0)
            {
                //Log.Error($"Triangle {i} has collinear or duplicate vertices after transformation.");
                tin = null;
                return false; // degenerate triangle
            }
            else if (sign < 0)
            { // ensure triangle is counter-clockwise
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

        // Connect the neighboring edges
        foreach (var (edge, idx) in oppositeEdges)
        {
            if (opposites[idx] != -1)
                continue; // already set

            if (oppositeEdges.TryGetValue((edge.t, edge.s), out int oidx))
            {
                opposites[idx] = oidx;
                opposites[oidx] = idx;
            }
        }

        // Make convex
        var hullSet = new HashSet<int>();
        bool change = true;
        while (change)
        {
            change = false;
            for (int i = 0; i < opposites.Count; i++)
            {
                if (opposites[i] != -1)
                {
                    hullSet.Remove(i); // remove from hull set if it has an opposite
                    continue; // no exterior edge or convex
                }
                if (hullSet.Contains(i)) continue; // no exterior edge or convex
                var (prev, next) = PrevNext(i);
                int j = prev;
                int count = 0;
                while (opposites[j] >= 0)
                {
                    if (count++ > triangles.Count)
                    {
                        //Log.Error("No valid convex hull found, tin has no valid topology.");
                        tin = null;
                        return false; // prevent infinite loop
                    }
                    j = Prev(opposites[j]);
                }
                int a = triangles[prev];
                int b = triangles[next];
                int c = triangles[Next(j)];
                if (ivertices[a].SideSign(ivertices[b], ivertices[c]) > 0)
                { // make new triangle
                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(c);
                    opposites[j] = opposites.Count;
                    opposites.Add(j);
                    int idx = opposites.Count;
                    opposites.Add(-1);
                    opposites[i] = opposites.Count;
                    opposites.Add(i);
                    bits.Add(false); // new triangle is exterior
                    if (oppositeEdges.TryGetValue((a, c), out int oidx))
                    {  // Neighboring edge found
                        opposites[idx] = oidx;
                        opposites[oidx] = idx;
                    }
                    else oppositeEdges.Add((c, a), idx); // only add free edge
                    change = true; // repeat on new edge
                }
                else hullSet.Add(i); // mark as convex hull edge
            }
        }

        // TIN is already convex, so we can create the hull from the opposites
        var hulls = new List<ImmutableArray<int>>();
        var found = new HashSet<int>(hullSet.Count);
        change = true;
        while (change)
        {
            change = false;
            foreach (int h in hullSet)
            {
                if (found.Contains(h)) continue; // skip convex edges
                var hull = new List<int>();
                int curr = h;
                int count = 0;
                do
                {
                    if (!found.Add(curr) || count++ > hullSet.Count)
                    {
                        //Log.Error($"Hull edge {curr} already found in another hull, tin has no valid topology.");
                        tin = null;
                        return false; // prevent infinite loop
                    }
                    hull.Add(curr);
                    curr = Prev(curr);
                    int innerCount = 0;
                    while (opposites[curr] >= 0)
                    {
                        if (innerCount++ > triangles.Count)
                        {
                            //Log.Error("No valid convex hull found, tin has no valid topology.");
                            tin = null;
                            return false; // prevent infinite loop
                        }
                        curr = Prev(opposites[curr]);
                    }
                } while (curr != h);
                if (hull.Count < 3)
                {
                    //Log.Error($"Hull edge {h} has less than 3 edges, rin has no valid topology.");
                    tin = null;
                    return false; // prevent invalid hull
                }
                hulls.Add([.. hull]);

            }
        }
        if (hulls.Count == 0)
        {
            //Log.Error("No valid convex hull found, rin has no valid topology.");
            tin = null;
            return false; // no hull found
        }
        tin = new Tin(
            transformation,
            [.. ivertices],
            [.. triangles],
            [.. opposites],
            [.. hulls],
            new BitArray([.. bits])
        );
        if (!tin.IsValid())
        {
            //Log.Error("TIN is not valid after creation.");
            tin.WriteSVG("debug.svg"); // write SVG for debugging
            tin = null;
            return false; // TIN is not valid
        }
        return true;
    }

    private long SideDet(int apexIndex, in IntXY point)
    {
        var (prev, next) = PrevNext(apexIndex);
        var a = Vertices[Triangles[next]];
        var b = Vertices[Triangles[prev]];
        return point.DetTo(a, b);
    }

    private static int Next(int apexIndex) => (Math.DivRem(apexIndex, 3, out int ti) * 3) + ((ti + 1) % 3);

    private static int Prev(int apexIndex) => (Math.DivRem(apexIndex, 3, out int ti) * 3) + ((ti + 2) % 3);

    private static (int prev, int next) PrevNext(int apexIndex)
    {
        int tri0 = Math.DivRem(apexIndex, 3, out int ti) * 3;
        return (tri0 + ((ti + 2) % 3), tri0 + ((ti + 1) % 3));
    }

    public bool Intersects(in UV pointUV)
    {
        var point = Transformation.Forward(pointUV);
        // Check if point lies inside the convex hull
        int minH = 0; // just a placeholder
        bool outside = true;
        foreach (var hull in Hulls)
        {
            minH = hull[0]; // start with first edge of the hull
            long minDet = long.MaxValue;
            bool inside = true;
            foreach (int h in hull)
            {
                long det = SideDet(h, point);
                if (det < 0)
                { // Point lies outside the convex hull
                    inside = false;
                    break;
                }
                if (det < minDet)
                {
                    minH = h; // remember
                    minDet = det;
                }
            }
            if (inside)
            { // Point lies inside the convex hull
                outside = false;
                break;
            }
        }
        if (outside)
        {
            // Point lies outside the convex hull
            return false;
        }
        // Point lies inside the convex hull, now we need to find the triangle containing the point
        int ab = minH;
        int abSign = double.Sign(SideDet(ab, point));
        int count = Triangles.Length;
        do
        { // Point is inside the triangulation
            if (ab < 0)
            {
                WriteSVG("debug.svg"); // write SVG for debugging
                throw new InvalidOperationException("Invalid triangle index in TIN, ab < 0.");
            }
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
        WriteSVG("debug.svg"); // write SVG for debugging
        throw new InvalidOperationException("Failed to find triangle containing point in TIN.");
    }

    public override string ToString()
    {
        // Format: [vertices],[triangles],[opposites],[hulls],[isInterior],minX,minY,maxX,maxY,scale
        // Vertices: [x1 y1,x2 y2,...]
        // Triangles, Opposites, Hull: [i1,i2,...]
        // Hulls: [[i1,i2,...],[j1,j2,...],...]
        // IsInterior: [0,1,...]


        static string JoinInts(IEnumerable<int> values) =>
            string.Join(",", values);

        static string JoinBools(BitArray bits)
        {
            int[] arr = new int[bits.Count];
            for (int i = 0; i < bits.Count; i++)
                arr[i] = bits[i] ? 1 : 0;
            return string.Join(",", arr);
        }

        string verts = string.Join(",", Vertices.Select(xy => $"{xy.X} {xy.Y}"));
        string triangles = JoinInts(Triangles);
        string opposites = JoinInts(Opposites);
        string hull = string.Join("],[", Hulls.Select(h => JoinInts(h)));
        string isInterior = JoinBools(IsInterior);

        return FormattableString.Invariant($"[{verts}],[{triangles}],[{opposites}],[[{hull}]],[{isInterior}],{Transformation.Min.U:G17},{Transformation.Min.V:G17},{Transformation.Max.U:G17},{Transformation.Max.V:G17},{Transformation.Scale:G17}");
    }

    public static bool TryParse(ReadOnlySpan<char> input, out Tin? tin)
    {
        tin = null;
        // Erwartetes Format: [vertices],[triangles],[opposites],[[hull1],[hull2],...],[isInterior],minX,minY,maxX,maxY,scale
        // Remove whitespace
        string str = input.ToString().Replace(" ", "");
        int idx = 0;

        static bool ParseArray(ReadOnlySpan<char> s, ref int idx, out List<string> items)
        {
            items = [];
            if (idx >= s.Length || s[idx] != '[') return false;
            idx++; // skip '['
            int start = idx;
            int depth = 1;
            while (idx < s.Length && depth > 0)
            {
                if (s[idx] == '[') depth++;
                else if (s[idx] == ']') depth--;
                idx++;
            }
            if (depth != 0) return false;
            string arr = s.Slice(start, idx - start - 1).ToString();
            if (arr.Length == 0)
                return true;
            items.AddRange(arr.Split(',', StringSplitOptions.RemoveEmptyEntries));
            return true;
        }

        // Parse [vertices]
        if (!ParseArray(str, ref idx, out var vertsStr)) return false;
        if (idx >= str.Length || str[idx] != ',') return false; idx++;

        // Parse [triangles]
        if (!ParseArray(str, ref idx, out var trianglesStr)) return false;
        if (idx >= str.Length || str[idx] != ',') return false; idx++;

        // Parse [opposites]
        if (!ParseArray(str, ref idx, out var oppositesStr)) return false;
        if (idx >= str.Length || str[idx] != ',') return false; idx++;

        // Parse [[hulls]]
        if (idx >= str.Length || str[idx] != '[') return false;
        idx++; // skip outer '['
        var hulls = new List<ImmutableArray<int>>();
        while (idx < str.Length && str[idx] == '[')
        {
            if (!ParseArray(str, ref idx, out var hullStr)) return false;
            int?[] hull = [.. hullStr.Select(s => int.TryParse(s, out int v) ? v : (int?)null)];
            if (hull.Any(v => v == null)) return false;
            hulls.Add(ImmutableArray.Create(hull.Select(v => v!.Value).ToArray()));
            if (idx < str.Length && str[idx] == ',') idx++; // skip comma between hulls
        }
        if (idx >= str.Length || str[idx] != ']') return false; idx++; // skip closing outer ']'
        if (idx >= str.Length || str[idx] != ',') return false; idx++;

        // Parse [isInterior]
        if (!ParseArray(str, ref idx, out var isInteriorStr)) return false;
        if (idx >= str.Length || str[idx] != ',') return false; idx++;

        // Parse minX, minY, maxX, maxY, scale
        int nextComma = str.IndexOf(',', idx);
        if (nextComma < 0) return false;
        if (!double.TryParse(str.AsSpan(idx, nextComma - idx), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double minX)) return false;
        idx = nextComma + 1;

        nextComma = str.IndexOf(',', idx);
        if (nextComma < 0) return false;
        if (!double.TryParse(str.AsSpan(idx, nextComma - idx), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double minY)) return false;
        idx = nextComma + 1;

        nextComma = str.IndexOf(',', idx);
        if (nextComma < 0) return false;
        if (!double.TryParse(str.AsSpan(idx, nextComma - idx), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double maxX)) return false;
        idx = nextComma + 1;

        nextComma = str.IndexOf(',', idx);
        if (nextComma < 0) return false;
        if (!double.TryParse(str.AsSpan(idx, nextComma - idx), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double maxY)) return false;
        idx = nextComma + 1;

        if (!double.TryParse(str.AsSpan(idx), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double scale)) return false;

        // Convert verts to ImmutableArray<IntXY>
        var verts = new List<IntXY>(vertsStr.Count / 2);
        for (int i = 0; i < vertsStr.Count; i += 2)
        {
            if (!int.TryParse(vertsStr[i], out int x) || !int.TryParse(vertsStr[i + 1], out int y))
                return false;
            verts.Add(new IntXY(x, y));
        }

        // Convert triangles, opposites
        int?[] triangles = [.. trianglesStr.Select(s => int.TryParse(s, out int v) ? v : (int?)null)];
        if (triangles.Any(v => v == null)) return false;

        int?[] opposites = [.. oppositesStr.Select(s => int.TryParse(s, out int v) ? v : (int?)null)];
        if (opposites.Any(v => v == null)) return false;

        // Convert isInterior
        bool[] bits = new bool[isInteriorStr.Count];
        for (int i = 0; i < isInteriorStr.Count; i++)
        {
            if (isInteriorStr[i] == "1") bits[i] = true;
            else if (isInteriorStr[i] == "0") bits[i] = false;
            else return false;
        }

        var transformation = new Transformation(scale, new UV(minX, minY), new UV(maxX, maxY));
        tin = new Tin(
            transformation,
            [.. verts],
            [.. triangles.Select(v => v!.Value)],
            [.. opposites.Select(v => v!.Value)],
            [.. hulls],
            new BitArray(bits)
        );
        return true;
    }
    public bool IsValid()
    {
        int triangleCount = Triangles.Length / 3;
        int sideCount = Triangles.Length;
        int vertexCount = Vertices.Length;
        int hullCount = Hulls.Sum(h => h.Length);
        int halfEdgeCount = sideCount + hullCount;

        if (Triangles.Length % 3 != 0
            || Triangles.Length != Opposites.Length
            || halfEdgeCount % 2 != 0
            || IsInterior.Count != triangleCount)
            return false; // Triangles must be a multiple of 3 and same to Opposites length

        //if ((2 * vertexCount) - (sideCount + hullCount) + (2 * triangleCount) != 2)
        //    return false; // Euler's formula for planar graphs: V - E + F = 1 (Exterior face counts as F=1)

        for (int i = 0; i < sideCount; i++)
        {
            if (Triangles[i] < 0 || Triangles[i] >= vertexCount)
                return false;
            if (Opposites[i] < -1 || Opposites[i] >= sideCount)
                return false; // Opposites must be -1 or a valid edge index
            if (Opposites[i] >= 0 && Opposites[Opposites[i]] != i)
                return false; // Opposites must be mutual
        }
        var hullSet = new HashSet<int>();
        foreach (var hull in Hulls)
            foreach (int h in hull)
            {
                if (h < 0 || h >= sideCount)
                    return false; // Hull edges must be valid indices
                if (Opposites[h] != -1)
                    return false; // Hull edges must not have opposites
                if (!hullSet.Add(h))
                    return false; // Hull must not contain duplicate edges
            }


        var edgePairs = new HashSet<(int, int)>();
        for (int t = 0; t < triangleCount; t++)
        {
            int i0 = t * 3;
            int ia = Triangles[i0];
            int ib = Triangles[i0 + 1];
            int ic = Triangles[i0 + 2];
            if (!edgePairs.Add((ia, ib)) || !edgePairs.Add((ib, ic)) || !edgePairs.Add((ic, ia)))
                return false; // Kante ist mehrfach vorhanden
            var a = Vertices[ia];
            var b = Vertices[ib];
            var c = Vertices[ic];

            if (a.SideSign(b, c) <= 0)
                return false; // Dreieck muss gegen den Uhrzeigersinn orientiert sein
            for (int v = 0; v < vertexCount; v++)
            {
                if (v == ia || v == ib || v == ic) continue; // Skip triangle vertices
                var vertex = Vertices[v];
                if (vertex.SideSign(a, b) > 0 && vertex.SideSign(b, c) > 0 && vertex.SideSign(c, a) > 0)
                {
                    return false; // Vertex is inside triangle, which is not allowed
                }
            }

        }

        return true;
    }

    public void WriteSVG(string file)
    {
        // Dynamische SVG-Parameter abhängig von der Ausdehnung
        double minX = Transformation.Min.U;
        double maxX = Transformation.Max.U;
        double minY = Transformation.Min.V;
        double maxY = Transformation.Max.V;

        double width = maxX - minX;
        double height = maxY - minY;

        // Zielgröße für die Anzeige (z.B. 800x800)
        const double targetSize = 800.0;
        double scale = Math.Min(targetSize / width, targetSize / height);

        // Dynamische Ränder und Stile
        double margin = Math.Max(targetSize * 0.05, 20.0);
        double svgWidth = (width * scale) + (2 * margin);
        double svgHeight = (height * scale) + (2 * margin);
        double strokeWidth = Math.Max(1.0, Math.Round(Math.Min(svgWidth, svgHeight) * 0.002));
        double pointRadius = Math.Max(2.0, Math.Round(Math.Min(svgWidth, svgHeight) * 0.01));

        const string triangleColor = "#1E90FF"; // Blau für Dreiecke
        const string exteriorColor = "#FF0000"; // Rot für Außendreicke
        const string triangleOpacity = "0.5";
        const string vertexColor = "#FF0000";
        const string hullColor = "#00C800";

        // Hilfsfunktion: Koordinaten transformieren (SVG y-Achse nach unten, skaliert)
        (double x, double y) ToSvg(IntXY iv)
        {
            var v = Transformation.Reverse(iv);
            double x = margin + ((v.U - minX) * scale);
            double y = svgHeight - (margin + ((v.V - minY) * scale));
            return (x, y);
        }

        using var writer = new System.IO.StreamWriter(file, false, System.Text.Encoding.UTF8);
        writer.WriteLine(FormattableString.Invariant(
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{svgWidth}\" height=\"{svgHeight}\" viewBox=\"0 0 {svgWidth} {svgHeight}\">"));

        // Dreiecke zeichnen
        for (int t = 0; t < Triangles.Length / 3; t++)
        {
            int i0 = t * 3;
            var a = Vertices[Triangles[i0]];
            var b = Vertices[Triangles[i0 + 1]];
            var c = Vertices[Triangles[i0 + 2]];
            var (x0, y0) = ToSvg(a);
            var (x1, y1) = ToSvg(b);
            var (x2, y2) = ToSvg(c);
            writer.WriteLine(FormattableString.Invariant(
                $"  <polygon points=\"{x0},{y0} {x1},{y1} {x2},{y2}\" fill=\"{(IsInterior[t] ? triangleColor : exteriorColor)}\" fill-opacity=\"{triangleOpacity}\" stroke=\"{triangleColor}\" stroke-width=\"{strokeWidth}\" />"));
        }

        // Hulls als grüne Polygone
        foreach (var hull in Hulls)
        {
            string hullPoints = string.Join(" ", hull.Select(h =>
            {
                var v = Vertices[Triangles[Next(h)]];
                var (x, y) = ToSvg(v);
                return FormattableString.Invariant($"{x},{y}");
            }));
            writer.WriteLine(
                $"  <polygon points=\"{hullPoints}\" fill=\"none\" stroke=\"{hullColor}\" stroke-width=\"{strokeWidth * 2}\" />");
        }

        // Vertices als rote Punkte
        foreach (var v in Vertices)
        {
            var (x, y) = ToSvg(v);
            writer.WriteLine(FormattableString.Invariant(
                $"  <circle cx=\"{x}\" cy=\"{y}\" r=\"{pointRadius}\" fill=\"{vertexColor}\" />"));
        }

        writer.WriteLine("</svg>");
    }

}

public readonly record struct IntXY(int X, int Y)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static IntXY operator -(in IntXY left, in IntXY right) => new(left.X - right.X, left.Y - right.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static long Det(int leftX, int leftY, int rightX, int rightY) => Math.BigMul(leftX, rightY) - Math.BigMul(leftY, rightX);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static long Det(in IntXY left, in IntXY right) => Det(left.X, left.Y, right.X, right.Y);

    public IntXY Min(in IntXY other) => new(int.Min(X, other.X), int.Min(Y, other.Y));

    public IntXY Max(in IntXY other) => new(int.Max(X, other.X), int.Max(Y, other.Y));

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public int SideSign(in IntXY edgeSource, in IntXY edgeTarget) => long.Sign(DetTo(edgeSource, edgeTarget));

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public long DetTo(in IntXY edgeSource, in IntXY edgeTarget) => Det(edgeSource - this, edgeTarget - this);
}

public readonly record struct UV(double U, double V)
{
    public UV Min(in UV right) => new(double.Min(U, right.U), double.Min(V, right.V));

    public UV Max(in UV right) => new(double.Max(U, right.U), double.Max(V, right.V));
}

public readonly record struct Mesh(UV[] Vertices, int[][] Triangles);

public readonly record struct Transformation(double Scale, UV Min, UV Max)
{
    public static bool Create(UV min, UV max, byte digits, out Transformation transformation)
    {
        double rangeX = max.U - min.U;
        double rangeY = max.V - min.V;
        double range = double.BitIncrement(double.Max(rangeX, rangeY));
        if (range > int.MaxValue)
        {
            transformation = default;
            return false;
        }
        double scale = double.Floor(int.MaxValue / range); // Scale of calculations
        double scalePrecision = 1.0 / scale; // Precision of calculations
        double digitPrecision = (double)new decimal(1, 0, 0, false, digits); // Precision of digits
        if (digitPrecision < scalePrecision)
        {
            transformation = default;
            return false;
        }
        // Align scale with decimal places, to avoid unnecessary rounding errors
        transformation = new Transformation(scale, min, max);
        return true;
    }

    internal IntXY Forward(in UV xy)
    {
        int xi = (int)double.Round((xy.U - Min.U) * Scale);
        int yi = (int)double.Round((xy.V - Min.V) * Scale);
        return new IntXY(xi, yi);
    }

    internal UV Reverse(IntXY xy)
    {
        double u = (xy.X / Scale) + Min.U;
        double v = (xy.Y / Scale) + Min.V;
        return new UV(u, v);
    }

}
