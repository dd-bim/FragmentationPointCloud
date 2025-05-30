using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Autodesk.Revit.DB;
using static Revit.Extensions;

using RD = Revit.Data;
using Octant = Revit.Data.Octant;
using Id = Revit.Data.Id;

namespace Revit.Green3DScan.SimulatePointCloud
{
    public static class RayCasting
    {
        private static readonly NormalDistribution _nrmDistrib = new(0);

        public static HashSet<Id>[] VisibleFaces(
            Dictionary<Id, RD.PlanarFace> planarFaces,
            Dictionary<string, RD.ReferencePlane> refPlanes, 
            List<XYZ> stations,
            SettingsJson settings, 
            out Dictionary<Id, int> countPoints,
            out XYZ[][] pointClouds,
            bool addNoise = false)
        {
            countPoints = [];
            var visibleFacesOfStations = new HashSet<RD.Id>[stations.Count];
            pointClouds = new XYZ[stations.Count][];
            if(addNoise)
                _nrmDistrib.StdDev = settings.NoiseOfScanner_Meter;

            for (int i = 0; i < visibleFacesOfStations.Length; i++)
            {
                visibleFacesOfStations[i] = visibleFaces(planarFaces, refPlanes, stations[i], settings, countPoints, out var pointCloud, addNoise);
                pointClouds[i] = pointCloud;
            }
            return visibleFacesOfStations;
        }

        private static bool GetMinDist(
            Dictionary<Id, RD.PlanarFace> planarFaces,
            Dictionary<string, RD.ReferencePlane> refPlanes, 
            Dictionary<Octant, HashSet<Id>> octants,
            XYZ station, XYZ direction, SettingsJson settings, 
            out Id minId, out XYZ minPoint,
            bool addNoise)
        {
            // test only faces in the correct octant
            var octantFaces = octants[direction.GetOctant()];
            double minDistance = double.PositiveInfinity;
            minPoint = XYZ.Zero;
            minId = new Id();

            foreach (var id in octantFaces)
            {
                var plane = refPlanes[planarFaces[id].ReferencePlaneId].Plane;
                double cos = direction.DotProduct(plane.Normal);

                //filtering by direction
                if (cos > -Constants.TRIGTOL) // in Revit the normal is defined out of solid
                    continue;

                // intersections
                double planeDistance = (plane.Origin - station).DotProduct(plane.Normal);
                double distance = planeDistance / cos;

                if (distance < settings.MinDF_Meter 
                    || distance > settings.MaxDF_Meter
                    || distance >= minDistance) 
                    continue;

                var intersectionPoint = station + (distance * direction);
                // point in collection?
                plane.Project(intersectionPoint, out var projectedPoint, out _);
                if (!planarFaces[id].Tin.Intersects(projectedPoint)) 
                    continue;
                minPoint = addNoise
                    ? new XYZ(
                        intersectionPoint.X + _nrmDistrib.Next,
                        intersectionPoint.Y + _nrmDistrib.Next,
                        intersectionPoint.Z + _nrmDistrib.Next)
                    : intersectionPoint;

                minDistance = distance;
                minId = id;
            }

            return !double.IsInfinity(minDistance);
        }

        private static HashSet<Id> visibleFaces(
            Dictionary<Id, RD.PlanarFace> planarFaces,
            Dictionary<string, RD.ReferencePlane> refPlanes, 
            XYZ station, 
            SettingsJson settings,
            Dictionary<Id, int> countPoints,
            out XYZ[] pointCloud,
            bool addNoise)
        {
            var visibleFaces = new HashSet<Id>();
            //var visibleFacesListPoints = new List<S.Id>();
            var points = new List<XYZ>();

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
            foreach (var planarFace in planarFaces.Values)
            {
                var oct = (planarFace.BtmLft - station).GetOctant();
                oct |= (planarFace.BtmRgt - station).GetOctant();
                oct |= (planarFace.TopRgt - station).GetOctant();
                oct |= (planarFace.TopLft - station).GetOctant();
                foreach (var (octant, ids) in octants)
                {
                    if (oct.HasFlag(octant))
                        ids.Add(planarFace.Id);
                }
            }

            int halfSteps = settings.StepsPerFullTurn / 2;
            var azimuth = UV.BasisU;
            var inclination = UV.BasisU;
            var step = (double.Tau / settings.StepsPerFullTurn).ToDirection();
            double beta = settings.Beta_Degree * Constants.gradToRad;

            void AddChecked(XYZ direction)
            {
                if (GetMinDist(planarFaces, refPlanes, octants, station, direction, settings, out var minId, out var minPoint, addNoise))
                {
                    double angle = double.Acos(
                        ToDirection(azimuth, inclination)
                        .DotProduct(refPlanes[planarFaces[minId].ReferencePlaneId].Plane.Normal));
                    if (angle < beta)
                    {
                        visibleFaces.Add(minId);

                        if (!countPoints.TryAdd(minId, 1))
                            countPoints[minId]++;

                        points.Add(minPoint);
                    }
                }
            }

            // faces at the poles
            AddChecked(XYZ.BasisZ);
            AddChecked(-XYZ.BasisZ);

            for (int i = 0; i < settings.StepsPerFullTurn; i++)
            {
                inclination = step;
                for (int j = 1; j < halfSteps; j++)
                {
                    var dir = ToDirection(azimuth, inclination);
                    AddChecked(dir);
                    inclination += step;
                }

                azimuth += step;
            }

            pointCloud = [.. points];
            return visibleFaces;
        }

 
    }

    internal sealed class NormalDistribution(double stdDev)
    {
        private readonly Random _random = new();
        private double? _next = null;

        public double StdDev { get; set; } = stdDev;

        public double Next
        {
            get
            {
                if (_next.HasValue)
                {
                    double value = _next.Value;
                    _next = null;
                    return StdDev * value;
                }
                return StdDev * nextDouble();
            }
            set => _next = value;
        }

        private double nextDouble()
        {
            double u, v, s;
            do
            {
                u = _random.NextDouble() * 2 - 1;
                v = _random.NextDouble() * 2 - 1;
                s = (u * u) + (v * v);
            } while (s >= 1 || s == 0);
            s = double.Sqrt(-2.0 * double.Log(s) / s);
            _next = v * s;
            return u * s;
        }
    }
}