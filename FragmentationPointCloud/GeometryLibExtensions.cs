using Autodesk.Revit.DB;
using GeometryLib.D3;
using System;
using System.Globalization;

namespace Revit;

public static class GeometryLibExtensions
{
    public static XYZ ToXYZ(this Vector vector)
    {
        return new XYZ(vector.x, vector.y, vector.z);
    }

    public static XYZ ToXYZ(this Direction direction)
    {
        return new XYZ(direction.x, direction.y, direction.z);
    }

    public static Vector ToVector(this XYZ xyz)
    {
        return new Vector(xyz.X, xyz.Y, xyz.Z);
    }

    public static Direction ToDirection(this XYZ xyz)
    {
        return new Direction(xyz.X, xyz.Y, xyz.Z);
    }

    public static string ToFullString(this XYZ xyz)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:G17} {1:G17} {2:G17}", xyz.X, xyz.Y, xyz.Z);
    }

    public static bool TryParseXYZ(this string input, out XYZ xyz)
    {
        string str = input.Trim();
        if (!string.IsNullOrEmpty(str))
        {
            string[] split = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (split.Length >= 3
                && double.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double x)
                && double.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y)
                && double.TryParse(split[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double z))
            {
                xyz = new XYZ(x, y, z);
                return true;
            }
        }

        xyz = XYZ.Zero;
        return false;
    }

    public static bool TryParseWktXYZ(this string input, out XYZ xyz)
    {
        if (!string.IsNullOrEmpty(input) && input.Length > 7)
        {
            int start = input.IndexOf("POINT", StringComparison.InvariantCultureIgnoreCase);
            int end = start + 5;
            if (start >= 0 && end < input.Length)
            {
                start = input.IndexOf("Z", end, StringComparison.InvariantCultureIgnoreCase);
                end = start + 1;
                if (start > 4 && end < input.Length)
                {
                    start = input.IndexOf('(', end) + 1;
                    if (start > 6 && start < input.Length)
                    {
                        end = input.IndexOf(')', start);
                        if (end > start + 4) 
                            return input[start..end].TryParseXYZ(out xyz);
                    }
                }
            }
        }

        xyz = XYZ.Zero;
        return false;
    }

    public static XYZ FromPlaneSystem(this Autodesk.Revit.DB.Plane plane, in UV uv)
    {
        return (plane.XVec * uv.U) + (plane.YVec * uv.V) + plane.Origin;
    }


}