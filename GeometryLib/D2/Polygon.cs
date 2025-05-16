using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using GeometryLib.Double.D3;

/* Unmerged change from project 'GeometryLib (netstandard2.1)'
Before:
using System.Collections.Immutable;
using static System.Math;
After:
using System.Collections.Immutable;

using static System.Math;
*/
/* Unmerged change from project 'GeometryLib (netstandard2.0)'
Before:
using System.Collections.Immutable;
using static System.Math;
After:
using System.Collections.Immutable;

using static System.Math;
*/

namespace GeometryLib.Double.D2;

public readonly struct Polygon : IReadOnlyList<LineString>
{
    public ImmutableArray<LineString> Rings { get; }

    public BBox BBox => Rings[0].BBox;

    public double Area { get; }

    public int Count => Rings.Length;

    public LineString this[int index] => Rings[index];

    private Polygon(IReadOnlyList<LineString> rings, double area)
    {
        Rings = rings.ToImmutableArray();
        Area = area;
    }

    public Polygon(in LineString ring)
    {
        if (!ring.IsLinearRing)
            throw new ArgumentException($"Parameter {nameof(ring)} must be a LinearRing");
        Rings = ImmutableArray.Create(ring.Area < 0 ? ring.Reverse() : ring);
        Area = ring.Area;
    }

    public Polygon ChangePlane(in Plane oldPlane, in Plane newPlane)
    {
        var p3 = new List<D3.LineString>(Rings.Length);
        foreach (LineString ls in Rings) p3.Add(new D3.LineString(oldPlane, ls));
        var p2 = new List<LineString>(Rings.Length);
        foreach (D3.LineString ls in p3) p2.Add(new LineString(newPlane, ls));
        return new Polygon(p2, Area);
    }

    public static bool Create(in IReadOnlyList<Vector> ring, out Polygon polygon)
    {
        LineString lineString = ring is LineString ls ? ls : new LineString(ring, true);
        if (!lineString.IsLinearRing)
        {
            polygon = default;
            return false;
        }

        if (lineString.Area < 0) lineString = lineString.Reverse();
        polygon = new Polygon(lineString);
        return true;
    }

    public static bool Create(in IReadOnlyList<LineString>? rings, out Polygon polygon)
    {
        if (rings is null || rings.Count < 1)
        {
            polygon = default;
            return false;
        }

        LineString ls = rings[0];
        if (!ls.IsLinearRing)
        {
            polygon = default;
            return false;
        }

        double area = ls.Area;
        bool reverse = area < 0;
        var lss = new List<LineString>(rings.Count)
        {
            reverse ? ls.Reverse() : ls
        };

        for (var i = 1; i < rings.Count; i++)
        {
            ls = rings[i];
            if (ls.Area == 0.0) continue;
            if (!ls.IsLinearRing || ls.Area > 0 != reverse || !lss[0].BBox.Encloses(ls.BBox))
            {
                polygon = default;
                return false;
            }

            area += ls.Area;
            lss.Add(reverse ? ls.Reverse() : ls);
        }

        area = reverse ? -area : area;

        if (area <= 0.0)
        {
            polygon = default;
            return false;
        }

        polygon = new Polygon(lss, area);
        return true;
    }

    public static bool Create(in LineString exterior, in IReadOnlyList<LineString>? interiors, out Polygon polygon)
    {
        if (!exterior.IsLinearRing)
        {
            polygon = default;
            return false;
        }

        var lrs = new List<LineString>(1 + (interiors?.Count ?? 0))
        {
            exterior.Area < 0 ? exterior.Reverse() : exterior
        };
        double area = Math.Abs(exterior.Area);
        if (interiors is not null && interiors.Count > 0)
            foreach (LineString lr in interiors)
            {
                if (lr.Area == 0.0) continue;
                if (!lr.IsLinearRing || !exterior.BBox.Encloses(lr.BBox))
                {
                    polygon = default;
                    return false;
                }

                area -= Math.Abs(lr.Area);
                lrs.Add(lr.Area > 0 ? lr.Reverse() : lr);
            }

        if (area <= 0.0)
        {
            polygon = default;
            return false;
        }

        polygon = new Polygon(lrs, area);
        return true;
    }

    public bool IsPointInPolygon(in Vector p)
    {
        if (!Rings[0].IsPointInPolygon(p)) return false;

        for (var i = 1; i < Rings.Length; i++)
        {
            if (Rings[i].IsPointInPolygon(p))
                return false;
        }

        return true;
    }

    public string ToString(string separator, string lineStringSeparator = ",", string vectorSeparator = " ")
    {
        var strings = new string[Rings.Length];
        for (var i = 0; i < Rings.Length; i++) strings[i] = Rings[i].ToString(lineStringSeparator, vectorSeparator);
        return '(' + string.Join(separator, strings) + ')';
    }

    public override string ToString()
    {
        return ToString(",");
    }

    public IEnumerator<LineString> GetEnumerator()
    {
        return ((IEnumerable<LineString>)Rings).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public static bool TryParse(in string input, out Polygon polygon)
    {
        int si = input.IndexOf('(') + 1;
        int lastei = input.LastIndexOf(')');
        if (si > 0 && lastei - si > 16)
        {
            si = input.IndexOf('(', si);
            if (si > 0)
            {
                ImmutableArray<LineString>.Builder rings = ImmutableArray.CreateBuilder<LineString>();
                int ei = input.IndexOf(')', si) + 1;
                while (ei > si && ei <= lastei && LineString.TryParse(input[si..ei], out LineString lineString, true))
                {
                    rings.Add(lineString);
                    int ci = input.IndexOf(',', ei);
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
        int wi = input.IndexOf("Polygon", StringComparison.InvariantCultureIgnoreCase);
        return wi >= 0 && TryParse(input[(wi + 7)..], out polygon);
    }
}