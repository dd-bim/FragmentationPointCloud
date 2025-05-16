using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using GeometryLib.D3;

namespace ScantraIO.Data;

public readonly struct ReferencePlane : IEquatable<ReferencePlane>
{
    public string Id { get; }

    public Plane Plane { get; }

    private ReferencePlane(in string id, in Plane plane)
    {
        Id = id;
        Plane = plane;
    }
 
    public ReferencePlane(in CoordinateSystem system, in Plane plane, int digits = 3)
    {
        Plane = plane;
        Direction lokNormal = plane.Normal * system.Rotation;
        (double n, char c) x = (n: Math.Abs(lokNormal.x), c: 'X');
        (double n, char c) y = (n: Math.Abs(lokNormal.y), c: 'Y');
        (double n, char c) z = (n: Math.Abs(lokNormal.z), c: 'Z');
        double lokD = -lokNormal.Dot((plane.Position - system.Position) * system.Rotation);
        (double n, char c) max = x.n > y.n ? x : y;
        max = max.n > z.n ? max : z;
        int hc = plane.Position.GetHashCode() ^ (3 * plane.Normal.GetHashCode()) ^ (5 * plane.PlaneX.GetHashCode());
        Id = $"0 {max.c} {(lokD < 0 ? '+' : '-')} {Math.Round(-lokD, digits)} {hc}";
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
        var line = new string[LineCount];
        line[0] = Id;
        line[1] = Plane.Position.ToWktString();
        line[2] = Plane.Normal.ToString();
        line[3] = Plane.PlaneX.ToString();
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
            && Vector.TryParseWkt(strings[1], out Vector position)
            && Vector.TryParse(strings[2], out Vector nrm)
            && Vector.TryParse(strings[3], out Vector px))
        {
            var normal = (Direction)nrm;
            var planeX = (Direction)px;
            //Plane plane = strings.Length >= LineCountCxx && D6.SpdMatrix.TryParseArray(strings[4], out var cxx)
            //    ? new StochasticPlane(position, normal, planeX, cxx)
            //    : new Plane (position, normal, planeX);
            var plane = new Plane(position, normal, planeX);

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