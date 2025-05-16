using Autodesk.Revit.DB;
using GeometryLib.D3;

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
}