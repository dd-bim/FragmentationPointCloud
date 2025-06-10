using Autodesk.Revit.DB;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

using static Revit.Extensions;

using Id = Revit.Data.Id;
using Octant = Revit.Data.Octant;
using RD = Revit.Data;

namespace Revit.Green3DScan.SimulatePointCloud
{
    public sealed class RayCasting
    {
        private static readonly NormalDistribution _nrmDistrib = new(0);

        private readonly Dictionary<Id, RD.PlanarFace> _planarFaces;
        private readonly Dictionary<string, RD.ReferencePlane> _refPlanes;
        private readonly bool _addNoise;
        private readonly int _stepsPerFullTurn;
        private readonly int _halfSteps;
        private readonly UV _step;
        private readonly double _cosbeta;
        private readonly double _minDF_Meter;
        private readonly double _maxDF_Meter;

        public RayCasting(
            Dictionary<Id, RD.PlanarFace> planarFaces,
            Dictionary<string, RD.ReferencePlane> refPlanes,
            SettingsJson settings,
            bool addNoise = false)
        {
            _planarFaces = planarFaces;
            _refPlanes = refPlanes;
            _stepsPerFullTurn = settings.StepsPerFullTurn;
            _halfSteps = _stepsPerFullTurn / 2;
            _step = (double.Tau / settings.StepsPerFullTurn).ToDirection();
            _cosbeta = -double.Cos(settings.Beta_Degree * Constants.gradToRad);
            _addNoise = addNoise;
            _nrmDistrib.StdDev = addNoise ? settings.NoiseOfScanner_Meter : 0;
            _minDF_Meter = settings.MinDF_Meter;
            _maxDF_Meter = settings.MaxDF_Meter;
        }

        public HashSet<Id>[] VisibleFaces(
            List<XYZ> stations,
            out Dictionary<Id, int> countPoints)
        {
            // countPoints threadsicher machen
            var concurrentCountPoints = new ConcurrentDictionary<Id, int>();
            var visibleFacesOfStations = new HashSet<Id>[stations.Count];

            Parallel.For(0, stations.Count, i =>
            {
                visibleFacesOfStations[i] = visibleFaces(stations[i], concurrentCountPoints, out _);
            });
            countPoints = new Dictionary<Id, int>(concurrentCountPoints);
            return visibleFacesOfStations;
        }

        public XYZ[][] PointClouds(List<XYZ> stations)
        {
            var pointClouds = new XYZ[stations.Count][];
            Parallel.For(0, stations.Count, i =>
            {
                _ = visibleFaces(stations[i], null, out var pointCloud);
                pointClouds[i] = pointCloud;
            });
            return pointClouds;
        }

        private HashSet<Id> visibleFaces(
             XYZ station,
             ConcurrentDictionary<Id, int>? countPoints,
             out XYZ[] pointCloud)
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
            foreach (var planarFace in _planarFaces.Values)
            {
                var normal = _refPlanes[planarFace.ReferencePlaneId].Plane.Normal;
                var btm = (planarFace.BtmLft + planarFace.BtmRgt) * 0.5;
                var top = (planarFace.TopLft + planarFace.TopRgt) * 0.5;
                var left = (planarFace.BtmLft + planarFace.TopLft) * 0.5;
                var right = (planarFace.BtmRgt + planarFace.TopRgt) * 0.5;
                var center = (btm + top + left + right) * 0.25;
                var oct = (planarFace.BtmLft - station).GetOctant(normal);
                oct |= (planarFace.BtmRgt - station).GetOctant(normal);
                oct |= (planarFace.TopRgt - station).GetOctant(normal);
                oct |= (planarFace.TopLft - station).GetOctant(normal);
                oct |= (center - station).GetOctant(normal);
                oct |= (btm - station).GetOctant(normal);
                oct |= (top - station).GetOctant(normal);
                oct |= (left - station).GetOctant(normal);
                oct |= (right - station).GetOctant(normal);
                foreach (var (octant, ids) in octants)
                {
                    if (oct.HasFlag(octant))
                        ids.Add(planarFace.Id);
                }
            }

            void AddChecked(XYZ direction)
            {
                if (GetMinDist(octants, station, direction, out var minId, out var minPoint))
                {
                    //var dir = ToDirection(azimuth, inclination);
                    //var nrm = _refPlanes[_planarFaces[minId].ReferencePlaneId].Plane.Normal;
                    //double cos = dir.DotProduct(nrm);
                    ////double angle = double.Acos(dir.DotProduct(nrm));
                    ////if (angle < _beta)
                    //if(cos < _cosbeta)
                    //{
                        visibleFaces.Add(minId);

                        if (countPoints is not null
                            && !countPoints.TryAdd(minId, 1))
                            countPoints[minId]++;

                        points.Add(minPoint);
                    //}
                }
            }

            // faces at the poles
            AddChecked(XYZ.BasisZ);
            AddChecked(-XYZ.BasisZ);

            var azimuth = UV.BasisU;
            for (int i = 0; i < _stepsPerFullTurn; i++)
            {
                var inclination = _step;
                for (int j = 1; j < _halfSteps; j++)
                {
                    var dir = ToDirection(azimuth, inclination);
                    AddChecked(dir);
                    inclination = inclination.DirectionAdd(_step);
                }

                azimuth = azimuth.DirectionAdd(_step);
            }

            pointCloud = [.. points];
            return visibleFaces;
        }

        private bool GetMinDist(
            Dictionary<Octant, HashSet<Id>> octants,
            XYZ station, XYZ direction,
            out Id minId, out XYZ minPoint)
        {
            // test only faces in the correct octant
            var octantFaces = octants[direction.GetOctant()];
            double minDistance = double.PositiveInfinity;
            minPoint = XYZ.Zero;
            minId = new Id();

            foreach (var id in octantFaces)
            {
                var plane = _refPlanes[_planarFaces[id].ReferencePlaneId].Plane;
                double cos = direction.DotProduct(plane.Normal);

                //filtering by direction
                if (cos > _cosbeta) // in Revit the normal is defined out of solid
                    continue;

                // intersections
                double planeDistance = (plane.Origin - station).DotProduct(plane.Normal);
                double distance = planeDistance / cos;

                if (distance < _minDF_Meter
                    || distance > _maxDF_Meter
                    || distance >= minDistance)
                    continue;

                var intersectionPoint = station + (distance * direction);
                // point in collection?
                plane.Project(intersectionPoint, out var projectedPoint, out _);
                if (!_planarFaces[id].Tin.Intersects(projectedPoint))
                    continue;
                minPoint = _addNoise
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


    }

    internal sealed class NormalDistribution(double stdDev)
    {
        // Thread-local Random und _next für Thread-Sicherheit
        [ThreadStatic]
        private static Random? _threadRandom;
        [ThreadStatic]
        private static double? _threadNext;

        public double StdDev { get; set; } = stdDev;

        public double Next
        {
            get
            {
                if (_threadNext.HasValue)
                {
                    double value = _threadNext.Value;
                    _threadNext = null;
                    return StdDev * value;
                }
                return StdDev * nextDouble();
            }
            set => _threadNext = value;
        }

        private double nextDouble()
        {
            var random = _threadRandom ??= new Random(Guid.NewGuid().GetHashCode());
            double u, v, s;
            do
            {
                u = (random.NextDouble() * 2) - 1;
                v = (random.NextDouble() * 2) - 1;
                s = (u * u) + (v * v);
            } while (s >= 1 || s == 0);
            s = double.Sqrt(-2.0 * double.Log(s) / s);
            _threadNext = v * s;
            return u * s;
        }
    }

}