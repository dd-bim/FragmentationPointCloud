//using Autodesk.Revit.DB;
//using NetTopologySuite.Geometries;
//using Point = NetTopologySuite.Geometries.Point;

//namespace Revit;

//public static class NTSExtensions
//{
//    public static readonly GeometryFactory GF = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(new PrecisionModel(1000000));

//    public static XYZ FromPlaneSystem(this Plane plane, in Point xy)
//    {
//        return (plane.XVec * xy.X) + (plane.YVec * xy.Y) + plane.Origin;
//    }

//    public static XYZ FromPlaneSystem(this Plane plane, in Coordinate xy)
//    {
//        return (plane.XVec * xy.X) + (plane.YVec * xy.Y) + plane.Origin;
//    }

//    public static Point ToNTSPoint(this XYZ xyz)
//    {
//       return  GF.CreatePoint(new CoordinateZ(xyz.X, xyz.Y, xyz.Z));
//    }

//    public static Point ToNTSPoint(this UV uv)
//    {
//       return GF.CreatePoint(new Coordinate(uv.U, uv.V));
//    }

//    public static UV ToUV(this Point point)
//    {
//        return new UV(point.X, point.Y));
//    }

//    public static XYZ ToXYZ(this Point point)
//    {
//       return new XYZ(point.X, point.Y, point.Z);
//    }


//}