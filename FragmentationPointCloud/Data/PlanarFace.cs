using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Autodesk.Revit.DB;

using Raum2D.Features;
using Raum2D.Geometry;

namespace Revit.Data;

public class PlanarFace : IEquatable<PlanarFace>
{
    private const string CsvHeader =
        "StateId;ObjectGuid;FaceId;PlaneId;BtmLft;BtmRgt;TopRgt;TopLft;BBoxMin;BBoxMax;Polygon";

    public const string ShortCsvHeader = "StateId;ObjectGuid;FaceId;Polygon";

    private const int LineCount = 11;

    private PlanarFace(Id id, string referencePlaneId, Vector planarBtmLft, Vector planarBtmRgt,
        Vector planarTopRgt, Vector planarTopLft, BBox bBox, Polygon polygon)
    {
        Id = id;
        ReferencePlaneId = referencePlaneId;
        PlanarBtmLft = planarBtmLft;
        PlanarBtmRgt = planarBtmRgt;
        PlanarTopRgt = planarTopRgt;
        PlanarTopLft = planarTopLft;
        BBox = bBox;
        Polygon = polygon;
    }

    private PlanarFace(in Id id, in ReferencePlane referencePlane, in Polygon polygon)
    {
        Id = id;
        ReferencePlaneId = referencePlane.Id;
        var envelope = polygon.EnvelopeInternal;
        PlanarBtmLft = referencePlane.Plane.FromPlaneSystem(new UV(envelope.MinX, envelope.MinY));
        PlanarBtmRgt = referencePlane.Plane.FromPlaneSystem(new UV(envelope.MinX, envelope.MaxY));
        PlanarTopRgt = referencePlane.Plane.FromPlaneSystem(new UV(envelope.MaxX, envelope.MaxY));
        PlanarTopLft = referencePlane.Plane.FromPlaneSystem(new UV(envelope.MaxX, envelope.MinY));
        Polygon = polygon;
    }

    public Id Id { get; }

    public string ReferencePlaneId { get; }

    public XYZ PlanarBtmLft { get; }

    public XYZ PlanarBtmRgt { get; }

    public XYZ PlanarTopRgt { get; }

    public XYZ PlanarTopLft { get; }

    public BoundingBoxXY BoundingBox2D => FaceFeature2D.BoundingBox;

    public SimpleFeature FaceFeature2D { get; private set; }

    public bool Equals(PlanarFace? other) => other?.Id.Equals(Id) ?? false;

    //private static bool ToNTSLinearRing(in LineString lineString, out NTS.LinearRing linearRing,
    //    bool reverse = false)
    //{
    //    NTS.GeometryFactory? gf = NTS.GeometryFactory.Floating;
    //    if (!lineString.IsClosed)
    //    {
    //        linearRing = gf.CreateLinearRing();
    //        return false;
    //    }

    //    var coo = new NTS.Coordinate[lineString.Count];
    //    if (reverse)
    //        for (var i = 0; i < lineString.Count; i++)
    //        {
    //            D2_Vector vector = lineString[i];
    //            coo[lineString.Count - 1 - i] = new NTS.Coordinate(vector.x, vector.y);
    //        }
    //    else
    //        for (var i = 0; i < lineString.Count; i++)
    //        {
    //            D2_Vector vector = lineString[i];
    //            coo[i] = new NTS.Coordinate(vector.x, vector.y);
    //        }

    //    try
    //    {
    //        linearRing = gf.CreateLinearRing(coo);
    //        return true;
    //    }
    //    catch
    //    {
    //        linearRing = gf.CreateLinearRing();
    //        return false;
    //    }
    //}

    //private static bool ToNTSPolygon(in LinearRingCollection linearRingCollection, out NTS.Polygon polygon)
    //{
    //    if (ToNTSLinearRing(linearRingCollection.Exteriors[0], out NTS.LinearRing exterior))
    //    {
    //        NTS.GeometryFactory? gf = NTS.GeometryFactory.Floating;
    //        polygon = gf.CreatePolygon(exterior);
    //        for (var i = 1; i < linearRingCollection.Exteriors.Count; i++)
    //        {
    //            if (!ToNTSLinearRing(linearRingCollection.Exteriors[i], out exterior))
    //                throw new Exception("Should not happen");
    //            NTS.Geometry? union = polygon.Union(gf.CreatePolygon(exterior));
    //            if (union.GeometryType == NTS.Geometry.TypeNamePolygon)
    //                polygon = (NTS.Polygon)union;
    //            else
    //                return false;
    //        }

    //        foreach (LineString innerRing in linearRingCollection.Interiors)
    //        {
    //            if (!ToNTSLinearRing(innerRing, out NTS.LinearRing interior, true))
    //                throw new Exception("Should not happen");
    //            NTS.Geometry? diff = polygon.Difference(gf.CreatePolygon(interior));
    //            if (diff.GeometryType == NTS.Geometry.TypeNamePolygon)
    //                polygon = (NTS.Polygon)diff;
    //            else
    //                return false;
    //        }

    //        return true;
    //    }

    //    polygon = NTS.Polygon.Empty;
    //    return false;
    //}

    //private static LineString ToLs2(in NTS.LineString lr)
    //{
    //    var vertices = new D2_Vector[lr.Count];
    //    for (var i = 0; i < vertices.Length; i++)
    //    {
    //        NTS.Coordinate? coo = lr[i];
    //        vertices[i] = new D2_Vector(coo.X, coo.Y);
    //    }

    //    return new LineString(vertices, lr.IsClosed);
    //}

    private static bool ToPolygon2d(in NTS.Polygon polygon, out Polygon polygon2)
    {
        if (!polygon.IsValid || !polygon.IsSimple)
        {
            polygon2 = default;
            return false;
        }

        LineString ext = ToLs2(polygon.ExteriorRing);
        var ints = new List<LineString>(polygon.InteriorRings.Length);
        foreach (NTS.LineString? ilr in polygon.InteriorRings) ints.Add(ToLs2(ilr));
        return Polygon.Create(ext, ints, out polygon2);
    }

    private static bool ToPolygon2d(in Plane plane, in XYZ[][] rings,
        out Polygon polygo, out double maxPlaneDist)
    {
        var rings2d = new LinearRingCollection();
        maxPlaneDist = 0.0;
        box = BBox.Empty;
        foreach (D3_LineString ring in rings)
        {
            var transformed = new D2_Vector[ring.Count];
            var zs = new double[transformed.Length];
            for (var i1 = 0; i1 < transformed.Length; i1++)
            {
                transformed[i1] = plane.ToPlaneSystem(ring[i1], out double z);
                zs[i1] = z;
            }

            var lr = new LineString(transformed, true);
            if (rings2d.Add(lr))
                for (var i = 0; i < zs.Length; i++)
                {
                    double az = Math.Abs(zs[i]);
                    if (az > maxPlaneDist) maxPlaneDist = az;
                    if (lr.Area > 0) box += ring[i];
                }
        }

        if (rings2d.Exteriors.Count > 0
            && ToNTSPolygon(rings2d, out NTS.Polygon ntsPolygon)
            && ToPolygon2d(ntsPolygon, out polygon))
            return true;
        polygon = default;
        return false;
    }

    public static bool Create(in Id id, in ReferencePlane refPlane, in XYZ[][] rings,
        out PlanarFace? planarFace, out double maxPlaneDist)
    {
        if (rings.Length < 1
            || !ToPolygon2d(refPlane.Plane, in rings, out Polygon polygon, out maxPlaneDist))
        {
            planarFace = null;
            maxPlaneDist = double.NaN;
            return false;
        }

        planarFace = new PlanarFace(id, refPlane, polygon);
        return true;
    }


    private string ToCsvString()
    {
        var line = new string[LineCount];
        line[0] = Id.StateId;
        line[1] = Id.ObjectId;
        line[2] = Id.PartId == 0 ? $"{Id.FaceId}" : $"{Id.FaceId}_{Id.PartId}";
        line[3] = ReferencePlaneId;
        line[4] = PlanarBtmLft.ToWktString();
        line[5] = PlanarBtmRgt.ToWktString();
        line[6] = PlanarTopRgt.ToWktString();
        line[7] = PlanarTopLft.ToWktString();
        line[8] = BBox.Min.ToWktString();
        line[9] = BBox.Max.ToWktString();
        line[10] = "POLYGON";
        return string.Join(";", line);
    }

    private static bool TryParseCsvLine(string line, out PlanarFace? planarFace, out string error)
    {
        if (string.IsNullOrEmpty(line))
        {
            error = "PlanarFace.ParseCsvLine: Input string is null or empty";
            planarFace = null;
            return false;
        }

        string[] strings = line.Split([';']);
        if (strings.Length == LineCount
            && Vector.TryParseWkt(strings[4], out Vector btmLft)
            && Vector.TryParseWkt(strings[5], out Vector btmRgt)
            && Vector.TryParseWkt(strings[6], out Vector topRgt)
            && Vector.TryParseWkt(strings[7], out Vector topLft)
            && Vector.TryParseWkt(strings[8], out Vector boxMin)
            && Vector.TryParseWkt(strings[9], out Vector boxMax)
            && Polygon.TryParseWkt(strings[10], out Polygon polygon))
        {
            error = string.Empty;
            planarFace = new PlanarFace(new Id(strings[0], strings[1], strings[2]), strings[3], btmLft, btmRgt, topRgt,
                topLft, new BBox(boxMin, boxMax), polygon);
            return true;
        }

        error = $"PlanarFace.ParseCsvLine: Line: \r\n{line}\r\n is not readable";
        planarFace = null;
        return false;
    }

    public override bool Equals(object? obj) => obj is PlanarFace face && Equals(face);

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(in PlanarFace left, in PlanarFace right) => left.Equals(right);

    public static bool operator !=(in PlanarFace left, in PlanarFace right) => !(left == right);

    public static void WriteObj(in string path, in IReadOnlyDictionary<string, ReferencePlane> planes,
        in IEnumerable<PlanarFace> planarFaces)
    {
        using StreamWriter file = File.CreateText(path + ".obj");
        var faces = new List<string>();
        var vertexCnt = 0;

        void AddLs(Plane plane, LineString ls)
        {
            int first = vertexCnt;
            var face = new StringBuilder("f");
            for (var i = 1; i < ls.Count; i++)
            {
                file.WriteLine($"v {plane.FromPlaneSystem(ls[i])}");
                face.AppendFormat(" {0}", first + i);
            }

            faces.Add(face.ToString());
            vertexCnt += ls.Count - 1;
        }

        foreach (PlanarFace pf in planarFaces)
        {
            faces.Add($"# {pf.Id}");
            foreach (LineString ls in pf.Polygon) AddLs(planes[pf.ReferencePlaneId].Plane, ls);
        }

        foreach (string face in faces) file.WriteLine(face);
    }

    public static void WriteCsv(in string path, in IReadOnlyCollection<PlanarFace> planarFaces)
    {
        using var csv = File.CreateText(path);
        csv.WriteLine(CsvHeader);
        foreach (var pf in planarFaces) csv.WriteLine(pf.ToCsvString());
    }

    public static HashSet<PlanarFace> ReadCsv(in string path, out string[] lineErrors, out string error)
    {
        string[] lines;
        var faces = new HashSet<PlanarFace>();
        try
        {
            lines = File.ReadAllLines(path);
        }
        catch (Exception e)
        {
            lineErrors = [];
            error = "PlanarFace.ReadCsv: " + e.Message;
            return faces;
        }

        if (lines.Length > 1)
        {
            var errors = new List<string>();
            for (var i = 1; i < lines.Length; i++)
            {
                if (TryParseCsvLine(lines[i], out PlanarFace? pf, out error))
                {
                    faces.Add(pf!);
                    continue;
                }

                errors.Add($"Line {i + 1} has Error: {error}");
                error = string.Empty;
            }

            lineErrors = errors.ToArray();
            error = string.Empty;
            return faces;
        }

        lineErrors = [];
        error = "PlanarFace.ReadCsv: CSV-File has no data lines";
        return faces;
    }

}