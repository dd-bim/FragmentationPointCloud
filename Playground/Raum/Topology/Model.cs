using Playground.Raum.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace Playground.Raum.Topology
{
    internal class Model
    {
        public Facet Exterior { get; private set; } = new HalfEdge().RefFacet;

        public List<Facet> Facets { get; } = [];
        public List<Vertex> Vertices { get; } = [];
        public List<HalfEdge> HalfEdges { get; } = [];

        public Dictionary<Guid, HashSet<Element>> RefIdElements { get; } = [];

        public Dictionary<VecI, Vertex> PointVertices { get; } = [];


        public static bool Create(byte digits, (double x, double y)[][][] multiPolygonA, (double x, double y)[][][] multiPolygonB, out Model model)
        {
            model = new Model();
            // Create BoundingBox
            var box = new BoundingBox();
            box.Extend(multiPolygonA);
            box.Extend(multiPolygonB);
            var epsilon = new Epsilon(digits, box);
            var pointSet = new HashSet<VecI>();
            var multiA = AddPoints(epsilon, pointSet, multiPolygonA);
            var multiB = AddPoints(epsilon, pointSet, multiPolygonB);

            var triangulation = new DelaunatorSharp.Delaunator([.. pointSet]);
            var halfEdgeMap = new Dictionary<int, HalfEdge>(triangulation.Halfedges.Length);
            var vertexMap = new Dictionary<int, Vertex>(triangulation.Points.Length);
            for (int i = 0; i < triangulation.Triangles.Length; i += 3)
            {
                var f = AddFacet(null, null);
                var a = CoordinateMap[triangulation.Triangles[i]];
                var b = CoordinateMap[triangulation.Triangles[i + 2]];
                var c = CoordinateMap[triangulation.Triangles[i + 1]];
                var sides = new List<HalfEdge>(3);
                foreach ((var src, var tgt, int ts) in new[]{
                    (a, b, i + 2), (b, c, i + 1), (c, a, i)})
                {
                    (ei, isForward) = Geometry.AddEdge(src.Id, tgt.Id);
                    if (!sections.TryGetValue(ei, out var s))
                    {
                        var e = isForward ? AddEdge(ei, src, tgt, out _) : AddEdge(ei, tgt, src, out _);
                        s = AddSection(0, 1, e);
                        (he, tw) = isForward ? (s.Forward, s.Forward.Twin) : (s.Forward.Twin, s.Forward);
                        sections.Add(ei, s);
                    }
                    else he = isForward ? s.Forward : s.Forward.Twin;
                    sides.Add(he);
                    he.refFacet = f;
                }
                f.RefHalfEdge = sides[0];
                sides[0].Next = sides[1];
                sides[1].Next = sides[2];
                sides[2].Next = sides[0];
            }
            (ei, isForward) = Geometry.AddEdge(triangulation.Delaunator.Hull[^1], triangulation.Delaunator.Hull[0]);
            var prev = isForward ? sections[ei].Forward : sections[ei].Forward.Twin;
            var first = prev;
            External.RefHalfEdge = prev;
            for (int i = 1; i < triangulation.Delaunator.Hull.Length; i++)
            {
                (ei, isForward) = Geometry.AddEdge(triangulation.Delaunator.Hull[i - 1], triangulation.Delaunator.Hull[i]);
                var s = sections[ei];
                he = isForward ? s.Forward : s.Forward.Twin;
                he.RefFacet = External;
                prev.Next = he;
                prev = he;
            }
            prev.Next = first;

            return false;
        }

        private static VecI[][][] AddPoints(Epsilon epsilon, HashSet<VecI> points,  (double x, double y)[][][] multiPolygon)
        {
            var multiPolygonI = new VecI[multiPolygon.Length][][];
            for (int i = 0; i < multiPolygon.Length; i++)
            {
                (double x, double y)[][] polygon = multiPolygon[i];
                var polygonI = multiPolygonI[i] = new VecI[polygon.Length][];
                for (int j = 0; j < polygon.Length; j++)
                {
                    (double x, double y)[] contour = polygon[j];
                    var contourI = polygonI[j] = new VecI[contour.Length];
                    for(int k = 0; k < contour.Length; k++)
                    {
                        var (x, y) = contour[k];
                        var point = contourI[k] = new VecI(epsilon, x, y);
                        _ = points.Add(point);
                    }
                }
            }
            return multiPolygonI;
        }

    }
}
