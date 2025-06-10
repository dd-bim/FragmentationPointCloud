using Autodesk.Revit.DB;

using Revit.Data;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace Revit;

internal static class Extensions
{
    public static string ToStringInvariant(this double value) => value.ToString(CultureInfo.InvariantCulture);
    public static string ToStringInvariant(this int value) => value.ToString(CultureInfo.InvariantCulture);
    public static string ToStringInvariant(this float value) => value.ToString(CultureInfo.InvariantCulture);
    public static string ToStringInvariant(this decimal value) => value.ToString(CultureInfo.InvariantCulture);
    public static string ToStringInvariant(this long value) => value.ToString(CultureInfo.InvariantCulture);

    public static bool TryParseInvariant(this string value, out double result) => double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    public static bool TryParseInvariant(this string value, out decimal result) => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    public static bool TryParseInvariant(this string value, out int result) => int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    public static bool TryParseInvariant(this string value, out float result) => float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    public static bool TryParseInvariant(this string value, out long result) => long.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);

    public static string ToFullString(this XYZ xyz) => string.Format(CultureInfo.InvariantCulture, "{0:G17} {1:G17} {2:G17}", xyz.X, xyz.Y, xyz.Z);

    public static string ToFullString(this UV uv) => string.Format(CultureInfo.InvariantCulture, "{0:G17} {1:G17}", uv.U, uv.V);

    public static string ToFullWktString(this XYZ xyz) => $"POINT Z({ToFullString(xyz)})";

    public static bool TryParseUV(this ReadOnlySpan<char> input, out UV uv)
    {
        uv = UV.Zero;
        var span = input.Trim();
        int firstSpace = span.IndexOf(' ');
        if (firstSpace < 0) return false;

        var uSpan = span[..firstSpace].Trim();
        var vSpan = span[(firstSpace + 1)..].Trim();

        if (double.TryParse(uSpan, NumberStyles.Any, CultureInfo.InvariantCulture, out double u) &&
            double.TryParse(vSpan, NumberStyles.Any, CultureInfo.InvariantCulture, out double v))
        {
            uv = new UV(u, v);
            return true;
        }
        return false;
    }


    public static bool TryParseXYZ(this ReadOnlySpan<char> input, out XYZ xyz)
    {
        xyz = XYZ.Zero;
        var span = input.Trim();
        int firstSpace = span.IndexOf(' ');
        if (firstSpace < 0) return false;
        int secondSpace = span[(firstSpace + 1)..].IndexOf(' ');
        if (secondSpace < 0) return false;
        secondSpace += firstSpace + 1;

        var xSpan = span[..firstSpace].Trim();
        var ySpan = span.Slice(firstSpace + 1, secondSpace - firstSpace - 1).Trim();
        var zSpan = span[(secondSpace + 1)..].Trim();

        if (double.TryParse(xSpan, NumberStyles.Any, CultureInfo.InvariantCulture, out double x) &&
            double.TryParse(ySpan, NumberStyles.Any, CultureInfo.InvariantCulture, out double y))
        {
            int zEnd = zSpan.IndexOf(' ');
            var zValue = zEnd >= 0 ? zSpan[..zEnd] : zSpan;
            if (double.TryParse(zValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double z))
            {
                xyz = new XYZ(x, y, z);
                return true;
            }
        }
        return false;
    }

    public static bool TryParseXYZ(this string input, out XYZ xyz)
        => TryParseXYZ(input.AsSpan(), out xyz);

    public static bool TryParseWktXYZ(this ReadOnlySpan<char> input, out XYZ xyz)
    {
        xyz = XYZ.Zero;
        if (input.Length <= 7)
            return false;

        var span = input;

        int pointIdx = span.IndexOf("POINT", StringComparison.InvariantCultureIgnoreCase);
        if (pointIdx < 0 || pointIdx + 5 >= span.Length)
            return false;

        int zIdx = span[(pointIdx + 5)..].IndexOf('Z');
        if (zIdx < 0)
            return false;
        zIdx += pointIdx + 5;

        int openIdx = span[(zIdx + 1)..].IndexOf('(');
        if (openIdx < 0)
            return false;
        openIdx += zIdx + 2;

        int closeIdx = span[openIdx..].IndexOf(')');
        if (closeIdx < 0)
            return false;
        closeIdx += openIdx;

        var xyzSpan = span[openIdx..closeIdx].Trim();
        return TryParseXYZ(xyzSpan, out xyz);
    }

    public static bool TryParseWktXYZ(this string input, out XYZ xyz)
        => TryParseWktXYZ(input.AsSpan(), out xyz);

    public static XYZ FromPlaneSystem(this Plane plane, in UV uv) => (plane.XVec * uv.U) + (plane.YVec * uv.V) + plane.Origin;

    public static XYZ Min(this XYZ a, XYZ b)
    {
        return new XYZ(
            Math.Min(a.X, b.X),
            Math.Min(a.Y, b.Y),
            Math.Min(a.Z, b.Z));
    }

    public static XYZ Max(this XYZ a, XYZ b)
    {
        return new XYZ(
            Math.Max(a.X, b.X),
            Math.Max(a.Y, b.Y),
            Math.Max(a.Z, b.Z));
    }

    public static UV Min(this UV a, UV b)
    {
        return new UV(
            Math.Min(a.U, b.U),
            Math.Min(a.V, b.V));
    }

    public static UV Max(this UV a, UV b)
    {
        return new UV(
            Math.Max(a.U, b.U),
            Math.Max(a.V, b.V));
    }

    public static int SideSign(this UV p, UV a, UV b)
    {
        double det = p.Det(a, b);
        return double.Sign(det);
    }

    public static double Det(this UV p, UV a, UV b)
    {
        //double crossProduct = ((a.U - p.U) * (b.V - p.V)) - ((a.V - p.V) * (b.U - p.U));
        return (a - p).CrossProduct(b - p);
    }


    public static XYZ MaxXYZ => new(double.MaxValue, double.MaxValue, double.MaxValue);

    public static XYZ MinXYZ => new(double.MinValue, double.MinValue, double.MinValue);

    public static UV MaxUV => new(double.MaxValue, double.MaxValue);

    public static UV MinUV => new(double.MinValue, double.MinValue);

    public static UV DirectionAdd(this UV a, UV b) => new UV((a.U * b.U) - (a.V * b.V), (a.V * b.U) + (a.U * b.V));

    public static UV ToDirection(this double angle) =>
        // Convert angle in radians to a direction vector
        new(double.Cos(angle), double.Sin(angle));

    public static XYZ ToDirection(UV azimuth, UV inclination)
    {
        // U => cos
        // V => sin
        double x = inclination.V * azimuth.U;
        double y = inclination.V * azimuth.V;
        double z = inclination.U;
        var vector = new XYZ(x, y, z);
        // Normalize the vector to ensure it has a length of 1
        return vector.Normalize();
    }


    public static Octant GetOctant(this XYZ vector, XYZ normal)
    {
        var 
            octant  = vector.X < 0 ? normal.X < 0 ? Octant.None : Octant.XNeg : normal.X > 0 ? Octant.None : Octant.XPos;
            octant |= vector.Y < 0 ? normal.Y < 0 ? Octant.None : Octant.YNeg : normal.Y > 0 ? Octant.None : Octant.YPos;
            octant |= vector.Z < 0 ? normal.Z < 0 ? Octant.None : Octant.ZNeg : normal.Z > 0 ? Octant.None : Octant.ZPos;
        return octant;
    }


    public static Octant GetOctant(this XYZ vector)
    {
        var
         octant = vector.X < 0 ? Octant.XNeg : Octant.XPos;
        octant |= vector.Y < 0 ? Octant.YNeg : Octant.YPos;
        octant |= vector.Z < 0 ? Octant.ZNeg : Octant.ZPos;
        return octant;
    }


    public static Plane GetPlane(in XYZ position, in XYZ normal, in XYZ xAxis)
    {
        var nrm = normal.Normalize();
        var planeX = xAxis.Normalize();
        var planeY = nrm.CrossProduct(planeX).Normalize();
        return Plane.CreateByOriginAndBasis(position, planeX, planeY);
    }

}