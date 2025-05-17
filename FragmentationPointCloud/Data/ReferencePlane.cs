using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Autodesk.Revit.DB;

namespace Revit.Data;

public readonly struct ReferencePlane : IEquatable<ReferencePlane>
{
    public string Id { get; }

    public Plane Plane { get; }

    private ReferencePlane(in string id, in Plane plane)
    {
        Id = id;
        Plane = plane;
    }
 
    public ReferencePlane(in Transform transform, in Plane plane, int digits = 3)
    {
        Plane = plane;
        var lokNormal = transform.OfVector(plane.Normal);
        var x = (n: double.Abs(lokNormal.X), c: 'X');
        var y = (n: double.Abs(lokNormal.Y), c: 'Y');
        var z = (n: double.Abs(lokNormal.Z), c: 'Z');
        double lokD = -lokNormal.DotProduct(transform.OfPoint(plane.Origin));
        var max = x.n > y.n ? x : y;
        max = max.n > z.n ? max : z;
        int hc = plane.Origin.GetHashCode() ^ (3 * plane.Normal.GetHashCode()) ^ (5 * plane.XVec.GetHashCode());
        Id = $"0 {max.c} {(lokD < 0 ? '+' : '-')} {double.Round(-lokD, digits)} {hc}";
    }

    public bool Equals(ReferencePlane other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is ReferencePlane rp && Equals(rp);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(ReferencePlane left, ReferencePlane right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ReferencePlane left, ReferencePlane right)
    {
        return !Equals(left, right);
    }


    private const string CsvHeader = "Id;Position;Normal;PlaneX;";

    private const int LineCount = 4;

    private string ToCsvString()
    {
        string[] line = new string[LineCount];
        line[0] = Id;
        line[1] = $"POINT Z({Plane.Origin.ToFullString()})";
        line[2] = Plane.Normal.ToFullString();
        line[3] = Plane.XVec.ToFullString();
        return string.Join(";", line);
    }

    private static bool TryParseCsvLine(string line, out ReferencePlane referencePlane, out string error)
    {
        if (string.IsNullOrEmpty(line))
        {
            error = "ReferencePlane.ParseCsvLine: Input string is null or empty";
            referencePlane = default;
            return false;
        }

        string[] strings = line.Split([';']);
        if (strings.Length >= LineCount
            && strings[1].TryParseWktXYZ(out var position)
            && strings[2].TryParseXYZ(out var nrm)
            && strings[3].TryParseXYZ(out var px))
        {
            var normal = nrm.Normalize();
            var planeX = px.Normalize();
            var planeY = normal.CrossProduct(planeX).Normalize();
            var plane = Plane.CreateByOriginAndBasis(position, planeX, planeY);

            referencePlane = new ReferencePlane(strings[0], plane);
            error = string.Empty;
            return true;
        }

        error = $"ReferencePlane.ParseCsvLine: Line: \r\n{line}\r\n is not readable";
        referencePlane = default;
        return false;
    }

    public static IReadOnlyDictionary<string, ReferencePlane> ReadCsv(in string path, out string[] lineErrors,
        out string error)
    {
        string[] lines;
        var planes = new Dictionary<string, ReferencePlane>();
        try
        {
            lines = File.ReadAllLines(path);
        }
        catch (Exception e)
        {
            lineErrors = [];
            error = "ReferencePlane.ReadCsv: " + e.Message;
            return planes;
        }

        if (lines.Length > 1)
        {
            var errors = new List<string>();
            for (var i = 1; i < lines.Length; i++)
            {
                if (TryParseCsvLine(lines[i], out ReferencePlane rp, out error))
                {
                    planes[rp.Id] = rp;
                    continue;
                }

                errors.Add($"Line {i + 1} has Error: {error}");
                error = string.Empty;
            }

            lineErrors = errors.ToArray();
            error = string.Empty;
            return planes;
        }

        lineErrors = [];
        error = "ReferencePlane.ReadCsv: CSV-File has no data lines";
        return planes;
    }

    public static void WriteCsv(in string path, in IEnumerable<ReferencePlane> referencePlanes)
    {
        using StreamWriter csv = File.CreateText(path);
        csv.WriteLine(CsvHeader);
        foreach (ReferencePlane rp in referencePlanes.ToImmutableHashSet()) csv.WriteLine(rp.ToCsvString());
    }
}