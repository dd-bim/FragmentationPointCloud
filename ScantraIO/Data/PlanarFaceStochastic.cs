using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Schema;

using GeometryLib.Double.D3;
using D6 = GeometryLib.Double.D6;

namespace ScantraIO.Data
{
    public readonly struct PlanarFaceStochastic : IEquatable<PlanarFaceStochastic>, IEquatable<PlanarFace>
    {
        public readonly Id Id;

        public readonly long NumberOfPoints;

        public readonly D6.SpdMatrix Cxx;

        public PlanarFaceStochastic(Id id, long numberOfPoints, D6.SpdMatrix cxx)
        {
            Id = id;
            NumberOfPoints = numberOfPoints;
            Cxx = cxx;
        }

        public static readonly string CsvHeader = "StateId; ObjectId; FaceId; NumberOfPoints; cXX";

        public static readonly int LineCount = 5;

        public static readonly int CxxStartIndex = 4;

        public string ToCsvString()
        {
            return $"{Id};{NumberOfPoints};{Cxx.ToArrayString()}";
        }

        public static bool TryParseCsvLine(string line, out PlanarFaceStochastic stochastic)
        {
            var strings = line.Split(new[] { ';' });
            if (strings.Length == LineCount
                && int.TryParse(strings[3], out int numberOfPoints)
                && D6.SpdMatrix.TryParseArray(strings[4], out var cxx))
            {
                stochastic = new PlanarFaceStochastic(new Id(strings[0], strings[1], strings[2]), numberOfPoints, cxx);
                return true;
            }
            stochastic = default;
            return false;
        }


        public override bool Equals(object? obj)
        {
            return obj is PlanarFace face && Equals(face);
        }

        public bool Equals(PlanarFaceStochastic other) => Id.Equals(other.Id);

        public bool Equals(PlanarFace? other) => other?.Id.Equals(Id) ?? false;

        public override int GetHashCode() => Id.GetHashCode();

        public static bool operator ==(PlanarFaceStochastic left, PlanarFaceStochastic right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PlanarFaceStochastic left, PlanarFaceStochastic right)
        {
            return !(left == right);
        }

        public static bool operator ==(PlanarFaceStochastic left, PlanarFace right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PlanarFaceStochastic left, PlanarFace right)
        {
            return !(left == right);
        }


        public static void WriteCsv(in string path, in IReadOnlyList<PlanarFaceStochastic> planarFaces)
        {
            using (var csv = File.CreateText(path))
            {
                csv.WriteLine(CsvHeader);
                foreach (var pf in planarFaces)
                {
                    csv.WriteLine(pf.ToCsvString());
                }
            }
        }

        public static PlanarFaceStochastic[] ReadCsv(in string path, out string[] lineErrors, out string error)
        {
            string[] lines;
            try
            {
                lines = File.ReadAllLines(path);
            }
            catch (Exception e)
            {
                lineErrors = Array.Empty<string>();
                error = "PlanarFaceStochastic.ReadCsv: " + e.Message;
                return Array.Empty<PlanarFaceStochastic>();
            }
            if (lines != null && lines.Length > 1)
            {
                var faces = new List<PlanarFaceStochastic>(lines.Length - 1);
                var errors = new List<string>();
                for (int i = 1; i < lines.Length; i++)
                {
                    if (TryParseCsvLine(lines[i], out var stochastic))
                    {
                        faces.Add(stochastic);
                    }
                    else
                    {
                        errors.Add($"Line {i} has Error");
                    }
                }
                lineErrors = errors.ToArray();
                error = string.Empty;
                return faces.ToArray();
            }
            lineErrors = Array.Empty<string>();
            error = "PlanarFaceStochastic.ReadCsv: CSV-File has no data lines";
            return Array.Empty<PlanarFaceStochastic>();
        }


    }
}
