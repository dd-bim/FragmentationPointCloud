//using Autodesk.Revit.DB;

//using Raum2D.Features;
//using Raum2D.Geometry;

//using System;
//using System.Collections.Generic;
//using System.IO;

//namespace Revit.Data;

///// <summary>
///// Represents a planar face in 3D space, defined by its bounding box, corners, and associated reference plane.
///// </summary>
///// <remarks>A <see cref="PlanarFace"/> is a geometric entity that describes a flat surface in 3D space. It is
///// defined by its unique identifier, a reference plane, and its bounding box corners. The class provides methods for
///// creating, exporting, and parsing planar faces, as well as utilities for working with their geometric data.</remarks>
//public sealed record PlanarFace : IEquatable<PlanarFace>
//{
//    private const string CsvHeader =
//        "StateId;ObjectGuid;FaceId;PlaneId;BtmLft;BtmRgt;TopRgt;TopLft;BBoxMin;BBoxMax;Polygon";

//    public const string ShortCsvHeader = "StateId;ObjectGuid;FaceId;Polygon";

//    private const int LineCount = 11;

//    private PlanarFace(Id id, string referencePlaneId, XYZ btmLft, XYZ btmRgt, XYZ topRgt, XYZ topLft, XYZ bBoxMin, XYZ bBoxMax, SimpleFeature faceFeature2D)
//    {
//        Id = id;
//        ReferencePlaneId = referencePlaneId;
//        BtmLft = btmLft;
//        BtmRgt = btmRgt;
//        TopRgt = topRgt;
//        TopLft = topLft;
//        BBoxMin = bBoxMin;
//        BBoxMax = bBoxMax;
//        FaceFeature2D = faceFeature2D;
//    }

//    private PlanarFace(in Id id, ReferencePlane referencePlane, SimpleFeature polygon, in XYZ min, in XYZ max)
//    {
//        Id = id;
//        ReferencePlaneId = referencePlane.Id;
//        var bbox = polygon.BoundingBox;
//        BtmLft = referencePlane.Plane.FromPlaneSystem(new UV((double)bbox.MinX, (double)bbox.MinY));
//        BtmRgt = referencePlane.Plane.FromPlaneSystem(new UV((double)bbox.MinX, (double)bbox.MaxY));
//        TopRgt = referencePlane.Plane.FromPlaneSystem(new UV((double)bbox.MaxX, (double)bbox.MaxY));
//        TopLft = referencePlane.Plane.FromPlaneSystem(new UV((double)bbox.MaxX, (double)bbox.MinY));
//        BBoxMin = min;
//        BBoxMax = max;
//        FaceFeature2D = polygon;
//    }

//    /// <summary>
//    /// Gets the unique identifier for the entity.
//    /// </summary>
//    public Id Id { get; }

//    /// <summary>
//    /// Gets the unique identifier of the reference plane associated with this object.
//    /// </summary>
//    public string ReferencePlaneId { get; }

//    /// <summary>
//    /// Gets the bottom-left corner of the planar surface in 3D space.
//    /// </summary>
//    public XYZ BtmLft { get; }

//    /// <summary>
//    /// Gets the bottom-right corner of the planar surface in 3D space.
//    /// </summary>
//    public XYZ BtmRgt { get; }

//    /// <summary>
//    /// Gets the top-right corner of the planar bounding box.
//    /// </summary>
//    public XYZ TopRgt { get; }

//    /// <summary>
//    /// Gets the top-left corner point of the planar surface in 3D space.
//    /// </summary>
//    public XYZ TopLft { get; }

//    /// <summary>
//    /// Gets the minimum corner of the bounding box.
//    /// </summary>
//    public XYZ BBoxMin { get; }

//    /// <summary>
//    /// Gets the maximum corner of the bounding box in 3D space.
//    /// </summary>
//    public XYZ BBoxMax { get; }

//    /// <summary>
//    /// Gets the 2D facial feature data associated with the current instance.
//    /// </summary>
//    public SimpleFeature FaceFeature2D { get; private set; }

//    public bool Equals(PlanarFace other) => other.Id.Equals(Id);

//    public override int GetHashCode() => Id.GetHashCode();

//    /// <summary>
//    /// Converts a set of 3D coordinate rings into a 2D polygon representation on a specified plane.
//    /// </summary>
//    /// <remarks>This method projects 3D coordinates onto the specified plane and converts them into a 2D
//    /// polygon representation. The maximum distance of any point from the plane is calculated and returned via the
//    /// <paramref name="maxPlaneDist"/> parameter. The resulting polygon is created with the specified precision,
//    /// determined by the <paramref name="digits"/> parameter.</remarks>
//    /// <param name="plane">The plane onto which the 3D coordinates will be projected.</param>
//    /// <param name="rings">An array of coordinate rings, where each ring is an array of <see cref="XYZ"/> points.</param>
//    /// <param name="digits">The number of decimal places to retain when converting coordinates to 2D.</param>
//    /// <param name="polygon">When this method returns, contains the resulting 2D polygon as a <see cref="SimpleFeature"/> object,  or an
//    /// empty polygon if the conversion fails.</param>
//    /// <param name="maxPlaneDist">When this method returns, contains the maximum distance of any point from the plane during projection.</param>
//    /// <returns><see langword="true"/> if the resulting polygon is not empty; otherwise, <see langword="false"/>.</returns>
//    private static bool ToPolygon2d(Plane plane, XYZ[][] rings, int digits,
//       out SimpleFeature polygon,
//       out double maxPlaneDist)
//    {
//        var rings2d = new DecimalXY[rings.Length][];
//        maxPlaneDist = 0.0;
//        for (int i = 0; i < rings.Length; i++)
//        {
//            var ring = rings[i];
//            var transformed = new DecimalXY[ring.Length];
//            for (int j = 0; j < ring.Length; j++)
//            {
//                var xyz = ring[j];
//                plane.Project(xyz, out var uv, out double z);
//                transformed[j] = uv.ToDecimalXY(digits);
//                maxPlaneDist = double.Max(maxPlaneDist, double.Abs(z));
//            }
//            rings2d[i] = transformed;
//        }

//        polygon = SimpleFeature.Create(rings2d, SFType.POLYGON);
//        return !polygon.IsEmpty;
//    }

//#nullable enable

//    /// <summary>
//    /// Creates a planar face from the specified reference plane and a set of 3D ring points.
//    /// </summary>
//    /// <remarks>This method projects the input 3D points onto the specified reference plane to create a 2D
//    /// polygon. If the input data is invalid or the projection fails, the method returns <see langword="false"/> and
//    /// outputs <see langword="null"/> for <paramref name="planarFace"/> and <see cref="double.NaN"/> for <paramref
//    /// name="maxPlaneDist"/>.</remarks>
//    /// <param name="id">The unique identifier associated with the planar face.</param>
//    /// <param name="refPlane">The reference plane used to define the planar face.</param>
//    /// <param name="rings">A jagged array of 3D points representing the rings that define the planar face.</param>
//    /// <param name="planarFace">When this method returns, contains the created <see cref="PlanarFace"/> if the operation succeeds; otherwise,
//    /// <see langword="null"/>.</param>
//    /// <param name="maxPlaneDist">When this method returns, contains the maximum distance of the input points from the reference plane. If the
//    /// operation fails, this will be set to <see cref="double.NaN"/>.</param>
//    /// <param name="digits2d">The number of decimal places to use for 2D precision when projecting points onto the reference plane. Defaults
//    /// to 7.</param>
//    /// <returns><see langword="true"/> if the planar face was successfully created; otherwise, <see langword="false"/>.</returns>
//    public static bool Create(in Id id, ReferencePlane refPlane, XYZ[][] rings,
//        out PlanarFace? planarFace, out double maxPlaneDist, int digits2d = 7)
//    {
//        var plane = refPlane.Plane;
//        if (rings.Length < 1
//            || !ToPolygon2d(plane, rings, digits2d, out var polygon, out maxPlaneDist))
//        {
//            planarFace = null;
//            maxPlaneDist = double.NaN;
//            return false;
//        }
//        var min = Extensions.MaxXYZ;
//        var max = Extensions.MinXYZ;
//        foreach (var point in polygon.Points())
//        {
//            var uv = point.ToUV();
//            var xyz = plane.FromPlaneSystem(uv);
//            min = min.Min(xyz);
//            max = max.Max(xyz);
//        }
//        planarFace = new PlanarFace(id, refPlane, polygon, min, max);
//        return true;
//    }


//    /// <summary>
//    /// Converts the current object to a CSV-formatted string representation.
//    /// </summary>
//    /// <remarks>The resulting string contains the object's properties and geometric data serialized into a 
//    /// semicolon-separated format. This format is suitable for exporting or logging purposes.</remarks>
//    /// <returns>A semicolon-separated string representing the object's state and associated geometric data.</returns>
//    private string ToCsvString()
//    {
//        string[] line =
//        [
//            Id.StateId,
//            Id.ObjectId,
//            Id.PartId == 0 ? $"{Id.FaceId}" : $"{Id.FaceId}_{Id.PartId}",
//            ReferencePlaneId,
//            BtmLft.ToFullWktString(),
//            BtmRgt.ToFullWktString(),
//            TopRgt.ToFullWktString(),
//            TopLft.ToFullWktString(),
//            BBoxMin.ToFullWktString(),
//            BBoxMax.ToFullWktString(),
//            FaceFeature2D.ToString(),
//        ];
//        return string.Join(";", line);
//    }

//    /// <summary>
//    /// Attempts to parse a CSV-formatted line into a <see cref="PlanarFace"/> object.
//    /// </summary>
//    /// <remarks>The input line must be a non-empty, semicolon-delimited string containing the required fields
//    /// in the expected order: StateId, ObjectGuid, FaceId, PlaneId, BtmLft, BtmRgt, TopRgt, TopLft, BBoxMin, BBoxMax,
//    /// and Polygon. If any field is missing, empty, or invalid, the method will return <see langword="false"/> and
//    /// provide an error message.</remarks>
//    /// <param name="line">The input line as a <see cref="ReadOnlySpan{T}"/> of characters, representing a CSV-formatted string.</param>
//    /// <param name="planarFace">When this method returns, contains the parsed <see cref="PlanarFace"/> object if the parsing was successful;
//    /// otherwise, <see langword="null"/>.</param>
//    /// <param name="error">When this method returns, contains an error message describing why the parsing failed, if applicable.</param>
//    /// <returns><see langword="true"/> if the line was successfully parsed into a <see cref="PlanarFace"/> object; otherwise,
//    /// <see langword="false"/>.</returns>
//    private static bool TryParseCsvLine(ReadOnlySpan<char> line, out PlanarFace? planarFace, out string error)
//    {
//        planarFace = null;
//        error = string.Empty;

//        if (line.IsEmpty)
//        {
//            error = "PlanarFace.ParseCsvLine: Input string is null or empty";
//            return false;
//        }

//        // Felder per Span extrahieren (StateId;ObjectGuid;FaceId;PlaneId;BtmLft;BtmRgt;TopRgt;TopLft;BBoxMin;BBoxMax;Polygon)
//        int idx1 = line.IndexOf(';');
//        if (idx1 < 0) goto NotReadable;
//        int idx2 = line[(idx1 + 1)..].IndexOf(';');
//        if (idx2 < 0) goto NotReadable;
//        idx2 += idx1 + 1;
//        int idx3 = line[(idx2 + 1)..].IndexOf(';');
//        if (idx3 < 0) goto NotReadable;
//        idx3 += idx2 + 1;
//        int idx4 = line[(idx3 + 1)..].IndexOf(';');
//        if (idx4 < 0) goto NotReadable;
//        idx4 += idx3 + 1;
//        int idx5 = line[(idx4 + 1)..].IndexOf(';');
//        if (idx5 < 0) goto NotReadable;
//        idx5 += idx4 + 1;
//        int idx6 = line[(idx5 + 1)..].IndexOf(';');
//        if (idx6 < 0) goto NotReadable;
//        idx6 += idx5 + 1;
//        int idx7 = line[(idx6 + 1)..].IndexOf(';');
//        if (idx7 < 0) goto NotReadable;
//        idx7 += idx6 + 1;
//        int idx8 = line[(idx7 + 1)..].IndexOf(';');
//        if (idx8 < 0) goto NotReadable;
//        idx8 += idx7 + 1;
//        int idx9 = line[(idx8 + 1)..].IndexOf(';');
//        if (idx9 < 0) goto NotReadable;
//        idx9 += idx8 + 1;
//        int idx10 = line[(idx9 + 1)..].IndexOf(';');
//        if (idx10 < 0) goto NotReadable;
//        idx10 += idx9 + 1;

//        var stateIdSpan = line[..idx1].Trim();
//        var objectGuidSpan = line[(idx1 + 1)..idx2].Trim();
//        var faceIdSpan = line[(idx2 + 1)..idx3].Trim();
//        var planeIdSpan = line[(idx3 + 1)..idx4].Trim();
//        var btmLftSpan = line[(idx4 + 1)..idx5].Trim();
//        var btmRgtSpan = line[(idx5 + 1)..idx6].Trim();
//        var topRgtSpan = line[(idx6 + 1)..idx7].Trim();
//        var topLftSpan = line[(idx7 + 1)..idx8].Trim();
//        var bboxMinSpan = line[(idx8 + 1)..idx9].Trim();
//        var bboxMaxSpan = line[(idx9 + 1)..idx10].Trim();
//        var polygonSpan = line[(idx10 + 1)..].Trim();

//        if (stateIdSpan.IsEmpty || objectGuidSpan.IsEmpty || faceIdSpan.IsEmpty || planeIdSpan.IsEmpty ||
//            btmLftSpan.IsEmpty || btmRgtSpan.IsEmpty || topRgtSpan.IsEmpty || topLftSpan.IsEmpty ||
//            bboxMinSpan.IsEmpty || bboxMaxSpan.IsEmpty || polygonSpan.IsEmpty)
//            goto NotReadable;

//        // FaceId und PartId extrahieren
//        string faceIdStr = faceIdSpan.ToString();
//        string faceIdOnly = faceIdStr;
//        int partId = 0;
//        int underscoreIdx = faceIdStr.IndexOf('_');
//        if (underscoreIdx > 0 && underscoreIdx < faceIdStr.Length - 1)
//        {
//            faceIdOnly = faceIdStr[..underscoreIdx];
//            _ = int.TryParse(faceIdStr[(underscoreIdx + 1)..], out partId);
//        }

//        if (btmLftSpan.TryParseWktXYZ(out var btmLft) &&
//            btmRgtSpan.TryParseWktXYZ(out var btmRgt) &&
//            topRgtSpan.TryParseWktXYZ(out var topRgt) &&
//            topLftSpan.TryParseWktXYZ(out var topLft) &&
//            bboxMinSpan.TryParseWktXYZ(out var boxMin) &&
//            bboxMaxSpan.TryParseWktXYZ(out var boxMax) &&
//            SimpleFeature.TryParseWkt(polygonSpan, out var polygon))
//        {
//            planarFace = new PlanarFace(
//                new Id(stateIdSpan.ToString(), objectGuidSpan.ToString(), faceIdOnly, partId),
//                planeIdSpan.ToString(),
//                btmLft, btmRgt, topRgt, topLft, boxMin, boxMax, polygon
//            );
//            return true;
//        }

//    NotReadable:
//        error = $"PlanarFace.ParseCsvLine: Line: \r\n{line.ToString()}\r\n is not readable";
//        planarFace = null;
//        return false;
//    }

//    /// <summary>
//    /// Writes a collection of planar faces to a CSV file at the specified path.
//    /// </summary>
//    /// <remarks>The method creates or overwrites the file at the specified path. The first line of the file 
//    /// contains a predefined header, followed by one line per planar face in the collection.</remarks>
//    /// <param name="path">The file path where the CSV file will be created. Must not be null or empty.</param>
//    /// <param name="planarFaces">A read-only collection of <see cref="PlanarFace"/> objects to be written to the CSV file.  Each face is
//    /// serialized using its <see cref="PlanarFace.ToCsvString"/> method.</param>
//    public static void WriteCsv(in string path, in IReadOnlyCollection<PlanarFace> planarFaces)
//    {
//        using var csv = File.CreateText(path);
//        csv.WriteLine(CsvHeader);
//        foreach (var pf in planarFaces) 
//            csv.WriteLine(pf.ToCsvString());
//    }

//    /// <summary>
//    /// Reads a CSV file and parses its contents into a set of <see cref="PlanarFace"/> objects.
//    /// </summary>
//    /// <remarks>This method attempts to read and parse a CSV file where each line represents a <see
//    /// cref="PlanarFace"/> object.  The first line of the file is treated as a header and is skipped. If a line cannot
//    /// be parsed,  an error message is added to <paramref name="lineErrors"/>. If no valid data lines are found, 
//    /// <paramref name="error"/> will contain a message indicating this condition.</remarks>
//    /// <param name="path">The file path to the CSV file to be read. Must not be null or empty.</param>
//    /// <param name="lineErrors">An array of error messages for lines in the CSV file that could not be parsed.  Each entry specifies the line
//    /// number and the associated error.</param>
//    /// <param name="error">An error message describing any critical issue encountered during the reading process,  or an empty string if
//    /// the operation completes successfully.</param>
//    /// <returns>A <see cref="HashSet{T}"/> containing the successfully parsed <see cref="PlanarFace"/> objects.  The set will be
//    /// empty if no valid data lines are found or if an error occurs.</returns>
//    public static HashSet<PlanarFace> ReadCsv(in string path, out string[] lineErrors, out string error)
//    {
//        var faces = new HashSet<PlanarFace>();
//        var errors = new List<string>();
//        error = string.Empty;

//        try
//        {
//            using var reader = new StreamReader(path);
//            string? line;
//            int lineNumber = 0;

//            // Header überspringen
//            if ((line = reader.ReadLine()) == null)
//            {
//                lineErrors = [];
//                error = "PlanarFace.ReadCsv: CSV-File has no data lines";
//                return faces;
//            }

//            while ((line = reader.ReadLine()) != null)
//            {
//                lineNumber++;
//                if (TryParseCsvLine(line.AsSpan(), out var pf, out string? parseError))
//                {
//                    faces.Add(pf!);
//                }
//                else
//                {
//                    errors.Add($"Line {lineNumber + 1} has Error: {parseError}");
//                }
//            }
//        }
//        catch (Exception e)
//        {
//            lineErrors = [];
//            error = "PlanarFace.ReadCsv: " + e.Message;
//            return faces;
//        }

//        lineErrors = [.. errors];
//        error = faces.Count == 0
//            ? "PlanarFace.ReadCsv: CSV-File has no data lines"
//            : string.Empty;
//        return faces;
//    }

//    /// <summary>
//    /// Writes the specified planar faces and their associated reference planes to an OBJ file.
//    /// </summary>
//    /// <remarks>The method generates an OBJ file containing the vertices and faces of the provided planar
//    /// faces.  Each face is transformed using its associated reference plane, and the resulting geometry is written  in
//    /// the OBJ format. The file will be created with a ".obj" extension appended to the specified path.</remarks>
//    /// <param name="path">The file path (without extension) where the OBJ file will be created.</param>
//    /// <param name="planes">A dictionary mapping reference plane IDs to their corresponding <see cref="ReferencePlane"/> objects.</param>
//    /// <param name="planarFaces">A list of <see cref="PlanarFace"/> objects to be written to the OBJ file.</param>
//    public static void WriteObj(in string path, Dictionary<string, ReferencePlane> planes, List<PlanarFace> planarFaces)
//    {
//        using var file = File.CreateText(path + ".obj");
//        var vertices = new List<XYZ>();
//        var faces = new List<int[]>();

//        foreach (var pf in planarFaces)
//        {
//            var polygons = pf.FaceFeature2D.PointArrays();
//            for (int i = 0; i < polygons.Length; i++)
//            {
//                file.WriteLine($"# {pf.Id}_{i}");
//                var polygon = polygons[i];
//                foreach (var ls in polygon)
//                {
//                    var plane = planes[pf.ReferencePlaneId].Plane;
//                    int[] indices = new int[ls.Length];
//                    for (int j = 0; j < ls.Length; j++)
//                    {
//                        var xyz = plane.FromPlaneSystem(ls[j].ToUV());
//                        vertices.Add(xyz);
//                        indices[j] = vertices.Count;
//                    }
//                    faces.Add(indices);
//                }
//            }
//        }

//        // Vertices schreiben
//        foreach (var v in vertices)
//            file.WriteLine($"v {v.X} {v.Y} {v.Z}");

//        // Faces schreiben
//        foreach (int[] indices in faces)
//        {
//            file.WriteLine("f " + string.Join(" ", indices));
//        }
//    }

//}