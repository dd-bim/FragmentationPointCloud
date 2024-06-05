using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

using GeometryLib.Double;
using GeometryLib.Double.D3;

using D2 = GeometryLib.Double.D2;
using D3 = GeometryLib.Double.D3;

namespace ScantraIO.Data
{
    public class PlanarFace : IEquatable<PlanarFace>
    {

        public Id Id { get; }

        public string ReferencePlaneId { get; private set; }

        public Vector PlanarBtmLft { get; }

        public Vector PlanarBtmRgt { get; }

        public Vector PlanarTopRgt { get; }

        public Vector PlanarTopLft { get; }

        /// <summary>
        /// Bounding Box, Min X Y Z + Max X Y Z
        /// </summary>
        public BBox BBox { get; }

        public D2.Polygon Polygon { get; private set; }

        public PlanarFace(Id id, string referencePlaneId, Vector planarBtmLft, Vector planarBtmRgt, Vector planarTopRgt, Vector planarTopLft, BBox bBox, D2.Polygon polygon)
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

        public PlanarFace(in Id id, in ReferencePlane referencePlane, in BBox bBox, in D2.Polygon polygon)
        {
            Id = id;
            ReferencePlaneId = referencePlane.Id;
            BBox = bBox;
            PlanarBtmLft = referencePlane.Plane.FromPlaneSystem(polygon.BBox.Min);
            PlanarBtmRgt = referencePlane.Plane.FromPlaneSystem(new D2.Vector(polygon.BBox.Min.x, polygon.BBox.Max.y));
            PlanarTopRgt = referencePlane.Plane.FromPlaneSystem(polygon.BBox.Max);
            PlanarTopLft = referencePlane.Plane.FromPlaneSystem(new D2.Vector(polygon.BBox.Max.x, polygon.BBox.Min.y));
            Polygon = polygon;
        }

        public bool ChangeReferencePlane(in ReferencePlane oldPlane, in ReferencePlane newPlane)
        {
            if (oldPlane.Id != ReferencePlaneId)
                return false;
            if (oldPlane.Id != newPlane.Id)
                return true;

            Polygon = Polygon.ChangePlane(oldPlane.Plane, newPlane.Plane);

            return true;
        }

        public CoordinateSystem GetDisplaySystem(in ReferencePlane referencePlane)
        {
            if(referencePlane.Id != ReferencePlaneId)
            {
                throw new Exception();
            }
            var x = referencePlane.Plane.PlaneX.x;
            CoordinateSystem coo;
            if (Math.Abs(referencePlane.Plane.Normal.z) > Constants.RSQRT2)
            { // Ansicht von oben bzw. unten (Lokale y Achse zeigt nach Norden)
                var z = referencePlane.Plane.PlaneX.z;
                if (z < 0)
                {
                    (x, z) = (-x, -z);
                }
                coo = new CoordinateSystem(referencePlane.Plane.Position, referencePlane.Plane.Normal, Axes.Z, new Vector(x, 0, z));
            }
            else
            { // Ansicht von der Seite (Lokale x Achse ist waagerecht)
                var y = referencePlane.Plane.PlaneX.y;
                if (y < 0)
                {
                    (x, y) = (-x, -y);
                }
                coo = new CoordinateSystem(referencePlane.Plane.Position, referencePlane.Plane.Normal, Axes.Z, new Vector(x, y, 0));
            }
            var ext2 = new D2.LineString(coo,  new D3.LineString(referencePlane.Plane, Polygon[0]));
            var pos = coo.FromPlaneSystem(ext2.BBox.Min);
            return new CoordinateSystem(pos, coo.Rotation);
        }

        public D2.Polygon ToDisplaySystem(in ReferencePlane referencePlane, in CoordinateSystem displaySystem)
        {
            if (referencePlane.Id != ReferencePlaneId)
            {
                throw new Exception();
            }
            return Polygon.ChangePlane(referencePlane.Plane, displaySystem);
        }

        //public static IReadOnlyDictionary<Id, Plane> GetUniqueFaces(IReadOnlyCollection<PlanarFace> planarFaces, double maxDihedralAngleDiff, double maxDiffD, out IReadOnlyList<string> failedFaces)
        //{
        //    // finde multiple PlanarFaces (selbe Object und FaceId)
        //    var unique = ImmutableDictionary.CreateBuilder<Id, Plane>();
        //    var failed = ImmutableArray.CreateBuilder<string>();
        //    foreach (var pf in planarFaces)
        //    {
        //        if (unique.TryGetValue(pf.Id, out var plane))
        //        {
        //            pf.Plane.Difference(plane, out var dA, out var dD);
        //            if (dA > maxDihedralAngleDiff || dD > maxDiffD)
        //            {
        //                failed.Add($"PlanarFace {pf.Id} hat einen Winkelfehler von {dA}rad und eine Differenz (D) von {dD}m");
        //                unique.Remove(pf.Id);
        //                continue;
        //            }
        //        }
        //        else
        //        {
        //            unique[pf.Id] = pf.Plane;
        //        }
        //    }
        //    failedFaces = failed.ToImmutable();
        //    return unique.ToImmutable();
        //}

        public const string CsvHeader = "StateId;ObjectGuid;FaceId;PlaneId;BtmLft;BtmRgt;TopRgt;TopLft;BBoxMin;BBoxMax;Polygon";

        public const string ShortCsvHeader = "StateId;ObjectGuid;FaceId;Polygon";

        public const int LineCount = 11;

        public const int ShortLineCount = 4;

        public string ToCsvString()
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
            line[10] = Polygon.ToWktString();
            return string.Join(";", line);
        }

        public static bool TryParseCsvLine(string line, out PlanarFace? planarFace, out string error)
        {
            if (string.IsNullOrEmpty(line))
            {
                error = "PlanarFace.ParseCsvLine: Input string is null or empty";
                planarFace = null;
                return false;
            }
            var strings = line.Split(new[] { ';' });
            if (strings.Length == LineCount
                && Vector.TryParseWkt(strings[4], out var btmLft)
                && Vector.TryParseWkt(strings[5], out var btmRgt)
                && Vector.TryParseWkt(strings[6], out var topRgt)
                && Vector.TryParseWkt(strings[7], out var topLft)
                && Vector.TryParseWkt(strings[8], out var boxMin)
                && Vector.TryParseWkt(strings[9], out var boxMax)
                && D2.Polygon.TryParseWkt(strings[10], out var polygon))
            {
                error = string.Empty;
                planarFace = new PlanarFace(new Id(strings[0], strings[1], strings[2]), strings[3], btmLft, btmRgt, topRgt, topLft, new BBox(boxMin, boxMax), polygon);
                return true;
            }
            error = $"PlanarFace.ParseCsvLine: Line: \r\n{line}\r\n is not readable";
            planarFace = default;
            return false;
        }

        public static bool TryParseShortCsvLine(string line, out PlanarFace? planarFace, out ReferencePlane? referencePlane, out double maxPlaneDist, out string error)
        {
            if (string.IsNullOrEmpty(line))
            {
                error = "PlanarFace.ParseCsvLine: Input string is null or empty";
                planarFace = null;
                referencePlane = null;
                maxPlaneDist = default;
                return false;
            }
            var strings = line.Split(new[] { ';' });
            if (strings.Length == ShortLineCount
                && D3.Polygon.TryParseWkt(strings[3], out var polygon, out maxPlaneDist))
            {
                error = string.Empty;
                referencePlane = new ReferencePlane(polygon.Plane);
                planarFace = new PlanarFace(
                    new Id(strings[0], strings[1], strings[2]), 
                    referencePlane.Value, 
                    polygon.BBox,
                    polygon.Polygon2D);
                return true;
            }
            error = $"PlanarFace.ParseCsvLine: Line: \r\n{line}\r\n is not readable";
            planarFace = null;
            referencePlane = null;
            maxPlaneDist = default;
            return false;
        }
        public override bool Equals(object? obj) => obj is PlanarFace face && Equals(face);

        public bool Equals(PlanarFace? other) => other?.Id.Equals(Id)??false;

        public override int GetHashCode() => Id.GetHashCode();

        public static bool operator ==(in PlanarFace left, in PlanarFace right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(in PlanarFace left, in PlanarFace right)
        {
            return !(left == right);
        }

        //public static IReadOnlyDictionary<Id, Plane> UniquePlanesById(in IReadOnlyCollection<PlanarFace> planarFaces)
        //{
        //    var facePlanes = new Dictionary<Id, Plane>(planarFaces.Count);
        //    foreach (var pf in planarFaces)
        //    {
        //        facePlanes[pf.Id] = pf.Plane;
        //    }
        //    return facePlanes;
        //}

        public static void WriteObj(in string path, in IReadOnlyDictionary<string, ReferencePlane> planes, in IEnumerable<PlanarFace> planarFaces)
        {
            using var file = File.CreateText(path + ".obj");
            var faces = new List<string>();
            int vertexCnt = 0;
            void AddLs(IPlane plane, D2.LineString ls)
            {
                int first = vertexCnt;
                var face = new StringBuilder("f");
                for (int i = 1; i < ls.Count; i++)
                {
                    file.WriteLine($"v {plane.FromPlaneSystem(ls[i])}");
                    face.AppendFormat(" {0}", first + i);
                }
                faces.Add(face.ToString());
                vertexCnt += ls.Count - 1;
            }
            foreach (var pf in planarFaces)
            {
                faces.Add($"# {pf.Id}");
                foreach (var ls in pf.Polygon)
                {
                    AddLs(planes[pf.ReferencePlaneId].Plane, ls);
                }
            }
            foreach (var face in faces)
            {
                file.WriteLine(face);
            }
        }

        public static void WriteCsv(in string path, in IReadOnlyCollection<PlanarFace> planarFaces)
        {
            using var csv = File.CreateText(path);
            csv.WriteLine(CsvHeader);
            foreach (var pf in planarFaces)
            {
                csv.WriteLine(pf.ToCsvString());
            }
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
                lineErrors = Array.Empty<string>();
                error = "PlanarFace.ReadCsv: " + e.Message;
                return faces;
            }
            if (lines.Length > 1)
            {
                var errors = new List<string>();
                for (int i = 1; i < lines.Length; i++)
                {
                    if (TryParseCsvLine(lines[i], out var pf, out error))
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
            lineErrors = Array.Empty<string>();
            error = "PlanarFace.ReadCsv: CSV-File has no data lines";
            return faces;
        }


        public static HashSet<PlanarFace> ReadCsv(in string path, out Dictionary<string, ReferencePlane> referencePlanes, out Dictionary<Id, double> maxPlaneDists, out string[] lineErrors, out string error)
        {
            string[] lines;
            var faces = new HashSet<PlanarFace>();
            referencePlanes = new Dictionary<string, ReferencePlane>();
            maxPlaneDists = new Dictionary<Id, double>();
            try
            {
                lines = File.ReadAllLines(path);
            }
            catch (Exception e)
            {
                lineErrors = Array.Empty<string>();
                error = "PlanarFace.ReadCsv: " + e.Message;
                return faces;
            }
            if (lines.Length > 1)
            {
                var errors = new List<string>();
                for (int i = 1; i < lines.Length; i++)
                {
                    if (TryParseShortCsvLine(lines[i], out var pf, out var rp, out var mpd, out error))
                    {
                        faces.Add(pf!);
                        referencePlanes[rp!.Value.Id] = rp.Value;
                        maxPlaneDists[pf!.Id] = mpd;
                        continue;
                    }
                    errors.Add($"Line {i + 1} has Error: {error}");
                    error = string.Empty;
                }
                lineErrors = errors.ToArray();
                error = string.Empty;
                return faces;
            }
            lineErrors = Array.Empty<string>();
            error = "PlanarFace.ReadCsv: CSV-File has no data lines";
            return faces;
        }

    }
}
