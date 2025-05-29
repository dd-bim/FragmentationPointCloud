using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using Autodesk.Revit.DB;

namespace Revit.Data;

/// <summary>
/// Represents a reference plane defined by a unique identifier and a geometric plane.
/// </summary>
/// <remarks>A <see cref="ReferencePlane"/> is an immutable record that associates a unique string identifier with
/// a geometric plane. It provides methods for creating, parsing, and serializing reference planes, as well as utilities
/// for working with CSV-formatted data.</remarks>
/// <param name="Id"></param>
/// <param name="Plane"></param>
public sealed record ReferencePlane(string Id, Plane Plane) : IEquatable<ReferencePlane>
{
 
    /// <summary>
    /// Creates a new <see cref="ReferencePlane"/> instance based on the specified transformation and plane.
    /// </summary>
    /// <remarks>The method generates a unique identifier for the reference plane based on the plane's
    /// orientation, transformed normal, and origin. The identifier includes the dominant axis of the plane's normal,
    /// the sign of the transformed distance, and a hash code derived from the plane's properties.</remarks>
    /// <param name="transform">The transformation to apply to the plane's normal and origin.</param>
    /// <param name="plane">The plane to be used for creating the reference plane.</param>
    /// <param name="digits">The number of decimal places to round the calculated distance. Defaults to 3.</param>
    /// <returns>A new <see cref="ReferencePlane"/> instance representing the transformed plane.</returns>
    public static ReferencePlane Create(in Transform transform, in Plane plane, int digits = 3)
    {
        var lokNormal = transform.OfVector(plane.Normal);
        var x = (n: double.Abs(lokNormal.X), c: 'X');
        var y = (n: double.Abs(lokNormal.Y), c: 'Y');
        var z = (n: double.Abs(lokNormal.Z), c: 'Z');
        double lokD = lokNormal.DotProduct(transform.OfPoint(plane.Origin));
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

    public bool Equals(ReferencePlane other) => Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();


    private const string CsvHeader = "Id;Position;Normal;PlaneX;";


    private const int LineCount = 4;

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
            $"POINT Z({Plane.Origin.ToFullString()})",
            Plane.Normal.ToFullString(),
            Plane.XVec.ToFullString(),
        ];
        return string.Join(";", line);
    }

    /// <summary>
    /// Attempts to parse a CSV-formatted line into a <see cref="ReferencePlane"/> object.
    /// </summary>
    /// <remarks>The input line must be a non-empty string formatted as "Id;Position;Normal;PlaneX;", where:
    /// <list type="bullet"> <item><description><c>Id</c> is a unique identifier.</description></item>
    /// <item><description><c>Position</c> is a WKT (Well-Known Text) representation of a 3D point.</description></item>
    /// <item><description><c>Normal</c> is a 3D vector representing the normal of the plane.</description></item>
    /// <item><description><c>PlaneX</c> is a 3D vector representing the X-axis direction of the
    /// plane.</description></item> </list> If the input line is invalid or cannot be parsed, the method returns <see
    /// langword="false"/> and provides an error message in the <paramref name="error"/> parameter.</remarks>
    /// <param name="line">The input line to parse, represented as a <see cref="ReadOnlySpan{T}"/> of characters. The line must follow the
    /// format: "Id;Position;Normal;PlaneX;".</param>
    /// <param name="referencePlane">When this method returns, contains the parsed <see cref="ReferencePlane"/> if the parsing was successful;
    /// otherwise, the default value.</param>
    /// <param name="error">When this method returns, contains an error message if the parsing failed; otherwise, an empty string.</param>
    /// <returns><see langword="true"/> if the line was successfully parsed into a <see cref="ReferencePlane"/>; otherwise, <see
    /// langword="false"/>.</returns>
    private static bool TryParseCsvLine(ReadOnlySpan<char> line, out ReferencePlane referencePlane, out string error)
    {
        referencePlane = default;
        error = string.Empty;

        // Leere oder zu kurze Zeile abfangen
        if (line.IsEmpty)
        {
            error = "ReferencePlane.ParseCsvLine: Input string is null or empty";
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
            var normal = nrm.Normalize();
            var planeX = px.Normalize();
            var planeY = normal.CrossProduct(planeX).Normalize();
            var plane = Plane.CreateByOriginAndBasis(position, planeX, planeY);

            referencePlane = new ReferencePlane(idSpan.ToString(), plane);
            return true;
        }

    NotReadable:
        error = $"ReferencePlane.ParseCsvLine: Line: \r\n{line.ToString()}\r\n is not readable";
        referencePlane = default;
        return false;
    }

    /// <summary>
    /// Reads a CSV file and parses its contents into a dictionary of reference planes.
    /// </summary>
    /// <remarks>This method processes the CSV file line by line, skipping the header row.  If a line cannot
    /// be parsed, an error message is added to <paramref name="lineErrors"/>.  If the file contains no valid data
    /// lines, <paramref name="error"/> will indicate this.</remarks>
    /// <param name="path">The file path to the CSV file to be read. Must not be null or empty.</param>
    /// <param name="lineErrors">An array of error messages for lines in the CSV file that could not be parsed.  Each entry specifies the line
    /// number and the associated error.</param>
    /// <param name="error">An error message describing a critical issue encountered during the operation,  or an empty string if the
    /// operation completed successfully.</param>
    /// <returns>A dictionary where the keys are the unique identifiers of the reference planes  and the values are the
    /// corresponding <see cref="ReferencePlane"/> objects.  The dictionary will be empty if no valid reference planes
    /// were parsed.</returns>
    public static Dictionary<string, ReferencePlane> ReadCsv(in string path, out string[] lineErrors, out string error)
    {
        var planes = new Dictionary<string, ReferencePlane>();
        var errors = new List<string>();
        error = string.Empty;

        try
        {
            using var reader = new StreamReader(path);
            string? line;
            int lineNumber = 0;

            // Erste Zeile (Header) überspringen
            if ((line = reader.ReadLine()) == null)
            {
                lineErrors = [];
                error = "ReferencePlane.ReadCsv: CSV-File has no data lines";
                return planes;
            }

            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;
                if (TryParseCsvLine(line.AsSpan(), out var rp, out string parseError))
                {
                    planes[rp.Id] = rp;
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
            error = "ReferencePlane.ReadCsv: " + e.Message;
            return planes;
        }

        lineErrors = [.. errors];
        error = planes.Count == 0 
            ? "ReferencePlane.ReadCsv: CSV-File has no data lines" 
            : string.Empty;
        return planes;
    }

    /// <summary>
    /// Writes a collection of reference planes to a CSV file at the specified path.
    /// </summary>
    /// <remarks>Each reference plane in the collection is written as a single line in the CSV file,  using
    /// the format defined by the <see cref="ReferencePlane.ToCsvString"/> method.  The first line of the file contains
    /// the CSV header.</remarks>
    /// <param name="path">The file path where the CSV file will be created. Must not be null or empty.</param>
    /// <param name="referencePlanes">A collection of reference planes to write to the CSV file. Must not be null.</param>
    public static void WriteCsv(in string path, HashSet<ReferencePlane> referencePlanes)
    {
        using var csv = File.CreateText(path);
        csv.WriteLine(CsvHeader);
        foreach (var rp in referencePlanes) 
            csv.WriteLine(rp.ToCsvString());
    }
}