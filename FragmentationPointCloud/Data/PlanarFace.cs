using Autodesk.Revit.DB;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit.Data;

public sealed record PlanarFace : IEquatable<PlanarFace>
{
    private const string CsvHeader =
        "StateId;ObjectGuid;FaceId;PlaneId;BtmLft;BtmRgt;TopRgt;TopLft;BBoxMin;BBoxMax;TIN";

    /// <summary>
    /// Gets the unique identifier for the entity.
    /// </summary>
    public Id Id { get; }

    /// <summary>
    /// Gets the unique identifier of the reference plane associated with this object.
    /// </summary>
    public string ReferencePlaneId { get; }

    /// <summary>
    /// Gets the bottom-left corner of the planar surface in 3D space.
    /// </summary>
    public XYZ BtmLft { get; }

    /// <summary>
    /// Gets the bottom-right corner of the planar surface in 3D space.
    /// </summary>
    public XYZ BtmRgt { get; }

    /// <summary>
    /// Gets the top-right corner of the planar bounding box.
    /// </summary>
    public XYZ TopRgt { get; }

    /// <summary>
    /// Gets the top-left corner point of the planar surface in 3D space.
    /// </summary>
    public XYZ TopLft { get; }

    /// <summary>
    /// Gets the minimum corner of the bounding box.
    /// </summary>
    public XYZ BBoxMin { get; }

    /// <summary>
    /// Gets the maximum corner of the bounding box in 3D space.
    /// </summary>
    public XYZ BBoxMax { get; }

    /// <summary>
    /// Gets the 2D facial feature data associated with the current instance.
    /// </summary>
    public Tin Tin { get; private set; }

    private PlanarFace(Id id, string referencePlaneId, XYZ btmLft, XYZ btmRgt, XYZ topRgt, XYZ topLft, XYZ bBoxMin, XYZ bBoxMax, Tin tin)
    {
        Id = id;
        ReferencePlaneId = referencePlaneId;
        BtmLft = btmLft;
        BtmRgt = btmRgt;
        TopRgt = topRgt;
        TopLft = topLft;
        BBoxMin = bBoxMin;
        BBoxMax = bBoxMax;
        Tin = tin;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlanarFace"/> class, representing a planar face defined by a
    /// reference plane, bounding box, and 2D/3D coordinate limits.
    /// </summary>
    /// <remarks>This constructor initializes the planar face by mapping the 2D coordinate limits to the 3D
    /// space defined by the reference plane. The bounding box and TIN provide additional spatial and surface
    /// information for the planar face.</remarks>
    /// <param name="id">The unique identifier for the planar face.</param>
    /// <param name="referencePlane">The reference plane that defines the orientation and position of the planar face.</param>
    /// <param name="bBoxMin">The minimum 3D bounding box coordinates of the planar face.</param>
    /// <param name="bBoxMax">The maximum 3D bounding box coordinates of the planar face.</param>
    /// <param name="min2D">The minimum 2D coordinates in the plane's local coordinate system.</param>
    /// <param name="max2D">The maximum 2D coordinates in the plane's local coordinate system.</param>
    /// <param name="tin">The triangulated irregular network (TIN) associated with the planar face, used for surface representation.</param>
    private PlanarFace(Id id, ReferencePlane referencePlane, XYZ bBoxMin, XYZ bBoxMax, UV min2D, UV max2D, Tin tin)
    {
        Id = id;
        ReferencePlaneId = referencePlane.Id;
        BtmLft = referencePlane.Plane.FromPlaneSystem(min2D);
        BtmRgt = referencePlane.Plane.FromPlaneSystem(new UV(max2D.U, min2D.V));
        TopRgt = referencePlane.Plane.FromPlaneSystem(max2D);
        TopLft = referencePlane.Plane.FromPlaneSystem(new UV(min2D.U, max2D.V));
        BBoxMin = bBoxMin;
        BBoxMax = bBoxMax;
        Tin = tin;
    }

    public bool Equals(PlanarFace? other) => other is not null && Id.Equals(other.Id);

    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>
    /// Creates a planar face from the specified mesh and reference plane.
    /// </summary>
    /// <remarks>This method processes the provided mesh to create a planar face aligned with the specified
    /// reference plane. If the mesh is invalid (e.g., <see langword="null"/> or contains no triangles) or the operation
    /// fails, the method returns <see langword="false"/> and outputs <see langword="null"/> for <paramref
    /// name="planarFace"/> and <see cref="double.NaN"/> for <paramref name="maxPlaneDist"/>.</remarks>
    /// <param name="id">The unique identifier for the planar face.</param>
    /// <param name="refPlane">The reference plane used to define the planar face.</param>
    /// <param name="mesh">The mesh to be processed. Must not be <see langword="null"/> and must contain at least one triangle.</param>
    /// <param name="planarFace">When this method returns, contains the created <see cref="PlanarFace"/> if the operation succeeds; otherwise,
    /// <see langword="null"/>.</param>
    /// <param name="maxPlaneDist">When this method returns, contains the maximum distance from the mesh to the reference plane if the operation
    /// succeeds; otherwise, <see cref="double.NaN"/>.</param>
    /// <returns><see langword="true"/> if the planar face was successfully created; otherwise, <see langword="false"/>.</returns>
    public static bool Create(in Id id, ReferencePlane refPlane, Mesh mesh, Transform transform,
        out PlanarFace? planarFace, out double maxPlaneDist)
    {
        var plane = refPlane.Plane;
        if(mesh == null || mesh.NumTriangles == 0 
            || !Tin.Create(mesh, plane, transform, out maxPlaneDist, 
            out var min3D, out var max3D, 
            out var min2D, out var max2D, out var tin))
        {
            planarFace = null;
            maxPlaneDist = double.NaN;
            return false;
        }
        planarFace = new PlanarFace(id, refPlane, min3D, max3D, min2D, max2D, tin!);
        return true;
    }

    /// <summary>
    /// Creates a planar face from the specified mesh triangle and reference plane.
    /// </summary>
    /// <remarks>This method attempts to create a planar face by projecting the specified mesh triangle onto
    /// the given reference plane. If <paramref name="meshTriangle"/> is <see langword="null"/>, the method returns <see
    /// langword="false"/>, and the output parameters <paramref name="planarFace"/> and <paramref name="maxPlaneDist"/>
    /// are set to <see langword="null"/> and <see cref="double.NaN"/>, respectively.</remarks>
    /// <param name="id">The unique identifier for the planar face to be created.</param>
    /// <param name="refPlane">The reference plane used to define the planar face.</param>
    /// <param name="meshTriangle">The mesh triangle used to generate the planar face. Cannot be <see langword="null"/>.</param>
    /// <param name="planarFace">When this method returns, contains the created <see cref="PlanarFace"/> if the operation succeeds; otherwise,
    /// <see langword="null"/>.</param>
    /// <param name="maxPlaneDist">When this method returns, contains the maximum distance from the mesh triangle to the reference plane if the
    /// operation succeeds; otherwise, <see cref="double.NaN"/>.</param>
    /// <returns><see langword="true"/> if the planar face was successfully created; otherwise, <see langword="false"/>.</returns>
    public static bool Create(in Id id, MeshTriangle meshTriangle, Transform transform, int digits,
        out PlanarFace? planarFace, out ReferencePlane? refPlane)
    {
       if (meshTriangle == null)
        {
            planarFace = null;
            refPlane = null;
            return false;
        }
        var tin = Tin.Create(meshTriangle, transform, 
            out var plane, out var min3D, out var max3D, out var min2D, out var max2D);
        refPlane = ReferencePlane.Create(plane, digits);
        planarFace = new PlanarFace(id, refPlane, min3D, max3D, min2D, max2D, tin);
        return true;
    }

    /// <summary>
    /// Converts the current object to a CSV-formatted string representation.
    /// </summary>
    /// <remarks>The resulting string contains the object's properties and geometric data serialized into a 
    /// semicolon-separated format. This format is suitable for exporting or logging purposes.</remarks>
    /// <returns>A semicolon-separated string representing the object's state and associated geometric data.</returns>
    private string ToCsvString()
    {
        string[] line =
        [
            Id.StateId,
            Id.ObjectId,
            Id.PartId == 0 ? $"{Id.FaceId}" : $"{Id.FaceId}_{Id.PartId}",
            ReferencePlaneId,
            BtmLft.ToFullWktString(),
            BtmRgt.ToFullWktString(),
            TopRgt.ToFullWktString(),
            TopLft.ToFullWktString(),
            BBoxMin.ToFullWktString(),
            BBoxMax.ToFullWktString(),
            Tin.ToString(),
        ];
        return string.Join(";", line);
    }

    /// <summary>
    /// Attempts to parse a CSV-formatted line into a <see cref="PlanarFace"/> object.
    /// </summary>
    /// <remarks>The input line must be a non-empty, semicolon-delimited string containing the required fields
    /// in the expected order. If any field is missing, empty, or invalid, the method will return <see
    /// langword="false"/> and provide an error message.</remarks>
    /// <param name="line">The input line of text, represented as a <see cref="ReadOnlySpan{T}"/> of characters, containing the CSV data to
    /// parse.</param>
    /// <param name="planarFace">When this method returns, contains the parsed <see cref="PlanarFace"/> object if the parsing was successful;
    /// otherwise, <see langword="null"/>.</param>
    /// <param name="error">When this method returns, contains an error message describing why the parsing failed, if applicable. If parsing
    /// succeeds, this will be an empty string.</param>
    /// <returns><see langword="true"/> if the line was successfully parsed into a <see cref="PlanarFace"/> object; otherwise,
    /// <see langword="false"/>.</returns>
    private static bool TryParseCsvLine(ReadOnlySpan<char> line, out PlanarFace? planarFace, out string error)
    {
        planarFace = null;
        error = string.Empty;

        if (line.IsEmpty)
        {
            error = "PlanarFace.ParseCsvLine: Input string is null or empty";
            return false;
        }

        // Felder per Span extrahieren (StateId;ObjectGuid;FaceId;PlaneId;BtmLft;BtmRgt;TopRgt;TopLft;BBoxMin;BBoxMax;Polygon)
        int idx1 = line.IndexOf(';');
        if (idx1 < 0) goto NotReadable;
        int idx2 = line[(idx1 + 1)..].IndexOf(';');
        if (idx2 < 0) goto NotReadable;
        idx2 += idx1 + 1;
        int idx3 = line[(idx2 + 1)..].IndexOf(';');
        if (idx3 < 0) goto NotReadable;
        idx3 += idx2 + 1;
        int idx4 = line[(idx3 + 1)..].IndexOf(';');
        if (idx4 < 0) goto NotReadable;
        idx4 += idx3 + 1;
        int idx5 = line[(idx4 + 1)..].IndexOf(';');
        if (idx5 < 0) goto NotReadable;
        idx5 += idx4 + 1;
        int idx6 = line[(idx5 + 1)..].IndexOf(';');
        if (idx6 < 0) goto NotReadable;
        idx6 += idx5 + 1;
        int idx7 = line[(idx6 + 1)..].IndexOf(';');
        if (idx7 < 0) goto NotReadable;
        idx7 += idx6 + 1;
        int idx8 = line[(idx7 + 1)..].IndexOf(';');
        if (idx8 < 0) goto NotReadable;
        idx8 += idx7 + 1;
        int idx9 = line[(idx8 + 1)..].IndexOf(';');
        if (idx9 < 0) goto NotReadable;
        idx9 += idx8 + 1;
        int idx10 = line[(idx9 + 1)..].IndexOf(';');
        if (idx10 < 0) goto NotReadable;
        idx10 += idx9 + 1;

        var stateIdSpan = line[..idx1].Trim();
        var objectGuidSpan = line[(idx1 + 1)..idx2].Trim();
        var faceIdSpan = line[(idx2 + 1)..idx3].Trim();
        var planeIdSpan = line[(idx3 + 1)..idx4].Trim();
        var btmLftSpan = line[(idx4 + 1)..idx5].Trim();
        var btmRgtSpan = line[(idx5 + 1)..idx6].Trim();
        var topRgtSpan = line[(idx6 + 1)..idx7].Trim();
        var topLftSpan = line[(idx7 + 1)..idx8].Trim();
        var bboxMinSpan = line[(idx8 + 1)..idx9].Trim();
        var bboxMaxSpan = line[(idx9 + 1)..idx10].Trim();
        var tinSpan = line[(idx10 + 1)..].Trim();

        if (stateIdSpan.IsEmpty || objectGuidSpan.IsEmpty || faceIdSpan.IsEmpty || planeIdSpan.IsEmpty ||
            btmLftSpan.IsEmpty || btmRgtSpan.IsEmpty || topRgtSpan.IsEmpty || topLftSpan.IsEmpty ||
            bboxMinSpan.IsEmpty || bboxMaxSpan.IsEmpty || tinSpan.IsEmpty)
            goto NotReadable;

        // FaceId und PartId extrahieren
        string faceIdStr = faceIdSpan.ToString();
        string faceIdOnly = faceIdStr;
        int partId = 0;
        int underscoreIdx = faceIdStr.IndexOf('_');
        if (underscoreIdx > 0 && underscoreIdx < faceIdStr.Length - 1)
        {
            faceIdOnly = faceIdStr[..underscoreIdx];
            _ = int.TryParse(faceIdStr[(underscoreIdx + 1)..], out partId);
        }

        if (btmLftSpan.TryParseWktXYZ(out var btmLft) &&
            btmRgtSpan.TryParseWktXYZ(out var btmRgt) &&
            topRgtSpan.TryParseWktXYZ(out var topRgt) &&
            topLftSpan.TryParseWktXYZ(out var topLft) &&
            bboxMinSpan.TryParseWktXYZ(out var boxMin) &&
            bboxMaxSpan.TryParseWktXYZ(out var boxMax) &&
            Tin.TryParse(tinSpan, out var tin))
        {
            planarFace = new PlanarFace(
                new Id(stateIdSpan.ToString(), objectGuidSpan.ToString(), faceIdOnly, partId),
                planeIdSpan.ToString(),
                btmLft, btmRgt, topRgt, topLft, boxMin, boxMax, tin!
            );
            return true;
        }

    NotReadable:
        error = $"PlanarFace.ParseCsvLine: Line: \r\n{line.ToString()}\r\n is not readable";
        planarFace = null;
        return false;
    }

    /// <summary>
    /// Writes a collection of planar faces to a CSV file at the specified path.
    /// </summary>
    /// <remarks>The method creates or overwrites the file at the specified path. The first line of the file 
    /// contains a predefined header, followed by one line per planar face in the collection.</remarks>
    /// <param name="path">The file path where the CSV file will be created. Must not be null or empty.</param>
    /// <param name="planarFaces">A read-only collection of <see cref="PlanarFace"/> objects to be written to the CSV file.  Each face is
    /// serialized using its <see cref="PlanarFace.ToCsvString"/> method.</param>
    public static void WriteCsv(in string path, in IReadOnlyCollection<PlanarFace> planarFaces)
    {
        using var csv = File.CreateText(path);
        csv.WriteLine(CsvHeader);
        foreach (var pf in planarFaces)
            csv.WriteLine(pf.ToCsvString());
    }

    /// <summary>
    /// Reads a CSV file and parses its contents into a dictionary of <see cref="PlanarFace"/> objects, keyed by their
    /// <see cref="Id"/>.
    /// </summary>
    /// <remarks>This method attempts to parse each line of the CSV file into a <see cref="PlanarFace"/>
    /// object.  If a line cannot be parsed, an error message is added to <paramref name="lineErrors"/>.  If the file
    /// contains no valid data lines, <paramref name="error"/> will indicate this condition.</remarks>
    /// <param name="path">The file path of the CSV file to read. Must not be null or empty.</param>
    /// <param name="lineErrors">An array of error messages for lines in the CSV file that could not be parsed.  Each entry specifies the line
    /// number and the associated error.</param>
    /// <param name="error">An error message describing a critical issue encountered during the operation,  or an empty string if the
    /// operation completed successfully.</param>
    /// <returns>A dictionary containing the parsed <see cref="PlanarFace"/> objects, keyed by their <see cref="Id"/>.  The
    /// dictionary will be empty if no valid data lines were found in the CSV file.</returns>
    public static Dictionary<Id,PlanarFace> ReadCsv(in string path, out string[] lineErrors, out string error)
    {
        var faces = new Dictionary<Id, PlanarFace>();
        var errors = new List<string>();
        error = string.Empty;

        try
        {
            using var reader = new StreamReader(path);
            string? line;
            int lineNumber = 0;

            // Header überspringen
            if ((line = reader.ReadLine()) == null)
            {
                lineErrors = [];
                error = "PlanarFace.ReadCsv: CSV-File has no data lines";
                return faces;
            }

            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;
                if (TryParseCsvLine(line.AsSpan(), out var pf, out string? parseError))
                {
                    faces.Add(pf!.Id, pf!);
                }
                else
                {
                    errors.Add($"Line {lineNumber + 1} has Error: {parseError}");
                }
            }
        }
        catch (Exception e)
        {
            lineErrors = [];
            error = "PlanarFace.ReadCsv: " + e.Message;
            return faces;
        }

        lineErrors = [.. errors];
        error = faces.Count == 0
            ? "PlanarFace.ReadCsv: CSV-File has no data lines"
            : string.Empty;
        return faces;
    }

    /// <summary>
    /// Writes the specified planar faces and their associated reference planes to an OBJ file.
    /// </summary>
    /// <remarks>The method generates an OBJ file containing the vertices and faces of the provided planar
    /// faces.  Each face is transformed using its associated reference plane, and the resulting geometry is written  in
    /// the OBJ format. The file will be created with a ".obj" extension appended to the specified path.</remarks>
    /// <param name="path">The file path (without extension) where the OBJ file will be created.</param>
    /// <param name="planes">A dictionary mapping reference plane IDs to their corresponding <see cref="ReferencePlane"/> objects.</param>
    /// <param name="planarFaces">A list of <see cref="PlanarFace"/> objects to be written to the OBJ file.</param>
    public static void WriteObj(in string path, Dictionary<string, ReferencePlane> planes, List<PlanarFace> planarFaces)
    {
        using var file = File.CreateText(path + ".obj");
        var vertices = new List<XYZ>();
        var vertexMap = new Dictionary<(string planeId, int vertexIdx), int>();
        var faces = new List<(string comment, int[] indices)>();

        foreach (var pf in planarFaces)
        {
            var tin = pf.Tin;
            var plane = planes[pf.ReferencePlaneId].Plane;
            int baseVertexCount = vertices.Count;

            // Map TIN-Vertices to 3D and collect indices
            for (int i = 0; i < tin.Vertices.Length; i++)
            {
                var uv = tin.Vertices[i];
                var xyz = plane.FromPlaneSystem(uv);
                vertices.Add(xyz);
                vertexMap[(pf.ReferencePlaneId, i)] = baseVertexCount + i + 1; // OBJ: 1-basiert
            }

            // Faces
            for (int t = 0; t < tin.Triangles.Length; t += 3)
            {
                if (!tin.IsInterior[t / 3]) continue;

                int i0 = vertexMap[(pf.ReferencePlaneId, tin.Triangles[t + 0])];
                int i1 = vertexMap[(pf.ReferencePlaneId, tin.Triangles[t + 1])];
                int i2 = vertexMap[(pf.ReferencePlaneId, tin.Triangles[t + 2])];
                faces.Add(($"# {pf.Id}", new[] { i0, i1, i2 }));
            }
        }

        // Write vertices
        foreach (var v in vertices)
            file.WriteLine($"v {v.X} {v.Y} {v.Z}");

        // Write faces (with comments)
        foreach (var (comment, indices) in faces)
        {
            file.WriteLine(comment);
            file.WriteLine("f " + string.Join(" ", indices));
        }
    }



}
