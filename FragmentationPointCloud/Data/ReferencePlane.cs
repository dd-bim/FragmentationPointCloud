using Autodesk.Revit.DB;

using Serilog;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;

namespace Revit.Data;

/// <summary>
/// Represents a reference plane defined by a unique identifier and a geometric plane.
/// </summary>
/// <remarks>A <see cref="ReferencePlane"/> is an immutable record that associates a unique string identifier with
/// a geometric plane. It provides methods for creating, parsing, and serializing reference planes, as well as utilities
/// for working with CSV-formatted data.</remarks>
/// <param name="Id"></param>
/// <param name="Plane"></param>
public sealed record ReferencePlane(string Id, DataPlane Plane) : IEquatable<ReferencePlane>
{

    /// <summary>
    /// Creates a new <see cref="ReferencePlane"/> instance based on the specified plane and precision.
    /// </summary>
    /// <remarks>The generated identifier for the <see cref="ReferencePlane"/> includes the dominant axis of
    /// the plane's normal vector,  the sign of the plane's distance from the origin, the rounded distance value, and a
    /// hash code derived from the plane's properties.  This ensures that the reference plane is uniquely
    /// identifiable.</remarks>
    /// <param name="plane">The <see cref="Plane"/> object representing the geometric plane to be used for creating the reference plane.</param>
    /// <param name="digits">The number of decimal places to include in the distance component of the reference plane's identifier.  Defaults
    /// to 3 if not specified.</param>
    /// <returns>A <see cref="ReferencePlane"/> instance uniquely identified by a string that encodes the plane's orientation, 
    /// distance from the origin, and a hash of its properties.</returns>
    public static ReferencePlane Create(in DataPlane plane, int digits = 3)
    {
        var x = (n: double.Abs(plane.Normal.X), c: 'X');
        var y = (n: double.Abs(plane.Normal.Y), c: 'Y');
        var z = (n: double.Abs(plane.Normal.Z), c: 'Z');
        double lokD = plane.Normal.DotProduct(plane.Origin);
        var max = x.n > y.n ? x : y;
        max = max.n > z.n ? max : z;
        int hc = HashCode.Combine(plane.Origin, plane.Normal, plane.XVec);

        // Feste Längen: Achse (1), Vorzeichen (1), Distanz (5 + digits, inkl. Punkt), Hash (10)
        string axis = max.c.ToString(); // 1 Zeichen
        string sign = lokD > 0 ? "+" : "-"; // 1 Zeichen
        string dist = double.Round(double.Abs(lokD), digits).ToString($"F{digits}", CultureInfo.InvariantCulture).PadLeft(5 + digits, '0'); // z.B. "0001.234"
        string hash = Math.Abs(hc).ToString().PadLeft(10, '0'); // z.B. "0001234567"

        string id = $"0 {axis} {sign} {dist} {hash}";
        return new ReferencePlane(id, plane);
    }

    public bool Equals(ReferencePlane? other) => other is not null && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();

    public static bool TryRayPlaneIntersection(XYZ origin, XYZ direction, Plane plane, out XYZ intersection)
    {
        intersection = null!;
        var n = plane.Normal;
        double denom = direction.DotProduct(n);
        if (Math.Abs(denom) < 1e-10)
            return false; // Parallel, kein Schnitt

        double t = (plane.Origin - origin).DotProduct(n) / denom;
        if (t < 0)
            return false; // Schnittpunkt liegt "hinter" dem Ursprung des Strahls

        intersection = origin + (t * direction);
        return true;
    }


    private const string CsvHeader = "Id;Position;Normal;PlaneX";

    /// <summary>
    /// Converts the current object to a CSV-formatted string representation.
    /// </summary>
    /// <remarks>The resulting string includes the object's ID, a formatted representation of the plane's
    /// origin,  normal vector, and X-axis vector, separated by semicolons.</remarks>
    /// <returns>A semicolon-separated string containing the object's ID and plane details in CSV format.</returns>
    private string ToCsvString()
    {
        string[] line =
        [
            Id,
            Plane.Origin.ToFullWktString(),
            Plane.Normal.ToFullString(),
            Plane.XVec.ToFullString(),
        ];
        return string.Join(";", line);
    }

    private static bool TryParseCsvLine(ReadOnlySpan<char> line, out ReferencePlane? referencePlane)
    {
        referencePlane = default;

        // Leere oder zu kurze Zeile abfangen
        if (line.IsEmpty)
        {
            Log.Warning("ReferencePlane.ParseCsvLine: Input string is null or empty");
            return false;
        }

        // Felder per Span extrahieren (Id;Position;Normal;PlaneX;)
        int idx1 = line.IndexOf(';');
        if (idx1 < 0) goto NotReadable;
        int idx2 = line[(idx1 + 1)..].IndexOf(';');
        if (idx2 < 0) goto NotReadable;
        idx2 += idx1 + 1;
        int idx3 = line[(idx2 + 1)..].IndexOf(';');
        if (idx3 < 0) goto NotReadable;
        idx3 += idx2 + 1;

        var idSpan = line[..idx1].Trim();
        var posSpan = line[(idx1 + 1)..idx2].Trim();
        var nrmSpan = line[(idx2 + 1)..idx3].Trim();
        var pxSpan = line[(idx3 + 1)..].Trim();

        if (idSpan.IsEmpty || posSpan.IsEmpty || nrmSpan.IsEmpty || pxSpan.IsEmpty)
            goto NotReadable;

        if (posSpan.TryParseWktXYZ(out var position) &&
            nrmSpan.TryParseXYZ(out var nrm) &&
            pxSpan.TryParseXYZ(out var px))
        {
            var plane = Extensions.GetPlane(position, nrm, px);

            referencePlane = new ReferencePlane(idSpan.ToString(), plane);
            return true;
        }

    NotReadable:
        Log.Warning("ReferencePlane.ParseCsvLine: Input string is not readable: {Line}", line.ToString());
        referencePlane = default;
        return false;
    }

    public static bool TryReadCsv(in string path, out Dictionary<string, ReferencePlane> planes)
    {
        planes = [];
        try
        {
            using var reader = new StreamReader(path);
            string? line;
            int lineNumber = 0;

            // Erste Zeile (Header) überspringen
            if ((line = reader.ReadLine()) == null)
            {
                Log.Error("ReferencePlane.ReadCsv: CSV-File is empty or has no header line");
                return false;
            }

            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;
                if (TryParseCsvLine(line.AsSpan(), out var rp))
                {
                    planes[rp!.Id] = rp;
                }
                else
                {
                    Log.Warning("ReferencePlane.ReadCsv: Line {LineNumber} is not readable: {Line}", lineNumber, line);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "ReferencePlane.ReadCsv: Error reading CSV file at {Path}", path);
            return false;
        }

        if (planes.Count == 0)
        {
            Log.Warning("ReferencePlane.TryReadCsv: CSV-File has no data lines");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Writes a collection of reference planes to a CSV file at the specified path.
    /// </summary>
    /// <remarks>Each reference plane in the collection is written as a single line in the CSV file,  using
    /// the format defined by the <see cref="ReferencePlane.ToCsvString"/> method.  The first line of the file contains
    /// the CSV header.</remarks>
    /// <param name="path">The file path where the CSV file will be created. Must not be null or empty.</param>
    /// <param name="referencePlanes">A collection of reference planes to write to the CSV file. Must not be null.</param>
    public static void WriteCsv(in string path, IReadOnlyCollection<ReferencePlane> referencePlanes)
    {
        using var csv = File.CreateText(path);
        csv.WriteLine(CsvHeader);
        foreach (var rp in referencePlanes)
            csv.WriteLine(rp.ToCsvString());
    }
}