using Autodesk.Revit.DB;

using Raum2D.Geometry;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    
    public static string ToFullWktString(this XYZ xyz) => $"POLYGON Z({ToFullString(xyz)})";

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

    public static UV ToUV(this DecimalXY xy) => new UV((double)xy.X, (double)xy.Y);

    public static DecimalXY ToDecimalXY(this UV uv, int digits) => new(
        decimal.Round((decimal)uv.U, digits), 
        decimal.Round((decimal)uv.V, digits));

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

    public static XYZ AllMax => new(double.MaxValue, double.MaxValue, double.MaxValue);

    public static XYZ AllMin => new(double.MinValue, double.MinValue, double.MinValue);

}