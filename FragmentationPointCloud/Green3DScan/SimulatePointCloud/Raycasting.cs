using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Revit.Data;
//using D2_Direction = GeometryLib.D2.Direction;
//using D2_Vector = GeometryLib.D2.Vector;
//using D3_Direction = GeometryLib.D3.Direction;
//using Plane = GeometryLib.D3.Plane;
//using D3_Vector = GeometryLib.D3.Vector;

namespace Revit.Green3DScan.SimulatePointCloud
{
    public static class RayCasting
    {
        public static HashSet<Id>[] VisibleFaces(IReadOnlyCollection<PlanarFace> planarFaces,
            IReadOnlyDictionary<string, ReferencePlane> refPlanes, IReadOnlyList<D3_Vector> stations,
            SettingsJson set, out D3_Vector[][] pointClouds, out Dictionary<Id, int> countPoints,
            out HashSet<Id> hashPMin)
        {
            countPoints = null;
            var count = new Dictionary<Id, int>();
            var pFMap = new Dictionary<Id, PlanarFace>();
            foreach (var pf in planarFaces) pFMap[pf.Id] = pf;
            var visibleWithPMin = new HashSet<Id>();
            var vf = new HashSet<Id>[stations.Count];
            pointClouds = new D3_Vector[vf.Length][];
            for (var i = 0; i < vf.Length; i++)
            {
                vf[i] = VisibleFaces(pFMap, refPlanes, stations[i], set, count, out var pointCloud, out count);
                pointClouds[i] = pointCloud;
            }

            countPoints = count;
            const int pMin = 1;
            // TODO Test with minimum number of points on face
            //var pMin = set.StepsPerFullTurn * set.StepsPerFullTurn * set.Beta_Degree / 25000;
            //Log.Information(pMin.ToString() + " pMin");
            // Test if a minimum number has been reached
            foreach (var pf in count.Where(pf => pf.Value >= pMin))
            {
                visibleWithPMin.Add(pf.Key);
            }

            //Log.Information(pf.Key.ToString());
            //Log.Information(pf.Value.ToString());
            //Log.Information(pf.Value.ToString());
            hashPMin = visibleWithPMin;
            return vf;
        }

        private static Octant GetOctant(D3_Vector vector)
        {
            var octant = vector.x < 0 ? Octant.XNeg : Octant.XPlus;
            octant |= vector.y < 0 ? Octant.YNeg : Octant.YPlus;
            octant |= vector.z < 0 ? Octant.ZNeg : Octant.ZPlus;
            return octant;
        }

        private static bool GetMinDist(Dictionary<Id, PlanarFace> pFMap,
            IReadOnlyDictionary<string, ReferencePlane> refPlanes, Dictionary<Octant, HashSet<Id>> octants,
            D3_Vector station, D3_Direction direction, SettingsJson set, out Id minId, out D3_Vector minPoint)
        {
            // test only faces in the correct octant
            var octantFaces = octants[GetOctant(direction)];
            double minDistance = double.PositiveInfinity;
            minPoint = default;
            minId = new Id();

            foreach (var id in octantFaces)
            {
                Plane pfRefPlane = refPlanes[pFMap[id].ReferencePlaneId].Plane;
                double r_ = direction.Dot(pfRefPlane.Normal);

                //filtering by direction
                if (r_ > -GeometryLib.Constants.TRIGTOL) // in Revit the normal is defined out of solid
                    continue;

                // intersections
                double p_ = (pfRefPlane.Position - station).Dot(pfRefPlane.Normal);
                double distance = p_ / r_;
                if (distance < set.MinDF_Meter || distance > set.MaxDF_Meter) continue;

                if (!(distance < minDistance)) continue;
                D3_Vector s = station + distance * direction;
                // point in collection?
                D2_Vector point = pfRefPlane.ToPlaneSystem(s);
                if (!pFMap[id].Polygon.IsPointInPolygon(point)) continue;
                minPoint = s;
                minDistance = distance;
                minId = id;
            }

            return !double.IsInfinity(minDistance);
        }

        private static HashSet<Id> VisibleFaces(Dictionary<Id, PlanarFace> pfMap,
            IReadOnlyDictionary<string, ReferencePlane> refPlanes, D3_Vector station, SettingsJson set,
            Dictionary<Id, int> countPointsAll, out D3_Vector[] pointCloud, out Dictionary<Id, int> countPoints)
        {
            var visibleFaces = new HashSet<Id>();
            //var visibleFacesListPoints = new List<S.Id>();
            var frequencyDict = countPointsAll;
            var points = new List<D3_Vector>();

            // create octants
            var octants = new Dictionary<Octant, HashSet<Id>>
            {
                { Octant.PPP, new HashSet<Id>() },
                { Octant.NPP, new HashSet<Id>() },
                { Octant.PNP, new HashSet<Id>() },
                { Octant.NNP, new HashSet<Id>() },
                { Octant.PPN, new HashSet<Id>() },
                { Octant.NPN, new HashSet<Id>() },
                { Octant.PNN, new HashSet<Id>() },
                { Octant.NNN, new HashSet<Id>() }
            };

            // assigning faces to octants
            foreach (var pf in pfMap.Values)
            {
                var oct = GetOctant(pf.PlanarBtmLft - station);
                oct |= GetOctant(pf.PlanarBtmRgt - station);
                oct |= GetOctant(pf.PlanarTopRgt - station);
                oct |= GetOctant(pf.PlanarTopLft - station);
                foreach (var kv in octants)
                {
                    if ((oct & kv.Key) == kv.Key)
                        kv.Value.Add(pf.Id);
                }
            }

            int halfSteps = set.StepsPerFullTurn / 2;
            D2_Direction azimuth = D2_Direction.UnitX;
            D2_Direction inclination = D2_Direction.UnitX;
            var step = new D2_Direction(Math.PI / halfSteps);
            double beta = set.Beta_Degree * Constants.gradToRad;

            // faces at the poles
            if (GetMinDist(pfMap, refPlanes, octants, station, D3_Direction.UnitZ, set, out var minId, out var minPoint))
            {
                double angle = Math.Acos(
                    new D3_Direction(azimuth, inclination).Dot(refPlanes[pfMap[minId].ReferencePlaneId].Plane.Normal));
                if (angle < beta)
                {
                    visibleFaces.Add(minId);
                    if (!frequencyDict.TryAdd(minId, 1))
                        frequencyDict[minId]++;

                    points.Add(minPoint);
                }
            }

            if (GetMinDist(pfMap, refPlanes, octants, station, D3_Direction.NegUnitZ, set, out minId, out minPoint))
            {
                double angle = Math.Acos(
                    new D3_Direction(azimuth, inclination).Dot(refPlanes[pfMap[minId].ReferencePlaneId].Plane.Normal));
                if (angle < beta)
                {
                    visibleFaces.Add(minId);
                    if (!frequencyDict.TryAdd(minId, 1))
                        frequencyDict[minId]++;

                    points.Add(minPoint);
                }
            }

            for (var i = 0; i < set.StepsPerFullTurn; i++)
            {
                inclination = step;
                for (var j = 1; j < halfSteps; j++)
                {
                    var dir = new D3_Direction(azimuth, inclination);
                    if (GetMinDist(pfMap, refPlanes, octants, station, dir, set, out minId, out minPoint))
                    {
                        double angle = Math.PI -
                                       Math.Acos(dir.Dot(refPlanes[pfMap[minId].ReferencePlaneId].Plane.Normal));
                        if (angle < beta)
                        {
                            visibleFaces.Add(minId);
                            if (!frequencyDict.TryAdd(minId, 1))
                                frequencyDict[minId]++;

                            points.Add(minPoint);
                        }
                    }

                    inclination += step;
                }

                azimuth += step;
            }

            pointCloud = [.. points];
            countPoints = frequencyDict;
            return visibleFaces;
        }
    }

    public static class CreatePointCloud
    {
        // Normally distributed random number for noise of the distance measurement
        private static readonly Random Random = new();

        public static D3_Vector[][] VisibleFaces(IReadOnlyCollection<PlanarFace> planarFaces,
            IReadOnlyDictionary<string, ReferencePlane> refPlanes, IReadOnlyList<D3_Vector> stations,
            SettingsJson set)
        {
            var pFMap = new Dictionary<Id, PlanarFace>();
            foreach (var pf in planarFaces) pFMap[pf.Id] = pf;
            var vf = new HashSet<Id>[stations.Count];
            var pointClouds = new D3_Vector[vf.Length][];

            var visibleFacesPerStation = new HashSet<Id>[stations.Count];

            Parallel.For(0, stations.Count, i =>
            {
                visibleFacesPerStation[i] = VisibleFaces(pFMap, refPlanes, stations[i], set, out var pointCloud);
                pointClouds[i] = pointCloud;
            });

            return pointClouds;
        }

        private static Octant GetOctant(D3_Vector vector)
        {
            var octant = vector.x < 0 ? Octant.XNeg : Octant.XPlus;
            octant |= vector.y < 0 ? Octant.YNeg : Octant.YPlus;
            octant |= vector.z < 0 ? Octant.ZNeg : Octant.ZPlus;
            return octant;
        }

        private static double GenerateNormalDistribution(double mean, double stdDev)
        {
            double u1 = 1.0 - Random.NextDouble();
            double u2 = 1.0 - Random.NextDouble();
            double randStdNormal =
                Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); // Box-Muller-Transformation
            return mean + stdDev * randStdNormal;
        }

        private static bool GetMinDist(Dictionary<Id, PlanarFace> pFMap,
            IReadOnlyDictionary<string, ReferencePlane> refPlanes, Dictionary<Octant, HashSet<Id>> octants,
            D3_Vector station, D3_Direction direction, SettingsJson set, out Id minId, out D3_Vector minPoint)
        {
            // test only faces in the correct octant
            var octantFaces = octants[GetOctant(direction)];
            double minDistance = double.PositiveInfinity;
            minPoint = default;
            minId = new Id();

            foreach (var id in octantFaces)
            {
                var pfRefPlane = refPlanes[pFMap[id].ReferencePlaneId].Plane;
                double r = direction.Dot(pfRefPlane.Normal);

                // filtering by direction
                if (r > -GeometryLib.Constants.TRIGTOL) // in Revit the normal is defined out of solid
                    continue;

                // intersections
                double p = (pfRefPlane.Position - station).Dot(pfRefPlane.Normal);
                double distance = p / r;
                // no minimum measuring distance
                if (distance > set.MaxDF_Meter) continue;

                if (!(distance < minDistance)) continue;
                var s = station + distance * direction;
                // point in collection?
                var point = pfRefPlane.ToPlaneSystem(s);
                if (!pFMap[id].Polygon.IsPointInPolygon(point)) continue;
                // insert noise
                minPoint = station + (distance + GenerateNormalDistribution(0, set.NoiseOfScanner_Meter)) * direction;
                minDistance = distance;
                minId = id;
            }

            return !double.IsInfinity(minDistance);
        }

        private static HashSet<Id> VisibleFaces(Dictionary<Id, PlanarFace> pfMap,
            IReadOnlyDictionary<string, ReferencePlane> refPlanes, D3_Vector station, SettingsJson set,
            out D3_Vector[] pointCloud)
        {
            var visibleFaces = new HashSet<Id>();
            var points = new List<D3_Vector>();

            // create octants
            var octants = new Dictionary<Octant, HashSet<Id>>
            {
                { Octant.PPP, [] },
                { Octant.NPP, [] },
                { Octant.PNP, [] },
                { Octant.NNP, [] },
                { Octant.PPN, [] },
                { Octant.NPN, [] },
                { Octant.PNN, [] },
                { Octant.NNN, [] }
            };

            // assigning faces to octants
            foreach (var pf in pfMap.Values)
            {
                var oct = GetOctant(pf.PlanarBtmLft - station);
                oct |= GetOctant(pf.PlanarBtmRgt - station);
                oct |= GetOctant(pf.PlanarTopRgt - station);
                oct |= GetOctant(pf.PlanarTopLft - station);
                foreach (var kv in octants.Where(kv => (oct & kv.Key) == kv.Key))
                {
                    kv.Value.Add(pf.Id);
                }
            }

            int halfSteps = set.StepsPerFullTurn / 2;
            var azimuth = D2_Direction.UnitX;
            var inclination = D2_Direction.UnitX;
            var step = new D2_Direction(Math.PI / halfSteps);
            double beta = set.Beta_Degree * Constants.gradToRad;

            // faces at the poles
            if (GetMinDist(pfMap, refPlanes, octants, station, D3_Direction.UnitZ, set, out var minId, out var minPoint))
            {
                double angle = Math.Acos(
                    new D3_Direction(azimuth, inclination).Dot(refPlanes[pfMap[minId].ReferencePlaneId].Plane.Normal));
                if (angle < beta)
                {
                    visibleFaces.Add(minId);
                    points.Add(minPoint);
                }
            }

            if (GetMinDist(pfMap, refPlanes, octants, station, D3_Direction.NegUnitZ, set, out minId, out minPoint))
            {
                double angle = Math.Acos(
                    new D3_Direction(azimuth, inclination).Dot(refPlanes[pfMap[minId].ReferencePlaneId].Plane.Normal));
                if (angle < beta)
                {
                    visibleFaces.Add(minId);
                    points.Add(minPoint);
                }
            }

            for (var i = 0; i < set.StepsPerFullTurn; i++)
            {
                inclination = step;
                for (var j = 1; j < halfSteps; j++)
                {
                    var dir = new D3_Direction(azimuth, inclination);
                    if (GetMinDist(pfMap, refPlanes, octants, station, dir, set, out minId, out minPoint))
                    {
                        double angle = Math.PI -
                                       Math.Acos(dir.Dot(refPlanes[pfMap[minId].ReferencePlaneId].Plane.Normal));
                        if (angle < beta)
                        {
                            visibleFaces.Add(minId);
                            points.Add(minPoint);
                        }
                    }

                    inclination += step;
                }

                azimuth += step;
            }

            pointCloud = [.. points];
            return visibleFaces;
        }
    }
}