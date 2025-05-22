using Playground.Raum.Geometry;
using Playground.Raum.Topology;

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Schema;

namespace Playground.Raum.Topology;

internal class Partition(Epsilon epsilon)
{
    static readonly Random RandomInstance = new(1); // Random seed for reproducibility
    public static bool RandomNext => RandomInstance.Next(2) == 0;

    public const long ExteriorId = 0;

    public bool HasReconstruction { get; private set; } = false;

    public Epsilon Epsilon { get; init; } = epsilon;

    public VecI Min { get; private set; } = new(long.MaxValue, long.MaxValue);

    public VecI Max { get; private set; } = new(long.MinValue, long.MinValue);

    public Facet Exterior { get; } = new HalfEdge(0).RefFacet;

    public List<Facet> Facets { get; } = [];
    public List<Vertex> Vertices { get; } = [];
    public List<HalfEdge> HalfEdges { get; } = [];

    public Dictionary<Guid, HashSet<Vertex>> SemanticVertices { get; } = [];

    public Dictionary<Guid, HashSet<HalfEdge>> SemanticHalfEdges { get; } = [];

    public Dictionary<Guid, HashSet<Facet>> SemanticFacets { get; } = [];

    public Dictionary<VecI, Vertex> PointVertices { get; } = [];

    public static (Feature[] features, (double x, double y)[][][][] multiPolygons) CreateFromRegions(
        Partition partition, params (double x, double y)[][][][] multiPolygons)
    {
        // Create triangulation, Points gets reference ids of multi-geometry and sub-geometries
        var features = new Feature[multiPolygons.Length];
        var edges = new List<(Vertex source, Vertex target)>();
        for (int i = 0; i < multiPolygons.Length; i++)
        {
            features[i] = partition.AddMultiPolygon(edges, multiPolygons[i]);
        }
        //#if DEBUG
        //        foreach (string error in partition.IsValid())
        //            Console.WriteLine(error);
        //        partition.WriteSvg("1_triangulation");
        //#endif

        // Reconstruct edges
        foreach (var (source, target) in edges)
        {
            // assign both sides of the edge the same reference IDs, for a robust polygon reconstruction
            if (!partition.ReconstructEdge(source.RefIds.ToImmutableHashSet().Intersect(target.RefIds), true, source, target))
            {
                throw new Exception($"Failed to reconstruct edge between {source} and {target}.");
            }
        }
        //#if DEBUG
        //        foreach (string error in partition.IsValid())
        //            Console.WriteLine(error);
        //        partition.WriteSvg("2_reconstruction");
        //#endif

        // Determine and assign the facets of the multi-polygons,
        // remove the reference IDs from the half-edges
        for (int i = 0; i < features.Length; i++)
        {   // Multi-polygon
            var multi = features[i];
            var multiRegions = new List<HashSet<Facet>>();
            for (int j = 0; j < multi.SubFeatures.Length; j++)
            {   // Polygon
                var poly = multi.SubFeatures[j];
                var polyIds = ImmutableHashSet.Create<Guid>(multi.Id, poly.Id);
                for (int k = 0; k < poly.SubFeatures.Length; k++)
                {   // Ring
                    var ring = poly.SubFeatures[k];
                    var halfEdges = partition.GetHalfEdgesWithRefId(ring.Id);
                    var regions = EnclosedFacetRegions(halfEdges);
                    multiRegions.AddRange(regions);
                    // remove the reference IDs from the original half-edges
                    var refIds = polyIds.Add(ring.Id);
                    foreach (var halfEdge in halfEdges)
                    {
                        partition.RemoveSemantics(halfEdge, refIds);
                    }
                }
            }
            var validRegions = MakeValidRegions(multiRegions);
            foreach (var region in validRegions)
            {
                foreach (var facet in region)
                {   // set only the reference ID of the multi-polygon to the facets
                    partition.AddSemantics(facet, multi.Id);
                }
            }
        }
        //#if DEBUG
        //        foreach (string error in partition.IsValid())
        //            Console.WriteLine(error);
        //        partition.WriteSvg("3_beforeRevision");
        //#endif

        // Get and assign the revised boundaries of the multi-polygons 
        var revisedMultiPolygons = new (double x, double y)[multiPolygons.Length][][][];
        var revisedFeatures = new Feature[multiPolygons.Length];
        for (int i = 0; i < multiPolygons.Length; i++)
        {
            var multiId = features[i].Id;
            var facets = partition.GetFacetsWithRefId(multiId);
            var regions = GetRegions(facets);
            var multiPolygon = revisedMultiPolygons[i] = new (double x, double y)[regions.Count][][];
            var polys = new Feature[regions.Count];
            revisedFeatures[i] = new Feature(multiId, polys);
            for (int j = 0; j < regions.Count; j++)
            {
                var polyId = Guid.NewGuid();
                var polyRefIds = ImmutableHashSet.Create<Guid>(multiId, polyId);
                var region = regions[j];
                var boundary = RegionBoundary(region);
                var polygon = multiPolygon[j] = new (double x, double y)[boundary.Count][];
                var rings = new Feature[boundary.Count];
                polys[j] = new Feature(polyId, rings);
                for (int k = 0; k < boundary.Count; k++)
                {
                    var ringId = Guid.NewGuid();
                    var refIds = polyRefIds.Add(ringId);
                    rings[k] = new Feature(ringId, []);
                    var halfEdges = boundary[k];
                    if (!PointsOfHalfEdgeChain(partition.Epsilon, halfEdges, out var ring))
                    {
                        throw new Exception($"Failed to get points of half-edge chain for ring {k}.");
                    }
                    polygon[k] = ring;
                    foreach (var he in halfEdges)
                    {
                        partition.AddSemantics(he, refIds);
                    }
                }
            }
        }

        return (revisedFeatures, revisedMultiPolygons);
    }

    #region Structure

    private void AddSemantics(Element element, IReadOnlySet<Guid> refIds)
    {
        element.RefIds.UnionWith(refIds);
        switch (element)
        {
            case Vertex vertex:
                foreach (var refId in refIds)
                {
                    if (SemanticVertices.TryGetValue(refId, out var value))
                    {
                        value.Add(vertex);
                    }
                    else
                    {
                        SemanticVertices[refId] = [vertex];
                    }
                }
                break;
            case HalfEdge halfEdge:
                foreach (var refId in refIds)
                {
                    if (SemanticHalfEdges.TryGetValue(refId, out var value))
                    {
                        value.Add(halfEdge);
                    }
                    else
                    {
                        SemanticHalfEdges[refId] = [halfEdge];
                    }
                }
                break;
            case Facet facet:
                foreach (var refId in refIds)
                {
                    if (SemanticFacets.TryGetValue(refId, out var value))
                    {
                        value.Add(facet);
                    }
                    else
                    {
                        SemanticFacets[refId] = [facet];
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Associates the specified element with a reference ID and updates the semantic mapping accordingly.
    /// </summary>
    /// <remarks>This method updates the <paramref name="element"/>'s reference ID collection by
    /// adding the provided ID. It also ensures that the semantic mapping is updated, associating the reference ID
    /// with the given element. If the reference ID already exists in the semantic mapping, the element is added to
    /// the existing collection of associated elements. Otherwise, a new mapping is created.</remarks>
    /// <param name="element">The element to associate with the provided reference ID.</param>
    /// <param name="refId">The reference ID to associate with the element. Cannot be null.</param>
    private void AddSemantics(Element element, Guid refId)
    {
        element.RefIds.Add(refId);
        switch (element)
        {
            case Vertex vertex:
                if (SemanticVertices.TryGetValue(refId, out var vvalue))
                {
                    vvalue.Add(vertex);
                }
                else
                {
                    SemanticVertices[refId] = [vertex];
                }
                break;
            case HalfEdge halfEdge:
                if (SemanticHalfEdges.TryGetValue(refId, out var hvalue))
                {
                    hvalue.Add(halfEdge);
                }
                else
                {
                    SemanticHalfEdges[refId] = [halfEdge];
                }
                break;
            case Facet facet:
                if (SemanticFacets.TryGetValue(refId, out var fvalue))
                {
                    fvalue.Add(facet);
                }
                else
                {
                    SemanticFacets[refId] = [facet];
                }
                break;
        }
    }

    private void RemoveSemantics(Element element, IReadOnlySet<Guid> refIds)
    {
        element.RefIds.ExceptWith(refIds);
        switch (element)
        {
            case Vertex vertex:
                foreach (var refId in refIds)
                {
                    if (SemanticVertices.TryGetValue(refId, out var value))
                    {
                        value.Remove(vertex);
                    }
                }
                break;
            case HalfEdge halfEdge:
                foreach (var refId in refIds)
                {
                    if (SemanticHalfEdges.TryGetValue(refId, out var value))
                    {
                        value.Remove(halfEdge);
                    }
                }
                break;
            case Facet facet:
                foreach (var refId in refIds)
                {
                    if (SemanticFacets.TryGetValue(refId, out var value))
                    {
                        value.Remove(facet);
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Creates a directed edge between two vertices and optionally associates semantic identifiers with the edge
    /// and its twin.
    /// </summary>
    /// <param name="source">The source vertex of the edge.</param>
    /// <param name="target">The target vertex of the edge.</param>
    /// <param name="heRefIds">A set of semantic identifiers to associate with the primary edge, or <see langword="null"/> if no
    /// identifiers are to be added.</param>
    /// <param name="twRefIds">A set of semantic identifiers to associate with the twin edge, or <see langword="null"/> if no identifiers
    /// are to be added.</param>
    /// <returns>A tuple containing the primary edge and its twin. The primary edge connects the <paramref name="source"/>
    /// vertex to the <paramref name="target"/> vertex, while the twin edge connects the <paramref name="target"/>
    /// vertex back to the <paramref name="source"/> vertex.</returns>
    private (HalfEdge he, HalfEdge tw) AddEdge(
        Vertex source, Vertex target,
        IReadOnlySet<Guid>? heRefIds,
        IReadOnlySet<Guid>? twRefIds)
    {
        var he = new HalfEdge();
        var tw = he.Twin;
        HalfEdges.Add(he);
        HalfEdges.Add(tw);
        he.RefVertex = source;
        tw.RefVertex = target;
        if (heRefIds is not null)
        {
            AddSemantics(he, heRefIds);
        }
        if (twRefIds is not null)
        {
            AddSemantics(tw, twRefIds);
        }
        return (he, tw);
    }

    /// <summary>
    /// Adds a new vertex to the collection and optionally associates it with reference IDs and a specific point.
    /// </summary>
    /// <param name="refIds">An optional set of reference IDs to associate with the vertex. If provided, the vertex will be linked to
    /// these IDs.</param>
    /// <returns>The newly created vertex.</returns>
    private Vertex AddVertex(IReadOnlySet<Guid>? refIds = null)
    {
        var vertex = new HalfEdge().RefVertex;
        Vertices.Add(vertex);
        if (refIds is not null)
        {
            AddSemantics(vertex, refIds);
        }
        return vertex;
    }

    /// <summary>
    /// Creates a new <see cref="Facet"/> instance using the specified reference half-edge and adds it to the
    /// collection of facets.
    /// </summary>
    /// <remarks>If <paramref name="refIds"/> is not null, the method associates the provided semantic
    /// identifiers with the created facet.</remarks>
    /// <param name="refHalfEdge">The reference <see cref="HalfEdge"/> that defines the geometry of the new facet. Cannot be null.</param>
    /// <param name="refIds">An optional set of semantic identifiers associated with the facet. If provided, these identifiers will be
    /// added to the facet's semantics.</param>
    /// <returns>The newly created <see cref="Facet"/> instance.</returns>
    private Facet AddFacet(HalfEdge refHalfEdge, IReadOnlySet<Guid>? refIds)
    {
        var facet = new Facet(refHalfEdge);
        Facets.Add(facet);
        if (refIds is not null)
        {
            AddSemantics(facet, refIds);
        }
        return facet;
    }

    #endregion

    #region Triangulation

    /// <summary>
    /// Creates a new triangular facet in the partition and links the specified half-edges to form a closed loop.
    /// </summary>
    /// <remarks>This method establishes a closed loop by linking the provided half-edges in the
    /// order: <paramref name="ab"/> → <paramref name="bc"/> → <paramref name="ca"/> → <paramref name="ab"/>.
    /// Additionally, the facet reference is assigned to the second and third half-edges.</remarks>
    /// <param name="ab">The first half-edge of the triangle.</param>
    /// <param name="bc">The second half-edge of the triangle.</param>
    /// <param name="ca">The third half-edge of the triangle.</param>
    /// <returns>The newly created <see cref="Facet"/> representing the triangle.</returns>
    private Facet NewTriangle(HalfEdge ab, HalfEdge bc, HalfEdge ca)
    {
        var abc = AddFacet(ab, null);
        ab.Next = bc;
        bc.Next = ca;
        ca.Next = ab;
        bc._refFacet = abc;
        ca._refFacet = abc;
        return abc;
    }

    /// <summary>
    /// Creates a new triangle in the partition using the specified half-edges.
    /// </summary>
    /// <remarks>This method establishes the connectivity between the provided half-edges and the
    /// newly created edges, forming a complete triangle in the partition.</remarks>
    /// <param name="ab">The half-edge representing the edge from vertex A to vertex B.</param>
    /// <param name="bc">The half-edge representing the edge from vertex B to vertex C.</param>
    /// <returns>A tuple containing the following: <list type="bullet"> <item> <description>The newly created <see
    /// cref="Facet"/> representing the triangle.</description> </item> <item> <description>The half-edge from
    /// vertex C to vertex A.</description> </item> <item> <description>The half-edge from vertex A to vertex
    /// C.</description> </item> </list></returns>
    private (Facet abc, HalfEdge ca, HalfEdge ac) NewTriangle(HalfEdge ab, HalfEdge bc)
    {
        var a = ab._refVertex;
        var c = bc.Twin._refVertex;
        var (ca, ac) = AddEdge(c, a, null, null);
        var abc = NewTriangle(ab, bc, ca);
        return (abc, ca, ac);
    }

    /// <summary>
    /// Determines the relative position of a point with respect to a directed line segment.
    /// </summary>
    /// <param name="point">The point to evaluate, represented as a <see cref="VecI"/>.</param>
    /// <param name="side">The directed line segment, represented by a <see cref="HalfEdge"/>.</param>
    /// <returns>An integer indicating the relative position of the point: <list type="bullet"> <item><description>A positive
    /// value if the point is to the left of the line segment.</description></item> <item><description>A negative
    /// value if the point is to the right of the line segment.</description></item> <item><description>Zero if the
    /// point lies on the line segment.</description></item> </list></returns>
    private static int sideSign(in VecI point, HalfEdge side) => point.SideSign(side.RefVertex.Point!.Value, side.Twin.RefVertex.Point!.Value);

    /// <summary>
    /// Adds external triangles to the partition by connecting a specified vertex to a sequence of edges.
    /// </summary>
    /// <remarks>This method creates new external triangles by iteratively connecting the specified
    /// vertex to the edges between <paramref name="firstHe"/> and <paramref name="lastHe"/>. The resulting
    /// halfedges are associated with the exterior facet of the partition. The method ensures that the connectivity of
    /// the half-edges is updated appropriately.</remarks>
    /// <param name="v">The vertex to connect to the sequence of edges.</param>
    /// <param name="firstHe">The first <see cref="HalfEdge"/> in the sequence of edges to process.</param>
    /// <param name="lastHe">The last <see cref="HalfEdge"/> in the sequence of edges to process.</param>
    /// <returns>A <see cref="HashSet{T}"/> containing the <see cref="HalfEdge"/> instances affected by the operation.</returns>
    private HashSet<HalfEdge> AddExternalTriangles(Vertex v, HalfEdge firstHe, HalfEdge lastHe)
    {
        var oi = firstHe;
        var vi = oi._refVertex;
        var (hi, hiT) = AddEdge(v, vi, null, null);

        var oiPrev = oi.Prev;
        hiT.Prev = oiPrev;
        hiT._refFacet = Exterior;

        HashSet<HalfEdge> affected = [hi, oi, oiPrev];
        do
        { // create new abc
            var oiSucc = oi.Next;
            (_, _, hi) = NewTriangle(hi, oi);
            oi = oiSucc;
            affected.UnionWith([oi, hi]);
        } while (oi != lastHe);
        lastHe.Prev = hi;
        hi.Prev = hiT;
        hi.RefFacet = Exterior;
        return affected;
    }

    /// <summary>
    /// Splits an edge of a triangle by inserting a new vertex and creating two new half-edges.
    /// </summary>
    /// <remarks>This method modifies the topology of the triangle by splitting the specified edge and
    /// updating the connectivity of the surrounding edges and vertices. The new vertex is inserted along the edge,
    /// and the resulting half-edges are linked to maintain the integrity of the partition.</remarks>
    /// <param name="ab">The half-edge to be split, directed from vertex A to vertex B.</param>
    /// <param name="v">The new vertex to be inserted along the edge.</param>
    /// <returns>A tuple containing the two new half-edges: <list type="bullet"> <item><description>The half-edge directed
    /// from the new vertex to vertex B.</description></item> <item><description>The twin half-edge directed from
    /// vertex B to the new vertex.</description></item> </list></returns>
    private (HalfEdge he, HalfEdge tw) TriangleEdgeSplit(HalfEdge ab, Vertex v)
    {
        var ba = ab.Twin;
        var b = ab.Twin._refVertex;
        var (vb, bv) = AddEdge(v, b, null, null);

        var abNext = ab.Next;
        var baPrev = ba.Prev;
        // Behind und InFront nicht setzen, da flip möglich
        //var abBehind = ab.Behind;
        //var baInFront = ba.InFront;

        var av = ab;
        var va = ba;

        av.Next = vb;
        //av.Behind = vb;
        va.Prev = bv;
        //va.InFront = bv;
        va.RefVertex = v;

        if (abNext != ba)
        {
            vb.Next = abNext;
            vb.Behind = abNext;
        }

        vb.RefFacet = av.RefFacet;
        if (baPrev != ab)
        {
            bv.Prev = baPrev;
            bv.InFront = baPrev;
        }

        bv.RefFacet = va.RefFacet;

        return (vb, bv);
    }

    /// <summary>
    /// Splits a triangle by adding a new vertex and updates the affected edges and facets.
    /// </summary>
    /// <remarks>This method modifies the provided partition by splitting the triangle associated with the
    /// given half-edge and adding a new vertex. It updates the connectivity of the edges and facets to reflect the
    /// split. If the triangle shares an edge with another triangle, the adjacent triangle is also
    /// updated.</remarks>
    /// <param name="ab">The half-edge representing one side of the triangle to be split.</param>
    /// <param name="v">The new vertex to be added to the triangle.</param>
    /// <returns>A <see cref="HashSet{T}"/> containing the half-edges affected by the split operation.</returns>
    private HashSet<HalfEdge> TriangleSplitSide(HalfEdge ab, Vertex v)
    {   // define/save old values
        var bc = ab.Next;
        var ca = ab.Prev;
        var c = ca._refVertex;

        // new sides to v
        var av = ab;

        var (vb, bv) = TriangleEdgeSplit(ab, v);
        var (vc, cv) = AddEdge(v, c, null, null);

        av.Next = vc;

        vc.Next = ca;
        vc.RefFacet = av._refFacet;

        _ = NewTriangle(vb, bc, cv);

        HashSet<HalfEdge> affected = [av, vb, bc, ca, vc];

        var va = av.Twin;
        if (va.RefFacet != Exterior)
        {
            var ad = va.Next;
            var db = ad.Next;
            var d = db._refVertex;
            var (vd, dv) = AddEdge(v, d, null, null);

            ad.Next = dv;
            va.Prev = dv;

            dv.RefFacet = va._refFacet;

            _ = NewTriangle(bv, vd, db);

            affected.UnionWith([ad, db, dv]);
        }

        return affected;
    }

    /// <summary>
    /// Splits a triangular facet into three smaller triangles by connecting a new vertex to each vertex of the
    /// original triangle.
    /// </summary>
    /// <remarks>This method modifies the provided partition by adding new edges and updating the
    /// connectivity of the original facet. The original triangle is split into three smaller triangles, each
    /// sharing the new vertex as a common point.</remarks>
    /// <param name="abc">The triangular facet to be split.</param>
    /// <param name="v">The new vertex to be connected to the vertices of the original triangle.</param>
    /// <returns>A <see cref="HashSet{T}"/> containing the half-edges created during the split operation.</returns>
    private HashSet<HalfEdge> TriangleSplit(Facet abc, Vertex v)
    {
        var ab = abc.RefHalfEdge;
        var bc = ab.Next;
        var ca = ab.Prev;
        var a = ab.RefVertex;
        var b = bc.RefVertex;
        var c = ca.RefVertex;

        var (va, av) = AddEdge(v, a, null, null);
        var (vb, bv) = AddEdge(v, b, null, null);
        var (vc, cv) = AddEdge(v, c, null, null);

        var abv = abc;
        ab.Next = bv;
        bv.Next = va;
        va.Next = ab;
        ab.RefFacet = abv;
        bv._refFacet = abv;
        va._refFacet = abv;

        _ = NewTriangle(bc, cv, vb); ;

        _ = NewTriangle(ca, av, vc);

        return [va, vb, vc, ab, bc, ca];
    }

    /// <summary>
    /// Reconfigures the connectivity of two adjacent triangles by flipping the shared edge.
    /// </summary>
    /// <remarks>This operation modifies the topology of the mesh by flipping the shared edge between
    /// two triangles, effectively swapping the diagonal of the quadrilateral formed by the four vertices (A, B, C,
    /// D). The method assumes that the input half-edge <paramref name="ab"/> is valid and part of a well-formed
    /// mesh.</remarks>
    /// <param name="ab">The half-edge representing the shared edge between two triangles, oriented from vertex A to vertex B.</param>
    /// <returns>An array of <see cref="HalfEdge"/> objects representing the updated half-edges of the two triangles after
    /// the flip. The array contains the following half-edges in order: the half-edge from A to D, the half-edge
    /// from D to B, the half-edge from B to C, and the half-edge from C to A.</returns>
    private static HalfEdge[] TriangleFlip(in HalfEdge ab)
    {
        var bc = ab.Next;
        var ca = bc.Next;

        var ba = ab.Twin;
        var ad = ba.Next;
        var db = ad.Next;

        var a = ab.RefVertex;
        var b = ba.RefVertex;
        var c = ca.RefVertex;
        var d = db.RefVertex;

        var dc = ab;
        var cd = ba;
        var dca = dc.RefFacet;
        var cdb = cd.RefFacet;

        dca._refHalfEdge = dc;
        dc.RefVertex = d;
        dc.Next = ca;
        ca.Next = ad;
        ad.Next = dc;
        ca._refFacet = dca;
        ad._refFacet = dca;

        cdb._refHalfEdge = cd;
        cd.RefVertex = c;
        cd.Next = db;
        db.Next = bc;
        bc.Next = cd;
        db._refFacet = cdb;
        bc._refFacet = cdb;

        a._refHalfEdge = ad;
        b._refHalfEdge = bc;

        return [ad, db, bc, ca];
    }

    /// <summary>
    /// Ensures that the triangulation of the given partition satisfies the Delaunay condition by flipping edges as
    /// necessary.
    /// </summary>
    /// <remarks>This method iteratively examines and flips edges in the triangulation to enforce the
    /// Delaunay condition,  which ensures that no point lies inside the circumcircle of any triangle. Edges
    /// connected to the exterior  of the partition are excluded from processing.</remarks>
    /// <param name="affected">A set of half-edges that are initially affected and may require processing.  This set is updated during the
    /// operation as additional edges are identified for flipping.</param>
    private void Delaunay(HashSet<HalfEdge> affected)
    {
        Queue<HalfEdge> queue = new(affected);
        while (queue.Count > 0)
        {
            var h = queue.Dequeue();
            if (h.RefFacet == Exterior || h.Twin.RefFacet == Exterior)
            {
                continue;
            }
            var a = h.RefVertex.Point!.Value;
            var b = h.Next.RefVertex.Point!.Value;
            var c = h.Prev.RefVertex.Point!.Value;
            var p = h.Twin.Next.Next.RefVertex.Point!.Value;
            if (p.DoFlip(in a, in b, in c))
            {
                foreach (var t in TriangleFlip(h))
                {
                    if (t.RefFacet != Exterior && t.Twin.RefFacet != Exterior
                        && !affected.Contains(t.Twin) && affected.Add(t))
                    {
                        queue.Enqueue(t);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Adds a new point to the partition, updating the triangulation and associated data structures as necessary.
    /// </summary>
    /// <remarks>This method updates the partition's triangulation to include the specified point.
    /// Depending on the point's position relative to the existing triangulation, it may create new triangles, split
    /// existing edges, or extend the partition's range. If the point already exists in the partition, its semantics are
    /// updated instead.</remarks>
    /// <param name="point">The point to add, represented as a <see cref="VecI"/> structure.</param>
    /// <param name="refIds">A set of reference IDs associated with the point, used to track semantics or metadata.</param>
    /// <returns>The newly created or updated <see cref="Vertex"/> instance representing the point.</returns>
    /// <exception cref="Exception"></exception>
    private Vertex AddPoint(in VecI point, IReadOnlySet<Guid> refIds)
    {
        if (PointVertices.TryGetValue(point, out var vertex))
        { // Point already inserted
            AddSemantics(vertex, refIds);
            return vertex;
        }

        // extend range
        Min = Min.Min(point);
        Max = Max.Max(point);

        // new vertex
        vertex = AddVertex(refIds);
        vertex.Point = point;
        PointVertices[point] = vertex;

        var exterior = Exterior;
        if (Facets.Count > 0)
        { // Triangulation has at least one triangle
          // start halfEdge
            var ab = HalfEdges[0];
            int abSign = sideSign(in point, ab);
            if (abSign < 0)
            {
                abSign = -abSign;
                ab = ab.Twin;
            }
            while (true)
            {
                if (abSign == 0 && ab.RefFacet == exterior)
                {
                    ab = ab.Twin;
                }
                if (ab.RefFacet == exterior)
                { // Point is outside the triangulation, create new triangles outside
                    var first = ab;
                    while (sideSign(in point, first.Prev) > 0)
                    { // search firstHe convex side
                        first = first.Prev;
                    }
                    var last = ab.NextNotBehind;
                    while (sideSign(in point, last) > 0)
                    { // search firstHe non convex side
                        last = last.NextNotBehind;
                    }
                    Delaunay(AddExternalTriangles(vertex, first, last));
                    break;
                }
                else
                {   // Point is inside the triangulation, create new triangles inside
                    var bc = ab.Next;
                    var ca = ab.Prev;
                    int bcSign = sideSign(in point, bc);
                    int caSign = sideSign(in point, ca);
                    int sum = abSign + bcSign + caSign;
                    if (sum > 1)
                    {
                        if (abSign == 0)
                        { // point is on oi
                            Delaunay(TriangleSplitSide(ab, vertex));
                        }
                        else if (bcSign == 0)
                        { // point is on bc
                            Delaunay(TriangleSplitSide(bc, vertex));
                        }
                        else if (caSign == 0)
                        { // point is on hi
                            Delaunay(TriangleSplitSide(ca, vertex));
                        }
                        else
                        { // point is inside abc
                            Delaunay(TriangleSplit(ab.RefFacet, vertex));
                        }
                        break;
                    }
                    else
                    { // Point on existing points is impossible, case ignored
                        (ab, abSign) = sum == 1
                            ? abSign < 0 ? (ab.Twin, -abSign) // behind ab
                            : bcSign < 0 ? (bc.Twin, -bcSign) // behind bc
                            : (ca.Twin, -caSign)              // behind ca
                        : // Random cases
                              abSign == 1 // behind c
                            ? RandomNext ? (bc.Twin, -bcSign) : (ca.Twin, -caSign)
                            : bcSign == 1 // behind a
                            ? RandomNext ? (ab.Twin, -abSign) : (ca.Twin, -caSign)
                            : RandomNext ? (ab.Twin, -abSign) : (bc.Twin, -bcSign);
                    }
                }
            }
        }
        else if (HalfEdges.Count > 0)
        {   // Triangulation has one side or multiple collinear sides
            // first HalfEdge with no predecessor
            var first = HalfEdges.First(h => h.Prev == h.Twin);
            // last HalfEdge with no successor
            var last = HalfEdges.Count == 2 ? first : HalfEdges
                    .First(h => h != first && h != first.Twin && h.Next == h.Twin);
            var firstP = first.RefVertex.Point!.Value;
            var lastP = last.Twin.RefVertex.Point!.Value;
            var diff = lastP - firstP;
            // compare axis with greatest difference to avoid comparisons with zero
            Func<VecI, VecI, bool> lesser = long.Abs(diff.X) > long.Abs(diff.Y)
                ? (VecI a, VecI b) => a.X < b.X
                : (VecI a, VecI b) => a.Y < b.Y;
            if (lesser(lastP, firstP))
            { // Reverse points
                (first, last, firstP, lastP) = (last.Twin, first.Twin, lastP, firstP);
            }
            int sideSign = point.SideSign(firstP, lastP);

            if (sideSign == 0)
            { // point is on the line, create only one new side

                if (lesser(point, firstP))
                { // point is left/below of the firstHe side
                    var (he, tw) = AddEdge(vertex, first.RefVertex, null, null);
                    he.Next = first;
                    he.RefFacet = exterior;
                    tw.Prev = first.Twin;
                    tw._refFacet = exterior;
                }
                else if (lesser(lastP, point))
                { // point is right/above of the lastHe side
                    var (he, tw) = AddEdge(last.Twin.RefVertex, vertex, null, null);
                    he.Prev = last;
                    he.RefFacet = exterior;
                    tw.Next = last.Twin;
                    tw._refFacet = exterior;
                }
                else
                { // point is on an existing side
                    var current = last;
                    while (true)
                    {
                        if (lesser(current.RefVertex.Point!.Value, point))
                        {
                            var (he, tw) = TriangleEdgeSplit(current, vertex);
                            break;
                        }
#if DEBUG
                        if (current == first)
                        {
                            throw new Exception("AddPoint: Error in algorithm.");
                        }
#endif
                        current = current.Prev;
                    }
                }
            }
            else
            { // build one or more new triangles
                last = last.Next; // Twin
                if (sideSign < 0)
                { // Reverse if point on right side
                    (first, last) = (last, first);
                }
                // delaunay not necessary, because of just three corner points, others are collinear
                _ = AddExternalTriangles(vertex, first, last);
            }
        }
        else if (PointVertices.Count > 1) // Attention: PointVertices.Count is +1, because point is inserted earlier
        { // Triangulation has only one point
            // add new edge between the first point and the new point
            var other = Vertices[0];
            var (he, tw) = AddEdge(other, vertex, null, null);
            he.RefFacet = exterior;
            tw._refFacet = exterior;
        }
        return vertex;
    }

    // TODO CTeste auf geschlossenen Ring schließe automatisch! baue alles auf Polygone um
    private Feature AddMultiPolygon(List<(Vertex source, Vertex target)> edges, (double x, double y)[][][] multiGeometry)
    {
        var multiId = Guid.NewGuid();
        var subIds = new Feature[multiGeometry.Length];
        for (int i = 0; i < multiGeometry.Length; i++)
        {
            var subIdi = Guid.NewGuid();
            var geometry = multiGeometry[i];
            var subsubIds = new Feature[geometry.Length];
            var refIdsi = ImmutableHashSet.Create(multiId, subIdi);
            for (int j = 0; j < geometry.Length; j++)
            {
                subsubIds[j] = new Feature(Guid.NewGuid(), []);
                var refIds = refIdsi.Add(subsubIds[j].Id);
                var region = geometry[j];
                Vertex? last = null;
                for (int k = 0; k < region.Length; k++)
                {
                    var (x, y) = region[k];
                    var point = Epsilon.Convert(x, y);
                    var curr = AddPoint(in point, refIds);
                    if (last is not null)
                        edges.Add((last, curr));
                    last = curr;
                }
            }
            subIds[i] = new Feature(subIdi, subsubIds);
        }
        return new Feature(multiId, subIds);
    }


    #endregion

    #region Reconstruction

    /// <summary>
    /// Splits the specified half-edge at a given position, creating two new half-edges and updating the mesh
    /// structure accordingly.
    /// </summary>
    /// <remarks>This method modifies the mesh structure by inserting a new vertex and updating the
    /// connectivity of the affected half-edges. The input half-edge <paramref name="ab"/> and its twin are updated
    /// to reflect the split, and the new half-edges are linked into the mesh. The caller is responsible for
    /// ensuring that the input parameters are valid and consistent with the mesh state.</remarks>
    /// <param name="ab">The half-edge to be split. This half-edge represents the edge from one vertex to another in the mesh.</param>
    /// <param name="position">The fractional position along the edge where the split occurs. Must be within the range [0, 1].</param>
    /// <param name="v">The new vertex to be inserted at the split position.</param>
    /// <returns>A tuple containing the two new half-edges created by the split: <list type="bullet">
    /// <item><description><c>vb</c>: The half-edge from the new vertex to the original destination vertex of
    /// <paramref name="ab"/>.</description></item> <item><description><c>bv</c>: The twin half-edge from the
    /// original destination vertex back to the new vertex.</description></item> </list></returns>
    private (HalfEdge vb, HalfEdge bv) SplitHalfEdgeAt(HalfEdge ab, in Fraction position, Vertex v)
    {
        var ba = ab.Twin;
        var abNext = ab.Next;
        var baPrev = ba.Prev;
        var baPos = ba.Position;
        var abBehind = ab.Behind;
        var baInFront = ba.InFront;

        var (vb, bv) = AddEdge(v, ba.RefVertex, ab.RefIds, ba.RefIds);

        var av = ab;
        var va = ba;

        av.Next = vb;
        av.Behind = vb;

        va.Prev = bv;
        va.InFront = bv;
        va.Position = position.Reverse();
        va._refVertex = v;

        vb.Next = abNext;
        vb.RefFacet = av.RefFacet;
        vb.Behind = abBehind;
        vb.Position = position;

        bv.Prev = baPrev;
        bv.RefFacet = va.RefFacet;
        bv.InFront = baInFront;
        bv.Position = baPos;

        return (vb, bv);
    }

    /// <summary>
    /// Updates the reference to the current <see cref="HalfEdge"/> to the one behind it, if available.
    /// </summary>
    /// <param name="h">A reference to the current <see cref="HalfEdge"/>. This will be updated to the <see cref="HalfEdge"/> behind
    /// it if one exists.</param>
    /// <returns><see langword="true"/> if the reference was successfully updated to the <see cref="HalfEdge"/> behind the
    /// current one; otherwise, <see langword="false"/> if there is no <see cref="HalfEdge"/> behind the current
    /// one.</returns>
    private static bool nextOnEdge(ref HalfEdge h)
    {
        if (h.Behind is not HalfEdge hBehind)
        {
            return false;
        }

        h = hBehind;
        return true;
    }


    /// <summary>
    /// Determines whether the specified edge intersects or ends on another edge in the half-edge structure.
    /// </summary>
    /// <param name="h">The starting half-edge to begin the traversal.</param>
    /// <param name="edgeSource">The source vertex of the edge being tested.</param>
    /// <param name="edgeTarget">The target vertex of the edge being tested.</param>
    /// <param name="transPos">When this method returns, contains the fractional position along the intersected edge where the intersection
    /// occurs, if an intersection is found. This parameter is passed uninitialized.</param>
    /// <param name="trans">When this method returns, contains the half-edge where the intersection or endpoint match occurs. This
    /// parameter is passed uninitialized.</param>
    /// <returns><see langword="true"/> if the specified edge intersects or ends on another edge; otherwise, <see
    /// langword="false"/>.</returns>
    /// <exception cref="Exception">Thrown in debug mode if the traversal loops back to the starting half-edge without finding an intersection
    /// or endpoint match.</exception>
    private static bool intersectOrEndsOn(HalfEdge h,
        in VecI edgeSource, in VecI edgeTarget,
        out Fraction transPos, out HalfEdge trans)
    {
        trans = h.Next;
        var pos1 = trans.Position;
        var pos2 = trans.Twin.Position.Reverse();
        var (otherSrc, otherTgt) = trans.Edge;
        while (!edgeSource.IntersectsOrTouchesFromRight(in edgeTarget,
            in otherSrc, in otherTgt,
            in pos1, in pos2, out transPos))
        {
            trans = trans.Next;
            pos1 = trans.Position;
            pos2 = trans.Twin.Position.Reverse();
#if DEBUG
            if (trans == h)
            {
                throw new Exception("intersectOrEndsOn: Unexpected error.");
            }
#endif
        }
        return trans.Twin.Position.Reverse() == transPos;
    }


    /// <summary>
    /// Reconstructs an edge between the specified source and target vertices, ensuring that all necessary
    /// connections and references are established or updated.
    /// </summary>
    /// <remarks>This method ensures that the edge between the source and target vertices is properly
    /// reconstructed,  handling cases where the edge already exists, intersects with other edges, or requires new
    /// vertices  or edges to be created. The method also associates the provided reference identifiers with the
    /// edge  and updates any affected facets. <para> The method assumes that the source and target vertices are
    /// part of the same graph structure and that  their associated points are valid. If the source and target
    /// vertices are the same, the method returns  <see langword="true"/> without making any changes. </para> <para>
    /// This method may throw exceptions if unexpected conditions are encountered during the reconstruction 
    /// process, such as algorithmic inconsistencies. </para></remarks>
    /// <param name="refIds">A set of reference identifiers to associate with the reconstructed edge.</param>
    /// <param name="bothSides">A value indicating whether the reference identifiers should also be associated with the twin halfedges  in
    /// the opposite direction.</param>
    /// <param name="source">The starting vertex of the edge to reconstruct.</param>
    /// <param name="target">The ending vertex of the edge to reconstruct.</param>
    /// <returns><see langword="true"/> if the edge was successfully reconstructed or already exists; otherwise,  <see
    /// langword="false"/> if the operation could not be completed due to invalid input or state.</returns>
    /// <exception cref="Exception">Thrown if unexpected conditions are encountered during the reconstruction process, such as algorithmic 
    /// inconsistencies or invalid graph states.</exception>
    internal bool ReconstructEdge(in IReadOnlySet<Guid> refIds, bool bothSides, Vertex source, Vertex target)
    {
        if (source == target)
        {
            //Logger?.LogWarning($"ReconstructFeatureEdge: source and target are equal ({sourceId}).");
            return true;
        }
        if (source.Point is not VecI sourcePoint
            || target.Point is not VecI targetPoint)
        {
            //Logger?.LogError("ReconstructFeatureEdge: source or target vertex has no point");
            return false;
        }

        // Check if edge already exists
        var chain = source.GetHalfEdgeChainTo(target);
        if (chain.Count > 0)
        {
            foreach (var he in chain)
            {
                AddSemantics(he, refIds);
                if (bothSides)
                {
                    AddSemantics(he.Twin, refIds);
                }
            }
            return true;
        }

        var current = source;
        // Start search
        while (true)
        {
            // Target found
            if (current == target)
            {
                break;
            }

            // Search halfedge right or on edge
            if (current.RightHalfEdge(in sourcePoint, in targetPoint, out var right))
            {
                // Edge allready exists
                // Move to lastHe HalfEdge on edge, and add all RefIds to the halfEdges
                do
                {
                    chain.Add(right);
                    AddSemantics(right, refIds);
                    if (bothSides)
                    {
                        AddSemantics(right.Twin, refIds);
                    }
                } while (nextOnEdge(ref right));
                current = right.Twin.RefVertex; // new Start
                continue;
            }

            // New edge necessary, search intersections until known point
            // List off crossing halfedges, firstHe is right of edge source other is lft of edge target
            HasReconstruction = true;
            var transverses = new List<(HalfEdge right, HalfEdge left)>();
            var currentPoint = current.Point!.Value;
            Vertex nextCurrent;
            VecI nextCurrentPoint;
            while (true)
            {
                // Calculate intersection (intersects or touches allways)
                // calculate position on edge later, because target point could change!
                if (intersectOrEndsOn(right, in sourcePoint, in targetPoint,
                    out var transPos, out var trans))
                {
                    // touches
                    transverses.Add((right, trans.Next));
                    if (transPos.IsOne) // real point, (new or defined edge target) 
                    {
                        nextCurrent = trans.Twin.RefVertex;
                        nextCurrentPoint = nextCurrent.Point!.Value;
                        break;
                    }
                    else if (!trans.Twin.RefVertex.RightHalfEdge(in sourcePoint, in targetPoint, out right))
                    {
                        // existing intersection, search new right edge
                        throw new Exception("Error in algorithm.");
                    }
                }
                else
                {
                    // Halbkante teilen
                    var v = AddVertex(null);
                    var (ntrans, _) = SplitHalfEdgeAt(trans, in transPos, v);
                    transverses.Add((right, ntrans));
                    right = trans.Twin;
                }

            }
            // neue Kante erstellen
            var twinRefIds = bothSides ? refIds : null;
            var (he, tw) = AddEdge(current, nextCurrent, refIds, twinRefIds);
            chain.Add(he);
            foreach (var (rgt, lft) in transverses)
            {
                // Teilen?
                var heNext = he;
                var twNext = tw;
                if (lft.RefVertex != tw.RefVertex)
                {
                    var (otherSource, otherTarget) = lft.Edge;
                    (heNext, twNext) = SplitHalfEdgeAt(he,
                        currentPoint.IntersectEdge(in nextCurrentPoint, in otherSource, in otherTarget),
                        lft.RefVertex);
                    chain.Add(heNext);
                }
                // Verknüpfen
                he.Prev = rgt.Prev;
                tw.Prev = lft.Prev;
                he.Next = lft;
                tw.Next = rgt;

                // Flächen
                var heFacet = AddFacet(he, rgt.RefFacet.RefIds);
                var curr = he.Next;
                do
                {
                    curr._refFacet = heFacet;
                    curr = curr.Next;
                } while (curr != he);
                rgt._refFacet.RefHalfEdge = tw;
                he = heNext;
                tw = twNext;
            }
            current = nextCurrent;
            continue;
        }
        // set behind and inFront of the new halfedges to speedup the next searches
        for (int i = 1; i < chain.Count; i++)
        {
            var infront = chain[i - 1];
            var behind = chain[i];
            infront.Behind = behind;
            behind.Twin.Behind = infront.Twin;
        }
        return true;
    }

    #endregion

    #region Requests

    /// <summary>
    /// Retrieves a set of <see cref="HalfEdge"/> instances associated with the specified reference ID.
    /// </summary>
    /// <remarks>This method filters elements associated with the given reference ID to include only those of
    /// type <see cref="HalfEdge"/>.</remarks>
    /// <param name="refId">The reference ID used to locate the associated <see cref="HalfEdge"/> instances.</param>
    /// <returns>A <see cref="HashSet{T}"/> containing the <see cref="HalfEdge"/> instances associated with the specified
    /// reference ID. Returns an empty set if no matching elements are found or if the reference ID is not present.</returns>
    public HashSet<HalfEdge> GetHalfEdgesWithRefId(in Guid refId)
    {
        if (!SemanticHalfEdges.TryGetValue(refId, out var elements) || elements.Count == 0)
            return [];

        var set = new HashSet<HalfEdge>();
        foreach (var e in elements)
        {
            if (e is HalfEdge he)
                set.Add(he);
        }
        return set;
    }

    /// <summary>
    /// Retrieves a set of <see cref="Vertex"/> instances associated with the specified reference ID.
    /// </summary>
    /// <remarks>This method filters elements associated with the given reference ID to include only those of
    /// type <see cref="Vertex"/>.</remarks>
    /// <param name="refId">The reference ID used to locate the associated <see cref="Vertex"/> instances.</param>
    /// <returns>A <see cref="HashSet{T}"/> containing the <see cref="Vertex"/> instances associated with the specified
    /// reference ID. Returns an empty set if no matching elements are found or if the reference ID is not present.</returns>
    public HashSet<Vertex> GetVerticesWithRefId(in Guid refId)
    {
        if (!SemanticVertices.TryGetValue(refId, out var elements) || elements.Count == 0)
            return [];

        var set = new HashSet<Vertex>();
        foreach (var e in elements)
        {
            if (e is Vertex v)
                set.Add(v);
        }
        return set;
    }


    /// <summary>
    /// Retrieves a set of <see cref="Facet"/> instances associated with the specified reference ID.
    /// </summary>
    /// <remarks>This method filters elements associated with the given reference ID to include only those of
    /// type <see cref="Facet"/>.</remarks>
    /// <param name="refId">The reference ID used to locate the associated <see cref="Facet"/> instances.</param>
    /// <returns>A <see cref="HashSet{T}"/> containing the <see cref="Facet"/> instances associated with the specified
    /// reference ID. Returns an empty set if no matching elements are found or if the reference ID is not present.</returns>
    public HashSet<Facet> GetFacetsWithRefId(in Guid refId)
    {
        if (!SemanticFacets.TryGetValue(refId, out var elements) || elements.Count == 0)
            return [];

        var set = new HashSet<Facet>();
        foreach (var e in elements)
        {
            if (e is Facet f)
                set.Add(f);
        }
        return set;
    }

    public HashSet<HalfEdge> GetHalfEdgesWithRefIds(IReadOnlyCollection<Guid> refIds)
    {
        var result = new HashSet<HalfEdge>();
        foreach (var refId in refIds)
        {
            if (SemanticHalfEdges.TryGetValue(refId, out var elements))
                result.UnionWith(elements);
        }
        return result;
    }

    public HashSet<Vertex> GetVerticesWithRefIds(IReadOnlyCollection<Guid> refIds)
    {
        var result = new HashSet<Vertex>();
        foreach (var refId in refIds)
        {
            if (SemanticVertices.TryGetValue(refId, out var elements))
                result.UnionWith(elements);
        }
        return result;
    }

    public HashSet<Facet> GetFacetsWithRefIds(IReadOnlyCollection<Guid> refIds)
    {
        var result = new HashSet<Facet>();
        foreach (var refId in refIds)
        {
            if (SemanticFacets.TryGetValue(refId, out var elements))
                result.UnionWith(elements);
        }
        return result;
    }

    #endregion

    #region Topology Methods

    private static List<HashSet<Facet>> EnclosedFacetRegions(in HashSet<HalfEdge> halfEdges)
    {
        var visitedFacets = new HashSet<Facet>();
        var regions = new List<HashSet<Facet>>();

        foreach (var he in halfEdges)
        {
            var startFacet = he.RefFacet;
            if (startFacet.Id == ExteriorId)
                continue;
            if (visitedFacets.Contains(startFacet))
                continue;

            var region = new HashSet<Facet> { startFacet };
            var queue = new Queue<Facet>();
            queue.Enqueue(startFacet);
            bool isOpen = false;

            while (queue.Count > 0 && !isOpen)
            {
                var facet = queue.Dequeue();
                foreach (var edge in facet.Boundary())
                {
                    if (halfEdges.Contains(edge))
                        continue;

                    var neighbor = edge.Twin.RefFacet;
                    if (neighbor.Id == ExteriorId)
                    {
                        isOpen = true;
                        break;
                    }
                    if (region.Add(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            // Markiere alle Facetten der Region als besucht, damit sie nicht erneut geprüft werden
            visitedFacets.UnionWith(region);

            if (!isOpen)
                regions.Add(region);
        }
        return regions;
    }

    private static bool IsClosedInterior(in HashSet<HalfEdge> boundary, bool halfEdgesInside = true)
    {
        if (boundary.Count < 3)
            return false;

        // Set für bereits besuchte Facetten
        var visitedFacets = new HashSet<Facet>();
        // Queue für BFS
        var queue = new Queue<Facet>();

        // Initialisiere mit allen Facetten, die an der Boundary liegen
        foreach (var he in boundary)
        {
            var facet = halfEdgesInside ? he.RefFacet : he.Twin.RefFacet;
            if (visitedFacets.Add(facet))
                queue.Enqueue(facet);
        }

        while (queue.Count > 0)
        {
            var facet = queue.Dequeue();
            foreach (var he in facet.Boundary())
            {
                // Prüfe, ob diese Kante zur Boundary gehört (je nach Orientierung)
                bool isBoundary = halfEdgesInside ? boundary.Contains(he) : boundary.Contains(he.Twin);
                if (isBoundary)
                    continue;

                // Wenn die Nachbar-Facette das Exterior ist, ist der Bereich offen
                if (he.Twin.RefFacet.Id == ExteriorId)
                    return false;

                // Wenn die Nachbar-Facette noch nicht besucht wurde, weiter traversieren
                if (visitedFacets.Add(he.Twin.RefFacet))
                    queue.Enqueue(he.Twin.RefFacet);
            }
        }

        // Wenn alle erreichbaren Facetten von der Boundary eingeschlossen sind und kein Exterior erreicht wurde, ist der Bereich geschlossen
        return true;
    }

    private static List<List<HalfEdge>> RegionBoundary(in HashSet<Facet> region)
    {
        // 1. Finde alle Begrenzungskanten (nur die, die nicht innerhalb der Region liegen)
        var boundarySet = new HashSet<HalfEdge>();
        foreach (var f in region)
        {
            foreach (var he in f.Boundary())
            {
                if (!boundarySet.Remove(he.Twin))
                    boundarySet.Add(he);
            }
        }

        // 2. Baue ein Dictionary für schnellen Zugriff: Startpunkt -> HalfEdge
        var startDict = boundarySet.ToDictionary(he => he.RefVertex);

        // 3. Finde und sortiere alle Ringe
        var boundaryList = new List<List<HalfEdge>>();
        var used = new HashSet<HalfEdge>();
        while (used.Count < boundarySet.Count)
        {
            // Finde eine noch nicht verwendete Kante als Start
            var start = boundarySet.First(he => !used.Contains(he));
            var ring = new List<HalfEdge>();
            var current = start;
            do
            {
                ring.Add(current);
                used.Add(current);
                // Nächste Kante suchen: Startpunkt = Endpunkt der aktuellen Kante
                var nextVertex = current.Twin.RefVertex;
                if (!startDict.TryGetValue(nextVertex, out var next))
                    throw new ArithmeticException("RegionBoundary: Fehler beim Finden des nächsten Ringschritts.");
                current = next;
            } while (current != start);

            if (ring.Count > 2)
                boundaryList.Add(ring);
            else
                throw new ArithmeticException("RegionBoundary: Ring zu kurz oder nicht geschlossen.");
        }

        // 4. Äußeren Umring an den Anfang
        for (int i = 0; i < boundaryList.Count; i++)
        {
            var ringSet = new HashSet<HalfEdge>(boundaryList[i]);
            if (IsClosedInterior(ringSet))
            {
                if (i > 0)
                    (boundaryList[0], boundaryList[i]) = (boundaryList[i], boundaryList[0]);
                break;
            }
        }
        return boundaryList;
    }

    private static List<HashSet<Facet>> GetRegions(in HashSet<Facet> facets)
    {
        var regions = new List<HashSet<Facet>>();
        var visited = new HashSet<Facet>();

        foreach (var start in facets)
        {
            if (visited.Contains(start))
                continue;

            var region = new HashSet<Facet>();
            var queue = new Queue<Facet>();
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var facet = queue.Dequeue();
                region.Add(facet);

                foreach (var he in facet.Boundary())
                {
                    var neighbor = he.Twin.RefFacet;
                    if (facets.Contains(neighbor) && !visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
            if (region.Count > 0)
                regions.Add(region);
        }
        return regions;
    }

    private static bool PointsOfHalfEdgeChain(Epsilon epsilon, List<HalfEdge> halfEdges, out (double x, double y)[] points)
    {
        points = [];
        int n = halfEdges.Count;
        if (n == 0)
            return false;

        bool isClosed = halfEdges[0].RefVertex == halfEdges[^1].Twin.RefVertex;
        var pointsList = new List<(double x, double y)>(n);
        int startIdx = -1;

        // Hilfsfunktion für Punktberechnung
        static (double x, double y) GetPoint(Epsilon eps, HalfEdge he)
        {
            if (!eps.Convert(he.RefVertex, out var p))
                Console.WriteLine($"PointsOfHalfEdgeChain: Point {p} is rounded.");
            return p;
        }

        // Hilfsfunktion für Kollinearität
        static bool IsCollinear(HalfEdge he, HalfEdge prev)
        {
            if (he.RefVertex.Point is VecI c && c.SideSign(prev.EdgeSource, he.EdgeTarget) == 0)
            {
                he.InFront = prev;
                return true;
            }
            return false;
        }

        if (!isClosed)
        {
            pointsList.Add(GetPoint(epsilon, halfEdges[0]));
            startIdx = 0;
        }

        for (int i = 1; i < n; i++)
        {
            var he = halfEdges[i];
            if ((he.InFront is null && IsCollinear(he, halfEdges[i - 1])) ||
                (he.InFront == halfEdges[i - 1]))
                continue;

            if (isClosed && he.RefVertex.Point is not null)
                startIdx = pointsList.Count;
            pointsList.Add(GetPoint(epsilon, he));
        }

        if (isClosed)
        {
            if (halfEdges[0].InFront is null && !IsCollinear(halfEdges[0], halfEdges[^1]))
            {
                if (halfEdges[0].RefVertex.Point is not null)
                    startIdx = pointsList.Count;
                pointsList.Add(GetPoint(epsilon, halfEdges[0]));
            }
            if (pointsList.Count < 3)
                return false;
        }
        else
        {
            pointsList.Add(GetPoint(epsilon, halfEdges[^1].Twin));
        }

        // Punkte ggf. rotieren
        int count = pointsList.Count;
        points = new (double x, double y)[count + (isClosed ? 1 : 0)];
        if (startIdx > 0)
        {
            // Block 1: Von startIdx bis Ende
            pointsList.CopyTo(startIdx, points, 0, count - startIdx);
            // Block 2: Von Anfang bis startIdx-1
            pointsList.CopyTo(0, points, count - startIdx, startIdx);
        }
        else
            pointsList.CopyTo(points, 0);

        if (isClosed)
            points[^1] = points[0];

        return true;
    }

    private static List<HashSet<Facet>> MakeValidRegions(List<HashSet<Facet>> regions)
    {
        // 1. Echte Teilmengen entfernen
        var filtered = new List<HashSet<Facet>>(regions.Count);
        var holes = new HashSet<Facet>();
        for (int i = 0; i < regions.Count; i++)
        {
            bool isSubset = false;
            for (int j = 0; j < regions.Count; j++)
            {
                if (i == j) continue;
                if (regions[j].IsSupersetOf(regions[i]) && regions[j].Count > regions[i].Count)
                {
                    isSubset = true;
                    holes.UnionWith(regions[i]);
                    break;
                }
            }
            if (!isSubset)
                filtered.Add(regions[i]);
        }
        // Entferne die Löcher aus den Regionen
        for (int i = 0; i < filtered.Count; i++)
        {
            filtered[i].ExceptWith(holes);
        }

        // 2. Überlappende Regionen vereinigen (Union-Find-Algorithmus)
        var result = new List<HashSet<Facet>>();
        bool[] visited = new bool[filtered.Count];

        for (int i = 0; i < filtered.Count; i++)
        {
            if (visited[i]) continue;
            var union = new HashSet<Facet>(filtered[i]);
            visited[i] = true;
            bool merged;
            do
            {
                merged = false;
                for (int j = 0; j < filtered.Count; j++)
                {
                    if (visited[j]) continue;
                    if (union.Overlaps(filtered[j]))
                    {
                        union.UnionWith(filtered[j]);
                        visited[j] = true;
                        merged = true;
                    }
                }
            } while (merged);
            result.Add(union);
        }

        return result;
    }


    public IEnumerable<string> IsValid()
    {
        var errors = new List<string>();

        // 1. Vertex.RefHalfEdge gesetzt und RefHalfEdge.RefVertex == Ausgangsvertex
        foreach (var v in Vertices)
        {
            if (v.RefHalfEdge == null)
            {
                errors.Add($"Vertex {v.Id}: RefHalfEdge is null.");
                continue;
            }
            if (v.RefHalfEdge.RefVertex != v)
            {
                errors.Add($"Vertex {v.Id}: RefHalfEdge.RefVertex != Vertex.");
            }
        }

        // 2. Facet.RefHalfEdge gesetzt und RefHalfEdge.RefFacet == Ausgangsfacet
        foreach (var f in Facets)
        {
            if (f.RefHalfEdge == null)
            {
                errors.Add($"Facet {f.Id}: RefHalfEdge is null.");
                continue;
            }
            if (f.RefHalfEdge.RefFacet != f)
            {
                errors.Add($"Facet {f.Id}: RefHalfEdge.RefFacet != Facet.");
            }
        }

        // 3. Für alle Facet.Boundary(): he.RefFacet == Ausgangsfacet
        foreach (var f in Facets)
        {
            foreach (var he in f.Boundary())
            {
                if (he.RefFacet != f)
                {
                    errors.Add($"Facet {f.Id}: Boundary HalfEdge {he.Id} RefFacet != Facet.");
                }
            }
        }

        // 4. HalfEdge-Referenzen: Twin, Next/Prev, InFront/Behind
        foreach (var he in HalfEdges)
        {
            // Twin
            if (he.Twin == null)
            {
                errors.Add($"HalfEdge {he.Id}: Twin is null.");
            }
            else if (he.Twin.Twin != he)
            {
                errors.Add($"HalfEdge {he.Id}: Twin.Twin != this.");
            }

            // Next/Prev
            if (he.Next == null)
            {
                errors.Add($"HalfEdge {he.Id}: Next is null.");
            }
            else if (he.Next.Prev != he)
            {
                errors.Add($"HalfEdge {he.Id}: Next.Prev != this.");
            }
            if (he.Prev == null)
            {
                errors.Add($"HalfEdge {he.Id}: Prev is null.");
            }
            else if (he.Prev.Next != he)
            {
                errors.Add($"HalfEdge {he.Id}: Prev.Next != this.");
            }

            // InFront/Behind (optional)
            if (he.InFront != null && he.InFront.Behind != he)
            {
                errors.Add($"HalfEdge {he.Id}: InFront.Behind != this.");
            }
            if (he.Behind != null && he.Behind.InFront != he)
            {
                errors.Add($"HalfEdge {he.Id}: Behind.InFront != this.");
            }

            // RefFacet/RefVertex referenziert
            if (he.RefFacet == null)
            {
                errors.Add($"HalfEdge {he.Id}: RefFacet is null.");
            }
            if (he.RefVertex == null)
            {
                errors.Add($"HalfEdge {he.Id}: RefVertex is null.");
            }
        }

        // 5. Optional: Prüfe auf doppelte IDs (kann auf Fehler im UniqueCounter hindeuten)
        if (Vertices.Select(v => v.Id).Distinct().Count() != Vertices.Count)
            errors.Add("Duplicate Vertex IDs found.");
        if (Facets.Select(f => f.Id).Distinct().Count() != Facets.Count)
            errors.Add("Duplicate Facet IDs found.");
        if (HalfEdges.Select(he => he.Id).Distinct().Count() != HalfEdges.Count)
            errors.Add("Duplicate HalfEdge IDs found.");

        // 6. Optional: Prüfe, ob alle referenzierten Vertices, Facets, HalfEdges in den Listen enthalten sind
        foreach (var he in HalfEdges)
        {
            if (!Vertices.Contains(he.RefVertex))
                errors.Add($"HalfEdge {he.Id}: RefVertex {he.RefVertex.Id} not in Vertices list.");
            if (he.RefFacet != Exterior && !Facets.Contains(he.RefFacet))
                errors.Add($"HalfEdge {he.Id}: RefFacet {he.RefFacet.Id} not in Facets list.");
        }
        foreach (var v in Vertices)
        {
            if (!HalfEdges.Contains(v.RefHalfEdge))
                errors.Add($"Vertex {v.Id}: RefHalfEdge {v.RefHalfEdge.Id} not in HalfEdges list.");
        }
        foreach (var f in Facets)
        {
            if (!HalfEdges.Contains(f.RefHalfEdge))
                errors.Add($"Facet {f.Id}: RefHalfEdge {f.RefHalfEdge.Id} not in HalfEdges list.");
        }

        return errors;
    }

    // Optional: Kurzform für bool
    public bool IsValidTopology() => !IsValid().Any();

    #endregion

    #region IO

    public static readonly ImmutableArray<string> ColorPalette = ["#377eb8", "#ff7f00", "#4daf4a", "#e41a1c"];


    /// <summary>
    /// Generates an SVG representation of the current geometric structure and writes it to a file.
    /// </summary>
    /// <remarks>This method creates an SVG file that visually represents the vertices, edges, and facets of
    /// the geometric structure.  It includes styling for hover effects and uses a greedy coloring algorithm to assign
    /// colors to facets.  The generated SVG is written to the specified file, overwriting it if it already exists.  The
    /// method assumes that the geometric structure is well-defined, with valid vertices, edges, and facets.</remarks>
    /// <param name="filename">The base name of the file to which the SVG content will be written. The method appends the ".svg" extension to
    /// this name.</param>
    public void WriteSvg(string filename)
    {
        var getPoint = new Func<Vertex, (string x, string y)>(v =>
        {
            if (v.Point is VecI c)
                return (c.X.ToString(CultureInfo.InvariantCulture), (-c.Y).ToString(CultureInfo.InvariantCulture));
            var he = v.RefHalfEdge;
            var (src, tgt) = he.Edge;
            double posScale = he.Position.Value;
            var diff = tgt - src;
            double x = src.X + (posScale * diff.X);
            double y = src.Y + (posScale * diff.Y);
            return (x.ToString(CultureInfo.InvariantCulture), (-y).ToString(CultureInfo.InvariantCulture));
        });
        var vertices = Vertices.ToDictionary(v => v, getPoint);

        var range = Max - Min;
        double viewBoxWidth = double.Max(10d, range.X);
        double viewBoxHeight = double.Max(10d, range.Y);
        double scale = double.Max(viewBoxWidth, viewBoxHeight);
        string bstrokeWidth = (scale * 0.002).ToString(CultureInfo.InvariantCulture);
        string strokeWidth = (scale * 0.001).ToString(CultureInfo.InvariantCulture);
        string pointRadius = (scale * 0.004).ToString(CultureInfo.InvariantCulture);
        string hoverWidth = pointRadius;
        string hoverRadius = (scale * 0.008).ToString(CultureInfo.InvariantCulture);

        double margin = scale * 0.05;

        StringBuilder sb = new(FormattableString.Invariant($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"{Min.X - margin} {-Max.Y - margin} {viewBoxWidth + margin + margin} {viewBoxHeight + margin + margin}\">"));
        _ = sb.AppendLine();
        _ = sb.AppendLine("<style type=\"text/css\">" +
            $"path:hover {{stroke:red;stroke-width:{hoverWidth};fill-opacity:1;}} " +
            $"line:hover {{stroke:red;stroke-width:{hoverWidth};}} " +
            $"circle:hover {{fill:red; r:{hoverRadius};}}</style>");

        // --- Greedy Coloring ---
        var faceColors = new Dictionary<Facet, string>();
        string[] palette = [.. ColorPalette];
        foreach (var f in Facets)
        {
            if (f == Exterior)
                continue;

            // Nachbarfarben sammeln
            var neighborColors = new HashSet<string>();
            foreach (var he in f.Boundary())
            {
                if (faceColors.TryGetValue(he.Twin.RefFacet, out string? col))
                    neighborColors.Add(col);
            }
            // Erste freie Farbe wählen
            string color = palette.FirstOrDefault(c => !neighborColors.Contains(c)) ?? palette[0];
            faceColors[f] = color;
        }

        // Draw Paths (Facets)
        _ = sb.AppendLine($"<g stroke=\"black\" stroke-width=\"{strokeWidth}\" stroke-linejoin=\"round\" fill-rule=\"evenodd\" fill-opacity=\"0.3\">");
        foreach (var f in Facets)
        {
            if (f == Exterior)
                continue;

            string[] pathPoints = [.. f.Boundary()
                .Select(he => vertices[he.RefVertex])
                .Select(p => $"{p.x} {p.y}")];

            string fc = faceColors[f];
            _ = sb.AppendLine($"<path fill=\"{fc}\" d=\"M{string.Join(" L", pathPoints)}Z\" >");
            _ = sb.AppendLine($"<title>{f}\r\n{f.Id}\r\n{string.Join(',', f.RefIds)}</title>");
            _ = sb.AppendLine("</path>");
        }
        _ = sb.AppendLine("</g>");

        // Draw edges
        _ = sb.AppendLine(FormattableString.Invariant($"<g stroke=\"black\" stroke-width=\"{strokeWidth}\" fill=\"none\">"));
        var doneHe = new HashSet<HalfEdge>();
        foreach (var he in HalfEdges)
        {
            if (!doneHe.Add(he) || !doneHe.Add(he.Twin))
                continue;

            var a = he.RefVertex;
            var b = he.Twin.RefVertex;
            var (x1, y1) = vertices[a];
            var (x2, y2) = vertices[b];
            _ = he.RefFacet == Exterior || he.Twin.RefFacet == Exterior
                ? sb.AppendLine(FormattableString.Invariant($"<line stroke=\"green\" stroke-width=\"{bstrokeWidth}\" x1=\"{x1}\" y1=\"{y1}\" x2=\"{x2}\" y2=\"{y2}\">"))
                : sb.AppendLine(FormattableString.Invariant($"<line x1=\"{x1}\" y1=\"{y1}\" x2=\"{x2}\" y2=\"{y2}\">"));
            _ = sb.AppendLine(FormattableString.Invariant($"<title>{he}\r\n{he.Twin}\r\n{string.Join(',', he.RefIds)}\r\n{string.Join(',', he.Twin.RefIds)}</title>"));
            _ = sb.AppendLine("</line>");
        }
        _ = sb.AppendLine("</g>");

        // Draw points
        _ = sb.AppendLine("<g stroke=\"none\" >");
        foreach (var v in Vertices)
        {
            var (x, y) = vertices[v];
            _ = sb.AppendLine(FormattableString.Invariant($"<circle fill=\"{(v.Point is null ? "blue" : "black")}\" cx=\"{x}\" cy=\"{y}\" r=\"{pointRadius}\">"));
            _ = sb.AppendLine(FormattableString.Invariant($"<title>{v}\r\n{v.Id}\r\n{string.Join(',', v.RefIds)}</title>"));
            _ = sb.AppendLine("</circle>");
        }
        _ = sb.AppendLine("</g>");
        _ = sb.AppendLine("</svg>");
        File.WriteAllText(filename + ".svg", sb.ToString());
    }

    #endregion
}
