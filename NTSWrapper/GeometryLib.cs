using System;
using System.Collections.Generic;
using System.Text;

using D2 = GeometryLib.Double.D2;

using D3 = GeometryLib.Double.D3;

using NTS = NetTopologySuite.Geometries;

namespace NTSWrapper
{
    public static class GeometryLib
    {
        public static NTS.Point ToNTSPoint(in D2.Vector vector) => new NTS.Point(vector.x, vector.y);

        public static NTS.Coordinate ToNTSCoordinate(in D2.Vector vector) => new NTS.Coordinate(vector.x, vector.y);

        public static NTS.CoordinateZ ToNTSCoordinate(in D3.Vector vector) => new NTS.CoordinateZ(vector.x, vector.y, vector.z);

        public static bool ToNTSLinearRing(in D2.LineString lineString, out NTS.LinearRing linearRing, bool reverse = false)
        {
            var gf = NTS.GeometryFactory.Floating;
            if (!lineString.IsClosed)
            {
                linearRing = gf.CreateLinearRing();
                return false;
            }
            var coo = new NTS.Coordinate[lineString.Count];
            if (reverse)
            {
                for (int i = 0; i < lineString.Count; i++)
                {
                    var vector = lineString[i];
                    coo[lineString.Count - 1 - i] = new NTS.Coordinate(vector.x, vector.y);
                }
            }
            else
            {
                for (int i = 0; i < lineString.Count; i++)
                {
                    var vector = lineString[i];
                    coo[i] = new NTS.Coordinate(vector.x, vector.y);
                }
            }

            try
            {
                linearRing = gf.CreateLinearRing(coo);
                return true;
            }
            catch
            {
                linearRing = gf.CreateLinearRing();
                return false;
            }
        }

        public static bool ToNTSLinearRing(in D3.LineString lineString, out NTS.LinearRing linearRing)
        {
            var gf = NTS.GeometryFactory.Floating;
            if (!lineString.IsClosed)
            {
                linearRing = gf.CreateLinearRing();
                return false;
            }
            var coo = new NTS.CoordinateZ[lineString.Count];
            for (int i = 0; i < lineString.Count; i++)
            {
                var vector = lineString[i];
                coo[i] = new NTS.CoordinateZ(vector.x, vector.y, vector.z);
            }
            try
            {
                linearRing = gf.CreateLinearRing(coo);
                return true;
            }
            catch
            {
                linearRing = gf.CreateLinearRing();
                return false;
            }
        }

        public static NTS.Polygon ToNTSPolygon(in D2.Polygon polyonD2)
        {
            if (ToNTSLinearRing(polyonD2[0], out var exterior))
            {
                var gf = NTS.GeometryFactory.Floating;
                if (polyonD2.Count == 1)
                {
                    return gf.CreatePolygon(exterior);
                }
                var interiors = new NTS.LinearRing[polyonD2.Count - 1];
                for (int i = 1; i < polyonD2.Count; i++)
                {
                    if (!ToNTSLinearRing(polyonD2[i], out var interior))
                    {
                        throw new Exception("Should not happen");
                    }
                    interiors[i - 1] = interior ?? throw new Exception("Should not happen");
                }
                return gf.CreatePolygon(exterior, interiors);
            }
            throw new Exception("Should not happen");
        }

        public static bool ToNTSPolygon(in D2.LinearRingCollection linearRingCollection, out NTS.Polygon polygon)
        {
            if (ToNTSLinearRing(linearRingCollection.Exteriors[0], out var exterior))
            {
                var gf = NTS.GeometryFactory.Floating;
                polygon = gf.CreatePolygon(exterior);
                for (int i = 1; i < linearRingCollection.Exteriors.Count; i++)
                {
                    if (!ToNTSLinearRing(linearRingCollection.Exteriors[i], out exterior))
                        throw new Exception("Should not happen");
                    var union = polygon.Union(gf.CreatePolygon(exterior));
                    if (union.GeometryType == NTS.Geometry.TypeNamePolygon)
                        polygon = (NTS.Polygon)union;
                    else
                        return false;
                }
                foreach (var interiorlr in linearRingCollection.Interiors)
                {
                    if (!ToNTSLinearRing(interiorlr, out var interior, true))
                        throw new Exception("Should not happen");
                    var diff = polygon.Difference(gf.CreatePolygon(interior));
                    if (diff.GeometryType == NTS.Geometry.TypeNamePolygon)
                        polygon = (NTS.Polygon)diff;
                    else
                        return false;
                }
                return true;
            }
            polygon = NTS.Polygon.Empty;
            return false;
        }


        public static bool ToPolygon2d(in D3.Plane plane, in IReadOnlyCollection<D3.LineString> rings, out D2.Polygon polygon, out D3.BBox box, out double maxPlaneDist)
        {
            var rings2d = new D2.LinearRingCollection();
            maxPlaneDist = 0.0;
            box = D3.BBox.Empty;
            foreach (var ring in rings)
            {
                var vertices = plane.System.ToSystem(ring, out var zs);
                var lr = new D2.LineString(vertices, true);
                if (rings2d.Add(lr))
                {
                    for (int i = 0; i < zs.Length; i++)
                    {
                        double az = Math.Abs(zs[i]);
                        if (az > maxPlaneDist)
                        {
                            maxPlaneDist = az;
                        }
                        if (lr.Area > 0)
                        {
                            box = box.Extend(ring[i]);
                        }
                    }
                }
            }
            if (rings2d.Exteriors.Count > 0
                && ToNTSPolygon(rings2d, out var ntsPolygon)
                && ToPolygon2d(ntsPolygon, out polygon))
            {
                return true;
            }
            polygon = default;
            return false;
        }

        public static NTS.Polygon ToNTSPolygon(in D3.Polygon polyonD3)
        {
            if (ToNTSLinearRing(polyonD3[0], out var exterior))
            {
                var gf = NTS.GeometryFactory.Floating;
                if (polyonD3.Count == 1)
                {
                    return gf.CreatePolygon(exterior);
                }
                var interiors = new NTS.LinearRing[polyonD3.Count - 1];
                for (int i = 1; i < polyonD3.Count; i++)
                {
                    if (!ToNTSLinearRing(polyonD3[i], out var interior))
                    {
                        throw new Exception("Should not happen");
                    }
                    interiors[i - 1] = interior ?? throw new Exception("Should not happen");
                }
                return gf.CreatePolygon(exterior, interiors);
            }
            throw new Exception("Should not happen");
        }

        private static D2.LineString toLs2(in NTS.LineString lr)
        {
            var vertices = new D2.Vector[lr.Count];
            for (int i = 0; i < vertices.Length; i++)
            {
                var coo = lr[i];
                vertices[i] = new D2.Vector(coo.X, coo.Y);
            }
            return new D2.LineString(vertices, lr.IsClosed);
        }

        //private static D3.LineString toLs3(in NTS.LineString lr)
        //{
        //    var vertices = new D3.Vector[lr.Count];
        //    for (int i = 0; i < vertices.Length; i++)
        //    {
        //        var coo = lr[i];
        //        vertices[i] = new D3.Vector(coo.X, coo.Y, coo.Z);
        //    }
        //    return new D3.LineString(vertices);
        //}


        //public static D3.Polygon ToPolygon(in NTS.Polygon polygon, in Plane? plane)
        //{
        //    if(polygon.Coordinate is NTS.CoordinateZ || polygon.Coordinate is NTS.CoordinateZM)
        //    {
        //        var linestrings = new D3.LineString[polygon.InteriorRings.Length + 1];
        //        linestrings[0] = toLs3(polygon.ExteriorRing);
        //        for (int i = 0; i < polygon.InteriorRings.Length; i++)
        //        {
        //            linestrings[i + 1] = toLs3(polygon.InteriorRings[i]);
        //        }
        //        if (!plane.HasValue)
        //        {

        //        }
        //        if(D3.Polygon.Create()
        //    }
        //    else
        //    {
        //        var linestrings = new D2.LineString[polygon.InteriorRings.Length + 1];
        //    }
        //}

        public static bool ToPolygon2d(in NTS.Polygon polygon, out D2.Polygon polygon2)
        {
            if (!polygon.IsValid || !polygon.IsSimple)
            {
                polygon2 = default;
                return false;
            }

            var ext = toLs2(polygon.ExteriorRing);
            var ints = new List<D2.LineString>(polygon.InteriorRings.Length);
            foreach (var ilr in polygon.InteriorRings)
            {
                ints.Add(toLs2(ilr));
            }
            return D2.Polygon.Create(ext, ints, out polygon2);
        }

        public static NTS.Prepared.PreparedPolygon ToPrepared(in NTS.Polygon poly) => new NTS.Prepared.PreparedPolygon(poly);

    }
}
