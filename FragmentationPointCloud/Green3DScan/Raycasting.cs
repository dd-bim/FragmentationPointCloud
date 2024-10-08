﻿using System;
using System.Collections.Generic;
using D2 = GeometryLib.Double.D2;
using D3 = GeometryLib.Double.D3;
using D = Revit.Data;
using S = ScantraIO.Data;
using YamlDotNet.Core.Tokens;
using Serilog;
using OpenCvSharp;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace Revit.Green3DScan
{
    public static class Raycasting
    {
        public static HashSet<S.Id>[] VisibleFaces(IReadOnlyCollection<S.PlanarFace> planarFaces, IReadOnlyDictionary<string, S.ReferencePlane> refPlanes, IReadOnlyList<D3.Vector> stations, SettingsJson set, out D3.Vector[][] pointClouds, out Dictionary<S.Id, int> countPoints, out HashSet<S.Id> hashPMin)
        {
            countPoints = default;
            Dictionary<S.Id, int> count = new Dictionary<S.Id, int>();
            var pFMap = new Dictionary<S.Id, S.PlanarFace>();
            foreach (var pf in planarFaces)
            {
                pFMap[pf.Id] = pf;
            }
            var visibleWithPMin = new HashSet<S.Id>();
            var vf = new HashSet<S.Id>[stations.Count];
            pointClouds = new D3.Vector[vf.Length][];
            for (int i = 0; i < vf.Length; i++)
            {
                vf[i] = VisibleFaces(pFMap, refPlanes, stations[i], set, count, out var pointCloud, out count);
                pointClouds[i] = pointCloud;
            }
            countPoints = count;
            var pMin = set.StepsPerFullTurn * set.StepsPerFullTurn * set.Beta_Degree / 25000;
            Log.Information(pMin.ToString() + " pMin");
            //Test einbauen, ob gewisse mindestanzahl erreicht wurde
            foreach (var pf in count)
            {
                if (pf.Value >= pMin)
                {
                    visibleWithPMin.Add(pf.Key);
                    Log.Information(pf.Key.ToString());
                    Log.Information(pf.Value.ToString());
                }
                else
                {
                    Log.Information(pf.Value.ToString());
                }
            }
            hashPMin = visibleWithPMin;
            return vf;
        }

        private static D.Octant GetOctant(D3.Vector vector)
        {
            var octant = vector.x < 0 ? D.Octant.XNeg : D.Octant.XPlus;
            octant |= vector.y < 0 ? D.Octant.YNeg : D.Octant.YPlus;
            octant |= vector.z < 0 ? D.Octant.ZNeg : D.Octant.ZPlus;
            return octant;
        }

        private static bool GetMinDist(Dictionary<S.Id, S.PlanarFace> pFMap, IReadOnlyDictionary<string, S.ReferencePlane> refPlanes, Dictionary<D.Octant, HashSet<S.Id>> octants, D3.Vector station, D3.Direction direction, SettingsJson set, out S.Id minId, out D3.Vector minPoint)
        {
            // test only faces in the correct octant
            var octantFaces = octants[GetOctant(direction)];
            var minDistance = double.PositiveInfinity;
            minPoint = default;
            minId = new S.Id();

            foreach (var id in octantFaces)
            {
                var pfRefPlane = refPlanes[pFMap[id].ReferencePlaneId].Plane;
                var r_ = direction.Dot(pfRefPlane.Normal);

                //filtering by direction
                if (r_ > -GeometryLib.Double.Constants.TRIGTOL) // in Revit the normal is defined out of solid
                {
                    continue;
                }

                // intersections
                var p_ = (pfRefPlane.Position - station).Dot(pfRefPlane.Normal);
                var distance = p_ / r_;
                if (distance < set.MinDF_Meter || distance > set.MaxDF_Meter)
                {
                    continue;
                }
                if (distance < minDistance)
                {
                    var s = station + distance * direction;
                    // point in collection?
                    var ntsPolygon = NTSWrapper.GeometryLib.ToNTSPolygon(pFMap[id].Polygon);
                    var prepPolygon = NTSWrapper.GeometryLib.ToPrepared(ntsPolygon);
                    var point = new NetTopologySuite.Geometries.Point(pfRefPlane.ToPlaneSystem(s).x, pfRefPlane.ToPlaneSystem(s).y);
                    if (prepPolygon.Contains(point))
                    {
                        minPoint = s;
                        minDistance = distance;
                        minId = id;
                    }
                }
            }
            return !double.IsInfinity(minDistance);
        }

        private static HashSet<S.Id> VisibleFaces(Dictionary<S.Id, S.PlanarFace> pfMap, IReadOnlyDictionary<string, S.ReferencePlane> refPlanes, D3.Vector station, SettingsJson set, Dictionary<S.Id, int> countPointsAll, out D3.Vector[] pointCloud, out Dictionary<S.Id, int> countPoints)
        {
            var visibleFaces = new HashSet<S.Id>();
            //var visibleFacesListPoints = new List<S.Id>();
            Dictionary<S.Id, int> frequencyDict = countPointsAll;
            var points = new List<D3.Vector>();

            // create octants
            var octants = new Dictionary<D.Octant, HashSet<S.Id>>{
                {D.Octant.PPP, new HashSet<S.Id>()},
                {D.Octant.NPP, new HashSet<S.Id>()},
                {D.Octant.PNP, new HashSet<S.Id>()},
                {D.Octant.NNP, new HashSet<S.Id>()},
                {D.Octant.PPN, new HashSet<S.Id>()},
                {D.Octant.NPN, new HashSet<S.Id>()},
                {D.Octant.PNN, new HashSet<S.Id>()},
                {D.Octant.NNN, new HashSet<S.Id>()}
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
                    {
                        kv.Value.Add(pf.Id);
                    }
                }
            }

            int halfSteps = set.StepsPerFullTurn / 2;
            var azimuth = D2.Direction.UnitX;
            var inclination = D2.Direction.UnitX;
            var step = new D2.Direction(Math.PI / halfSteps);
            var beta = set.Beta_Degree * Constants.gradToRad;
            S.Id minId;
            D3.Vector minPoint;

            // faces at the poles
            if (GetMinDist(pfMap, refPlanes, octants, station, D3.Direction.UnitZ, set, out minId, out minPoint))
            {
                var angle = Math.Acos(new D3.Direction(azimuth, inclination).Dot(refPlanes[pfMap[minId].ReferencePlaneId].Plane.Normal));
                if (angle < beta)
                {
                    visibleFaces.Add(minId);
                    if (frequencyDict.ContainsKey(minId))
                    {
                        frequencyDict[minId]++;
                    }
                    else
                    {
                        frequencyDict[minId] = 1;
                    }
                    points.Add(minPoint);
                }
            }
            if (GetMinDist(pfMap, refPlanes, octants, station, D3.Direction.NegUnitZ, set, out minId, out minPoint))
            {
                var angle = Math.Acos(new D3.Direction(azimuth, inclination).Dot(refPlanes[pfMap[minId].ReferencePlaneId].Plane.Normal));
                if (angle < beta)
                {
                    visibleFaces.Add(minId);
                    if (frequencyDict.ContainsKey(minId))
                    {
                        frequencyDict[minId]++;
                    }
                    else
                    {
                        frequencyDict[minId] = 1;
                    }
                    points.Add(minPoint);
                }
            }

            for (var i = 0; i < set.StepsPerFullTurn; i++)
            {
                inclination = step;
                for (var j = 1; j < halfSteps; j++)
                {
                    var dir = new D3.Direction(azimuth, inclination);
                    if (GetMinDist(pfMap, refPlanes, octants, station, dir, set, out minId, out minPoint))
                    {
                        var angle = Math.PI - Math.Acos(dir.Dot(refPlanes[pfMap[minId].ReferencePlaneId].Plane.Normal));
                        if (angle < beta)
                        {
                            visibleFaces.Add(minId);
                            if (frequencyDict.ContainsKey(minId))
                            {
                                frequencyDict[minId]++;
                            }
                            else
                            {
                                frequencyDict[minId] = 1;
                            }
                            points.Add(minPoint);
                        }
                    }
                    inclination = inclination.Add(step);
                }
                azimuth = azimuth.Add(step);
            }
            pointCloud = points.ToArray();
            countPoints = frequencyDict;
            return visibleFaces;
        }
    }
}