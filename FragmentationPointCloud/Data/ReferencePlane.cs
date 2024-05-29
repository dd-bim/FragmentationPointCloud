using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

using GeometryLib.Double.D3;
using D6 = GeometryLib.Double.D6;
using static GeometryLib.Double.Constants;

namespace Revit.Data
{
    public readonly struct ReferencePlane : IEquatable<ReferencePlane>
    {
        public string Id { get; }

        public IPlane Plane { get; }

        public ReferencePlane(in string id, in IPlane plane)
        {
            Id = id;
            Plane = plane;
        }
        public ReferencePlane(in CoordinateSystem system, in IPlane plane, in Id id, int digits = 3)
        {
            Plane = plane;
            var lokNormal = system.ToSystem(plane.Normal);
            var x = (n: Math.Abs(lokNormal.x), c: 'X');
            var y = (n: Math.Abs(lokNormal.y), c: 'Y');
            var z = (n: Math.Abs(lokNormal.z), c: 'Z');
            var lokD = -lokNormal.Dot(system.ToSystem(plane.Position));
            var max = x.n > y.n ? x : y;
            max = max.n > z.n ? max : z;
            int hc = plane.Position.GetHashCode() ^ (3 * plane.Normal.GetHashCode()) ^ (5 * plane.PlaneX.GetHashCode());
            Id = id.ToString();
        }
        public ReferencePlane(in IPlane plane, int digits = 3)
        {
            Plane = plane;
            var x = (n: Math.Abs(plane.Normal.x), c: 'X');
            var y = (n: Math.Abs(plane.Normal.y), c: 'Y');
            var z = (n: Math.Abs(plane.Normal.z), c: 'Z');
            var max = x.n > y.n ? x : y;
            max = max.n > z.n ? max : z;
            int hc = plane.Position.GetHashCode() ^ (3 * plane.Normal.GetHashCode()) ^ (5 * plane.PlaneX.GetHashCode());
            Id = $"0 {max.c} {(plane.D < 0 ? '+' : '-')} {Math.Round(-plane.D, digits)} {hc}";
        }

        public ReferencePlane(in CoordinateSystem system, in IPlane plane, int digits = 3)
        {
            Plane = plane;
            var lokNormal = system.ToSystem(plane.Normal);
            var x = (n: Math.Abs(lokNormal.x), c: 'X');
            var y = (n: Math.Abs(lokNormal.y), c: 'Y');
            var z = (n: Math.Abs(lokNormal.z), c: 'Z');
            var lokD = -lokNormal.Dot(system.ToSystem(plane.Position));
            var max = x.n > y.n ? x : y;
            max = max.n > z.n ? max : z;
            int hc = plane.Position.GetHashCode() ^ (3 * plane.Normal.GetHashCode()) ^ (5 * plane.PlaneX.GetHashCode());
            Id = $"0 {max.c} {(lokD < 0 ? '+' : '-')} {Math.Round(-lokD, digits)} {hc}";
        }

        public bool ApproxEquals(ReferencePlane other,
            in double maxDifferenceD = 0.0005,
            in double maxDifferenceCosOne = TRIGTOL) =>
            this.Equals(other)
            || Plane.ApproxEquals(other.Plane, maxDifferenceD, maxDifferenceCosOne);

        public bool Equals(ReferencePlane other) => Id == other.Id;

        public override bool Equals(object? obj) => obj is ReferencePlane rp && Equals(rp);

        public override int GetHashCode() => Id.GetHashCode();

        public static bool operator ==(ReferencePlane left, ReferencePlane right) => Equals(left, right);

        public static bool operator !=(ReferencePlane left, ReferencePlane right) => !Equals(left, right);


        public const string CsvHeader = "Id;Position;Normal;PlaneX;";

        public const int LineCount = 4;

        public const int LineCountCxx = 5;

        public string ToCsvString()
        {
            string[] line;
            switch (Plane)
            {
                case Plane p:
                    line = new string[LineCount];
                    line[0] = Id;
                    line[1] = p.Position.ToWktString();
                    line[2] = p.Normal.ToString();
                    line[3] = p.PlaneX.ToString();
                    break;
                case StochasticPlane sp:
                    line = new string[LineCountCxx];
                    line[0] = Id;
                    line[1] = sp.Position.ToWktString();
                    line[2] = sp.Normal.ToString();
                    line[3] = sp.PlaneX.ToString();
                    line[4] = sp.Cxx.ToArrayString(); break;
                default:
                    line = Array.Empty<string>();
                    break;
            }
            return string.Join(";", line);
        }

        public static bool TryParseCsvLine(string line, out ReferencePlane referencePlane, out string error)
        {
            if (string.IsNullOrEmpty(line))
            {
                error = "ReferencePlane.ParseCsvLine: Input string is null or empty";
                referencePlane = default;
                return false;
            }
            var strings = line.Split(new[] { ';' });
            if (strings.Length >= LineCount
                && Vector.TryParseWkt(strings[1], out var position)
                && Vector.TryParse(strings[2], out var nrm)
                && Vector.TryParse(strings[3], out var px))
            {
                var normal = (Direction)nrm;
                var planeX = (Direction)px;
                IPlane plane = strings.Length >= LineCountCxx && D6.SpdMatrix.TryParseArray(strings[4], out var cxx)
                    ? new StochasticPlane(position, normal, planeX, cxx)
                    : new Plane(position, normal, planeX);

                referencePlane = new ReferencePlane(strings[0], plane);
                error = string.Empty;
                return true;
            }
            error = $"ReferencePlane.ParseCsvLine: Line: \r\n{line}\r\n is not readable";
            referencePlane = default;
            return false;
        }

        public static IReadOnlyDictionary<string, ReferencePlane> ReadCsv(in string path, out string[] lineErrors, out string error)
        {
            string[] lines;
            var planes = new Dictionary<string, ReferencePlane>();
            try
            {
                lines = File.ReadAllLines(path);
            }
            catch (Exception e)
            {
                lineErrors = Array.Empty<string>();
                error = "ReferencePlane.ReadCsv: " + e.Message;
                return planes;
            }
            if (lines.Length > 1)
            {
                var errors = new List<string>();
                for (int i = 1; i < lines.Length; i++)
                {
                    if (TryParseCsvLine(lines[i], out var rp, out error))
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
            lineErrors = Array.Empty<string>();
            error = "ReferencePlane.ReadCsv: CSV-File has no data lines";
            return planes;
        }

        public static void WriteCsv(in string path, in IEnumerable<ReferencePlane> referencePlanes)
        {
            using var csv = File.CreateText(path);
            csv.WriteLine(CsvHeader);
            foreach (var rp in referencePlanes.ToImmutableHashSet())
            {
                csv.WriteLine(rp.ToCsvString());
            }
        }

        public static Dictionary<ReferencePlane, List<PlanarFace>> CombineAndMap(
            in IEnumerable<ReferencePlane> referencePlanes,
            in IEnumerable<PlanarFace> planarFaces,
            in double maxDifferenceD = 0.0005,
            in double maxDifferenceCosOne = TRIGTOL)
        {
            var map = new Dictionary<string, ReferencePlane>();
            foreach (var rp in referencePlanes)
            {
                map[rp.Id] = rp;
            }
            return CombineAndMap(map, planarFaces, maxDifferenceD, maxDifferenceCosOne);
        }

        public static Dictionary<ReferencePlane, List<PlanarFace>> CombineAndMap(
            in IReadOnlyDictionary<string, ReferencePlane> referencePlanes,
            in IEnumerable<PlanarFace> planarFaces,
            in double maxDifferenceD = 0.0005,
            in double maxDifferenceCosOne = TRIGTOL)
        {
            var idMap = new Dictionary<string, string>(referencePlanes.Count);
            var rpArr = ImmutableArray.CreateRange(referencePlanes.Values);
            for (int i = 0; i < rpArr.Length; i++)
            {
                var pi = rpArr[i];
                if (pi.Plane is Plane pli)
                    for (int j = i + 1; j < rpArr.Length; j++)
                    {
                        var pj = rpArr[j];
                        if (pj.Plane is Plane plj 
                            && pli.ApproxEquals(plj, maxDifferenceD, maxDifferenceCosOne))
                        {
                            idMap[pj.Id] = pi.Id;
                        }
                    }
            }
            var planeMap = new Dictionary<ReferencePlane, List<PlanarFace>>(referencePlanes.Count);
            foreach (var pf in planarFaces)
            {
                if (idMap.TryGetValue(pf.ReferencePlaneId, out var newId))
                {
                    if (!pf.ChangeReferencePlane(referencePlanes[pf.ReferencePlaneId], referencePlanes[newId]))
                        throw new Exception("Nearly impossible exception");
                }
                else
                    newId = pf.ReferencePlaneId;

                if (!planeMap.TryGetValue(referencePlanes[newId], out var pfList))
                {
                    pfList = new List<PlanarFace>();
                    planeMap.Add(referencePlanes[newId], pfList);
                }
                pfList.Add(pf);
            }
            return planeMap;
        }
    }
}