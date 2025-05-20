using Playground.Raum.Geometry;

using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Xml.Schema;

namespace Playground.Raum.Topology;

internal class Partition(Epsilon epsilon)
{
    static readonly Random RandomInstance = new(1); // Random seed for reproducibility
    public static bool RandomNext => RandomInstance.Next(2) == 0;

    public const long ExteriorId = 1;

    public Epsilon Epsilon { get; init; } = epsilon;

    public VecI Min { get; private set; } = new(long.MaxValue, long.MaxValue);

    public VecI Max { get; private set; } = new(long.MinValue, long.MinValue);

    public Facet Exterior { get; } = new HalfEdge().RefFacet; // should always be the first facet, to assure the id 1

    public List<Facet> Facets { get; } = [];
    public List<Vertex> Vertices { get; } = [];
    public List<HalfEdge> HalfEdges { get; } = [];

    public Dictionary<Guid, HashSet<Element>> SemanticElements { get; } = [];

    public Dictionary<VecI, Vertex> PointVertices { get; } = [];

    public static void Create(Epsilon epsilon,
        (double x, double y)[][][] multiPolygonA,
        (double x, double y)[][][] multiPolygonB,
        out Partition partition)
    {
        partition = new Partition(epsilon);
        
        // Create BoundingBox
        var box = new BoundingBox();
        box.Extend(multiPolygonA);
        box.Extend(multiPolygonB);
        
        // Create triangulation
        var edges = new List<(Vertex source, Vertex target)>();
        var (multiAId, multiAPolygonIds) = AddMultiPolygon(partition, edges, multiPolygonA);
        var (multiBId, multiBPolygonIds) = AddMultiPolygon(partition, edges, multiPolygonB);

        // Reconstruct edges
        foreach (var (source, target) in edges)
        {
            var refIds = new HashSet<Guid>(source.RefIds);
            refIds.IntersectWith(target.RefIds);
            // assign both sides of the edge the same reference IDs, for a robust polygon reconstruction
            if (!partition.ReconstructEdge(refIds, true, source, target))
            {
                throw new Exception($"Failed to reconstruct edge between {source} and {target}.");
            }
        }

        // Get the facets of the rings

    }

    #region Structure

    /// <summary>
    /// Associates the specified element with a set of reference IDs and updates the semantic mapping accordingly.
    /// </summary>
    /// <remarks>This method updates the <paramref name="element"/>'s reference ID collection by
    /// adding the provided IDs. It also ensures that the semantic mapping is updated, associating each reference ID
    /// with the given element. If a reference ID already exists in the semantic mapping, the element is added to
    /// the existing collection of associated elements. Otherwise, a new mapping is created.</remarks>
    /// <param name="element">The element to associate with the provided reference IDs.</param>
    /// <param name="refIds">A set of reference IDs to associate with the element. Cannot be null.</param>
    private void addSemantics(Element element, HashSet<Guid> refIds)
    {
        element.RefIds.UnionWith(refIds);
        foreach (var refId in refIds)
        {
            if (SemanticElements.TryGetValue(refId, out var value))
            {
                value.Add(element);
            }
            else
            {
                SemanticElements[refId] = [element];
            }
        }
    }

    /// <summary>
    /// Creates a directed edge between two vertices, represented as a pair of half-edges.
    /// </summary>
    /// <remarks>If the source or target vertex does not already exist in the vertex collection, it is
    /// created and added. The method ensures that the half-edge and its twin are properly linked to their
    /// respective vertices.</remarks>
    /// <param name="source">The source vertex of the edge, represented as a <see cref="VecI"/>.</param>
    /// <param name="target">The target vertex of the edge, represented as a <see cref="VecI"/>.</param>
    /// <param name="heRefIds">An optional set of semantic identifiers to associate with the half-edge originating from the source vertex.
    /// If <see langword="null"/>, no semantics are added to this half-edge.</param>
    /// <param name="twRefIds">An optional set of semantic identifiers to associate with the twin half-edge originating from the target
    /// vertex. If <see langword="null"/>, no semantics are added to this twin half-edge.</param>
    /// <returns>A tuple containing the created half-edge and its twin: <list type="bullet"> <item><description><c>he</c>:
    /// The half-edge originating from the source vertex.</description></item> <item><description><c>tw</c>: The
    /// twin half-edge originating from the target vertex.</description></item> </list></returns>
    private (HalfEdge he, HalfEdge tw) addEdge(
        in VecI source, in VecI target,
        HashSet<Guid>? heRefIds,
        HashSet<Guid>? twRefIds)
    {
        var he = new HalfEdge();
        var tw = he.Twin;
        HalfEdges.Add(he);
        HalfEdges.Add(tw);
        if (PointVertices.TryGetValue(source, out var sourceVertex))
        {
            he.RefVertex = sourceVertex;
        }
        else
        {
            he.RefVertex.Point = source;
            Vertices.Add(he.RefVertex);
            PointVertices[source] = he.RefVertex;
        }
        if (PointVertices.TryGetValue(target, out var targetVertex))
        {
            tw.RefVertex = targetVertex;
        }
        else
        {
            tw.RefVertex.Point = target;
            Vertices.Add(tw.RefVertex);
            PointVertices[target] = tw.RefVertex;
        }
        if (heRefIds is not null)
        {
            addSemantics(he, heRefIds);
        }
        if (twRefIds is not null)
        {
            addSemantics(tw, twRefIds);
        }
        return (he, tw);
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
    private (HalfEdge he, HalfEdge tw) addEdge(
        Vertex source, Vertex target,
        HashSet<Guid>? heRefIds,
        HashSet<Guid>? twRefIds)
    {
        var he = new HalfEdge();
        var tw = he.Twin;
        HalfEdges.Add(he);
        HalfEdges.Add(tw);
        he.RefVertex = source;
        tw.RefVertex = target;
        if (heRefIds is not null)
        {
            addSemantics(he, heRefIds);
        }
        if (twRefIds is not null)
        {
            addSemantics(tw, twRefIds);
        }
        return (he, tw);
    }

    /// <summary>
    /// Adds a new vertex to the collection and optionally associates it with reference IDs and a specific point.
    /// </summary>
    /// <param name="refIds">An optional set of reference IDs to associate with the vertex. If provided, the vertex will be linked to
    /// these IDs.</param>
    /// <param name="point">An optional point to assign to the vertex. If provided, the vertex will be associated with this point, and
    /// the point will be mapped to the vertex.</param>
    /// <returns>The newly created vertex.</returns>
    private Vertex addVertex(HashSet<Guid>? refIds = null, VecI? point = null)
    {
        var vertex = new HalfEdge().RefVertex;
        Vertices.Add(vertex);
        if (refIds is not null)
        {
            addSemantics(vertex, refIds);
        }
        if (point is not null)
        {
            vertex.Point = point;
            PointVertices[point.Value] = vertex;
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
    private Facet addFacet(HalfEdge refHalfEdge, HashSet<Guid>? refIds)
    {
        var facet = new Facet(refHalfEdge);
        Facets.Add(facet);
        if (refIds is not null)
        {
            addSemantics(facet, refIds);
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
    /// <param name="partition">The <see cref="Partition"/> instance to which the new facet will be added.</param>
    /// <param name="ab">The first half-edge of the triangle.</param>
    /// <param name="bc">The second half-edge of the triangle.</param>
    /// <param name="ca">The third half-edge of the triangle.</param>
    /// <returns>The newly created <see cref="Facet"/> representing the triangle.</returns>
    private static Facet NewTriangle(Partition partition, HalfEdge ab, HalfEdge bc, HalfEdge ca)
    {
        var abc = partition.addFacet(ab, null);
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
    /// <param name="partition">The partition to which the new triangle will be added.</param>
    /// <param name="ab">The half-edge representing the edge from vertex A to vertex B.</param>
    /// <param name="bc">The half-edge representing the edge from vertex B to vertex C.</param>
    /// <returns>A tuple containing the following: <list type="bullet"> <item> <description>The newly created <see
    /// cref="Facet"/> representing the triangle.</description> </item> <item> <description>The half-edge from
    /// vertex C to vertex A.</description> </item> <item> <description>The half-edge from vertex A to vertex
    /// C.</description> </item> </list></returns>
    private static (Facet abc, HalfEdge ca, HalfEdge ac) NewTriangle(Partition partition, HalfEdge ab, HalfEdge bc)
    {
        var a = ab._refVertex;
        var c = bc.Twin._refVertex;
        var (ca, ac) = partition.addEdge(c, a, null, null);
        var abc = NewTriangle(partition, ab, bc, ca);
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
    /// <param name="partition">The <see cref="Partition"/> instance to which the external triangles will be added.</param>
    /// <param name="v">The vertex to connect to the sequence of edges.</param>
    /// <param name="firstHe">The first <see cref="HalfEdge"/> in the sequence of edges to process.</param>
    /// <param name="lastHe">The last <see cref="HalfEdge"/> in the sequence of edges to process.</param>
    /// <returns>A <see cref="HashSet{T}"/> containing the <see cref="HalfEdge"/> instances affected by the operation.</returns>
    private static HashSet<HalfEdge> AddExternalTriangles(Partition partition, Vertex v, HalfEdge firstHe, HalfEdge lastHe)
    {
        var oi = firstHe;
        var vi = oi._refVertex;
        var (hi, hiT) = partition.addEdge(v, vi, null, null);

        var oiPrev = oi.Prev;
        hiT.Prev = oiPrev;
        hiT._refFacet = partition.Exterior;

        HashSet<HalfEdge> affected = [hi, oi, oiPrev];
        do
        { // create new abc
            var oiSucc = oi.Next;
            (_, _, hi) = NewTriangle(partition, hi, oi);
            oi = oiSucc;
            affected.UnionWith([oi, hi]);
        } while (oi != lastHe);
        lastHe.Prev = hi;
        hi.Prev = hiT;
        hi.RefFacet = partition.Exterior;
        return affected;
    }

    /// <summary>
    /// Splits an edge of a triangle by inserting a new vertex and creating two new half-edges.
    /// </summary>
    /// <remarks>This method modifies the topology of the triangle by splitting the specified edge and
    /// updating the connectivity of the surrounding edges and vertices. The new vertex is inserted along the edge,
    /// and the resulting half-edges are linked to maintain the integrity of the partition.</remarks>
    /// <param name="partition">The geometric partition containing the triangle and its edges.</param>
    /// <param name="ab">The half-edge to be split, directed from vertex A to vertex B.</param>
    /// <param name="v">The new vertex to be inserted along the edge.</param>
    /// <returns>A tuple containing the two new half-edges: <list type="bullet"> <item><description>The half-edge directed
    /// from the new vertex to vertex B.</description></item> <item><description>The twin half-edge directed from
    /// vertex B to the new vertex.</description></item> </list></returns>
    private static (HalfEdge he, HalfEdge tw) TriangleEdgeSplit(Partition partition, HalfEdge ab, Vertex v)
    {
        var ba = ab.Twin;
        var b = ab.Twin._refVertex;
        var (vb, bv) = partition.addEdge(v, b, null, null);

        var abNext = ab.Next;
        var baPrev = ba.Prev;
        var abBehind = ab.Behind;
        var baInFront = ba.InFront;

        var av = ab;
        var va = ba;

        av.Next = vb;
        av.Behind = vb;
        va.Prev = bv;
        va.InFront = bv;
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
    /// <param name="partition">The <see cref="Partition"/> instance representing the geometric structure.</param>
    /// <param name="ab">The half-edge representing one side of the triangle to be split.</param>
    /// <param name="v">The new vertex to be added to the triangle.</param>
    /// <returns>A <see cref="HashSet{T}"/> containing the half-edges affected by the split operation.</returns>
    private static HashSet<HalfEdge> TriangleSplitSide(Partition partition, HalfEdge ab, Vertex v)
    {   // define/save old values
        var bc = ab.Next;
        var ca = ab.Prev;
        var c = ca._refVertex;

        // new sides to v
        var av = ab;

        var (vb, bv) = TriangleEdgeSplit(partition, ab, v);
        var (vc, cv) = partition.addEdge(v, c, null, null);

        av.Next = vc;

        vc.Next = ca;
        vc.RefFacet = av._refFacet;

        _ = NewTriangle(partition, vb, bc, cv);

        HashSet<HalfEdge> affected = [av, vb, bc, ca, vc];

        var va = av.Twin;
        if (va.RefFacet != partition.Exterior)
        {
            var ad = va.Next;
            var db = ad.Next;
            var d = db._refVertex;
            var (vd, dv) = partition.addEdge(v, d, null, null);

            ad.Next = dv;
            va.Prev = dv;

            dv.RefFacet = va._refFacet;

            _ = NewTriangle(partition, bv, vd, db);

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
    /// <param name="partition">The <see cref="Partition"/> instance to which the facet and edges belong.</param>
    /// <param name="abc">The triangular facet to be split.</param>
    /// <param name="v">The new vertex to be connected to the vertices of the original triangle.</param>
    /// <returns>A <see cref="HashSet{T}"/> containing the half-edges created during the split operation.</returns>
    private static HashSet<HalfEdge> TriangleSplit(Partition partition, Facet abc, Vertex v)
    {
        var ab = abc.RefHalfEdge;
        var bc = ab.Next;
        var ca = ab.Prev;
        var a = ab.RefVertex;
        var b = bc.RefVertex;
        var c = ca.RefVertex;

        var (va, av) = partition.addEdge(v, a, null, null);
        var (vb, bv) = partition.addEdge(v, b, null, null);
        var (vc, cv) = partition.addEdge(v, c, null, null);

        var abv = abc;
        ab.Next = bv;
        bv.Next = va;
        va.Next = ab;
        ab.RefFacet = abv;
        bv._refFacet = abv;
        va._refFacet = abv;

        _ = NewTriangle(partition, bc, cv, vb); ;

        _ = NewTriangle(partition, ca, av, vc);

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
    /// <param name="partition">The geometric partition containing the triangulation to be processed.</param>
    /// <param name="affected">A set of half-edges that are initially affected and may require processing.  This set is updated during the
    /// operation as additional edges are identified for flipping.</param>
    private static void Delaunay(Partition partition, HashSet<HalfEdge> affected)
    {
        var exterior = partition.Exterior;
        Queue<HalfEdge> queue = new(affected);
        while (queue.Count > 0)
        {
            var h = queue.Dequeue();
            if (h.RefFacet == exterior || h.Twin.RefFacet == exterior)
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
                    if (t.RefFacet != exterior && t.Twin.RefFacet != exterior
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
    /// <param name="partition">The <see cref="Partition"/> instance to which the point will be added.</param>
    /// <param name="point">The point to add, represented as a <see cref="VecI"/> structure.</param>
    /// <param name="refIds">A set of reference IDs associated with the point, used to track semantics or metadata.</param>
    /// <returns>The newly created or updated <see cref="Vertex"/> instance representing the point.</returns>
    /// <exception cref="Exception"></exception>
    private static Vertex AddPoint(Partition partition, in VecI point, HashSet<Guid> refIds)
    {
        if (partition.PointVertices.TryGetValue(point, out var vertex))
        { // Point already inserted
            partition.addSemantics(vertex, refIds);
            return vertex;
        }

        // extend range
        partition.Min = partition.Min.Min(point);
        partition.Max = partition.Max.Max(point);

        // new vertex
        vertex = partition.addVertex(refIds, point);
        var exterior = partition.Exterior;
        if (partition.Facets.Count > 0)
        { // Triangulation has at least one triangle
          // start halfEdge
            var ab = partition.HalfEdges[0];
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
                    Delaunay(partition, AddExternalTriangles(partition, vertex, first, last));
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
                            Delaunay(partition, TriangleSplitSide(partition, ab, vertex));
                        }
                        else if (bcSign == 0)
                        { // point is on bc
                            Delaunay(partition, TriangleSplitSide(partition, bc, vertex));
                        }
                        else if (caSign == 0)
                        { // point is on hi
                            Delaunay(partition, TriangleSplitSide(partition, ca, vertex));
                        }
                        else
                        { // point is inside abc
                            Delaunay(partition, TriangleSplit(partition, ab.RefFacet, vertex));
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
        else if (partition.HalfEdges.Count > 0)
        {   // Triangulation has one side or multiple collinear sides
            // first HalfEdge with no predecessor
            var first = partition.HalfEdges.First(h => h.Prev == h.Twin);
            // last HalfEdge with no successor
            var last = partition.HalfEdges.Count == 2 ? first
                : partition.HalfEdges
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
                    var (he, tw) = partition.addEdge(vertex, first.RefVertex, null, null);
                    he.Next = first;
                    he.RefFacet = exterior;
                    tw.Prev = first.Twin;
                    tw._refFacet = exterior;
                }
                else if (lesser(lastP, point))
                { // point is right/above of the lastHe side
                    var (he, tw) = partition.addEdge(last.Twin.RefVertex, vertex, null, null);
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
                        if (lesser(current.RefVertex.Point!.Value, p))
                        {
                            var (he, tw) = TriangleEdgeSplit(partition, current, vertex);
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
                _ = AddExternalTriangles(partition, vertex, first, last);
            }
        }
        else if (partition.PointVertices.Count > 1) // Attention: PointVertices.Count is +1, because point is inserted earlier
        { // Triangulation has only one point
            // add new edge between the first point and the new point
            var other = partition.Vertices[0];
            var (he, tw) = partition.addEdge(other, vertex, null, null);
            he.RefFacet = exterior;
            tw._refFacet = exterior;
        }
        return vertex;
    }

    /// <summary>
    /// Adds a multi-polygon to the specified partition and generates unique identifiers for its components.
    /// </summary>
    /// <remarks>This method generates unique identifiers for the multi-polygon, its polygons, and
    /// their respective rings. Each point within the rings is processed and added to the partition with references to
    /// its associated identifiers.</remarks>
    /// <param name="partition">The partition to which the multi-polygon will be added.</param>
    /// <param name="edges">A list of edges to be updated with the new connections created by the multi-polygon.</param>
    /// <param name="multiPolygon">A three-dimensional array representing the multi-polygon. The first dimension represents individual
    /// polygons, the second dimension represents the rings within each polygon, and the third dimension contains
    /// the points (as (x, y) coordinates) within each ring.</param>
    /// <returns>A tuple containing the following: <list type="bullet"> <item> <description> <see cref="Guid"/> representing
    /// the unique identifier for the multi-polygon. </description> </item> <item> <description> An array of tuples,
    /// where each tuple contains: <list type="bullet"> <item> <description> A <see cref="Guid"/> representing the
    /// unique identifier for a polygon. </description> </item> <item> <description> An array of <see cref="Guid"/>
    /// values representing the unique identifiers for the rings within the polygon. </description> </item> </list>
    /// </description> </item> </list></returns>
    private static (Guid multiId, (Guid, Guid[])[] polyIds) AddMultiPolygon(Partition partition, List<(Vertex source, Vertex target)> edges, (double x, double y)[][][] multiPolygon)
    {
        var multiId = Guid.NewGuid();
        var polyIds = new (Guid, Guid[])[multiPolygon.Length];
        for (int i = 0; i < multiPolygon.Length; i++)
        {
            var polyId = Guid.NewGuid();
            var polygon = multiPolygon[i];
            var ringIds = new Guid[polygon.Length];
            polyIds[i] = (polyId, ringIds);
            for (int j = 0; j < polygon.Length; j++)
            {
                var ring = polygon[j];
                ringIds[j] = Guid.NewGuid();
                var refIds = new HashSet<Guid> { multiId, polyId, ringIds[j] };
                Vertex? last = null;
                for (int k = 0; k < ring.Length; k++)
                {
                    var (x, y) = ring[k];
                    var point = partition.Epsilon.Convert(x, y);
                    var curr = AddPoint(partition, in point, refIds);
                    if(last is not null)
                    {
                        edges.Add((last, curr));
                    }
                    last = curr;
                }
            }
        }
        return (multiId, polyIds);
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
    private (HalfEdge vb, HalfEdge bv) sideSplit(HalfEdge ab, in Fraction position, Vertex v)
    {
        var ba = ab.Twin;
        var abNext = ab.Next;
        var baPrev = ba.Prev;
        var baPos = ba.Position;
        var abBehind = ab.Behind;
        var baInFront = ba.InFront;

        var (vb, bv) = addEdge(v, ba.RefVertex, ab.RefIds, ba.RefIds);

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
    internal bool ReconstructEdge(in HashSet<Guid> refIds, bool bothSides, Vertex source, Vertex target)
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
                    addSemantics(right, refIds);
                    if(bothSides)
                    {
                        addSemantics(right.Twin, refIds);
                    }
                } while (nextOnEdge(ref right));
                current = right.Twin.RefVertex; // new Start
                continue;
            }

            // New edge necessary, search intersections until known point
            // List off crossing halfedges, firstHe is right of edge source other is lft of edge target
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
                    var v = addVertex(null);
                    var (ntrans, _) = sideSplit(trans, in transPos, v);
                    transverses.Add((right, ntrans));
                    right = trans.Twin;
                }

            }
            // neue Kante erstellen
            var twinRefIds = bothSides ? refIds : null;
            var (he, tw) = addEdge(current, nextCurrent, refIds, twinRefIds);
            foreach (var (rgt, lft) in transverses)
            {
                // Teilen?
                var heNext = he;
                var twNext = tw;
                if (lft.RefVertex != tw.RefVertex)
                {
                    var (otherSource, otherTarget) = lft.Edge;
                    (heNext, twNext) = sideSplit(he,
                        currentPoint.IntersectEdge(in nextCurrentPoint, in otherSource, in otherTarget),
                        lft.RefVertex);
                }
                // Verknüpfen
                he.Prev = rgt.Prev;
                tw.Prev = lft.Prev;
                he.Next = lft;
                tw.Next = rgt;

                // Flächen
                var heFacet = addFacet(he, rgt.RefFacet.RefIds);
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
        return true;
    }


    #endregion

    #region Topology Methods

    /// <summary>
    /// Identifies and returns all enclosed regions of facets within a given set of half-edges.
    /// </summary>
    /// <remarks>An enclosed region is defined as a group of facets that are fully surrounded by the provided
    /// half-edges without any connection to the exterior or other ambiguous boundaries. Facets that are part of open or
    /// ambiguous regions are excluded from the result.</remarks>
    /// <param name="halfEdges">A read-only set of half-edges that define the boundaries to evaluate for enclosed regions.</param>
    /// <returns>An array of <see cref="HashSet{T}"/> objects, where each set contains the facets that form a single enclosed
    /// region. If no enclosed regions are found, the array will be empty.</returns>
    public static HashSet<Facet>[] EnclosedFacetRegions(in IReadOnlySet<HalfEdge> halfEdges)
    {
        var visitedFacets = new HashSet<Facet>();
        var visitedHalfEdges = new HashSet<HalfEdge>();
        var regions = new List<HashSet<Facet>>();

        foreach (var he in halfEdges)
        {
            if (!visitedHalfEdges.Add(he) || !visitedFacets.Add(he.RefFacet))
                continue;

            var queue = new Queue<Facet>();
            queue.Enqueue(he.RefFacet);
            var region = new HashSet<Facet> { he.RefFacet };
            bool isClosed = true;

            while (queue.TryDequeue(out var facet))
            {
                foreach (var the in facet.Boundary())
                {
                    // Wenn die Kante Teil der Boundary ist, ist das die Grenze der Region
                    if (halfEdges.Contains(the))
                        continue;

                    // Wenn die Nachbar-Facet das Exterior ist, ist die Region offen
                    if (the.Twin.RefFacet.Id == ExteriorId )
                    {
                        isClosed = false;
                        break;
                    }

                    // Wenn das Twin Teil der Boundary ist,
                    // ist die Region nicht eindeutig abgetrennt (anliegende Regionen)
                    if (halfEdges.Contains(the.Twin))
                    {
                        isClosed = false;
                        break;
                    }

                    // Wenn die Kante oder das Facet schon besucht wurde, weiter
                    if (!visitedHalfEdges.Add(the.Twin) || !visitedFacets.Add(the.Twin.RefFacet))
                        continue;

                    region.Add(the.Twin.RefFacet);
                    queue.Enqueue(the.Twin.RefFacet);
                }
                if (!isClosed)
                    break;
            }

            if (isClosed)
                regions.Add(region);
        }
        return [.. regions];
    }


    public static bool IsClosedInterior(in HashSet<HalfEdge> boundary)
    {
        if (boundary.Count < 3)
            return false;
        var region = new HashSet<Facet>();
        foreach (var he in boundary)
        {
            if (he.RefFacet.Id == null || he.Twin.RefFacet == null)
                return false;
            region.Add(he.RefFacet);
            region.Add(he.Twin.RefFacet);
        }
    }


    public static HalfEdge[][] RegionBoundary(in HashSet<Facet> region)
    {
        // Finde alle Kanten, die die Region umgeben, etferne die Kanten, die in der Region sind
        var boundarySet = new HashSet<HalfEdge>();
        foreach (var f in region)
        {
            foreach (var he in f.Boundary())
            {
                if (!boundarySet.Remove(he.Twin))
                {
                    boundarySet.Add(he);
                }
            }
        }
        // Eine Region kann Löcher haben, deshalb sind mehrere Boundaries möglich
        // Sortiere die Kanten in der Reihenfolge, in der sie die Region umgeben
        // der Erste Rand in der Liste ist der Äußere Rand, evtl. andere sind Löcher

        var boundaryList = new List<HalfEdge[]>(boundarySet.Count);
        
        
        var first = boundarySet.First();
        boundaryList.Add(first);
        boundarySet.Remove(first);
        var tail = first.Twin.RefVertex;
        while (boundarySet.Count > 0)
        {
            foreach (var he in boundarySet)
            {
                if (he.RefVertex == tail)
                {
                    boundaryList.Add(he);
                    boundarySet.Remove(he);
                    tail = he.Twin.RefVertex;
                    break;
                }
            }

        }
        return [.. boundaryList];
    }


    #endregion

    #region IO
    public static readonly ImmutableArray<string> ColorPalette = ["#ba495b", "#56ae6c", "#8960b3", "#b0923b"]; // maroon, navy, orange, lavender (light purple) ["blue", "cyan", "green", "yellow", "red", "purple"]

    public void WriteSvg(string filename)
    {
        var getPoint = new Func<Vertex, (string x, string y)>(v =>
        {
            if (v.Point is VecI c)
            {
                return (
                c.X.ToString(CultureInfo.InvariantCulture),
                (-c.Y).ToString(CultureInfo.InvariantCulture));
            }
            var he = v.RefHalfEdge;
            var (src, tgt) = he.Edge;
            double posScale = he.Position.Value;
            var diff = tgt - src;
            double x = src.X + (posScale * diff.X);
            double y = src.Y + (posScale * diff.Y);
            return (
                x.ToString(CultureInfo.InvariantCulture),
                (-y).ToString(CultureInfo.InvariantCulture));
        });
        var vertices = Vertices.ToDictionary(v => v, getPoint);

        var range = Max - Min;
        double viewBoxWidth = double.Max(10d, range.X);
        double viewBoxHeight = double.Max(10d, range.Y);
        double scale = double.Max(viewBoxWidth, viewBoxHeight);
        string bstrokeWidth = (scale * 0.002).ToString(CultureInfo.InvariantCulture); // Adjust these values as needed
        string strokeWidth = (scale * 0.001).ToString(CultureInfo.InvariantCulture); // Adjust these values as needed
        string pointRadius = (scale * 0.004).ToString(CultureInfo.InvariantCulture); // Adjust these values as needed
        string hoverWidth = pointRadius;
        string hoverRadius = (scale * 0.008).ToString(CultureInfo.InvariantCulture);

        // Calculate the margin as 5% of the width and height
        double margin = scale * 0.05;

        // Create an SVG document with a viewBox that fits the PointValues and has a proportional margin
        StringBuilder sb = new(FormattableString.Invariant($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"{Min.X - margin} {-Max.Y - margin} {viewBoxWidth + margin + margin} {viewBoxHeight + margin + margin}\">"));
        _ = sb.AppendLine();
        _ = sb.AppendLine("<style type=\"text/css\">" +
            $"path:hover {{stroke:red;stroke-width:{hoverWidth};fill-opacity:1;}} " +
            $"line:hover {{stroke:red;stroke-width:{hoverWidth};}} " +
            $"circle:hover {{fill:red; r:{hoverRadius};}}</style>");
        // Draw Paths
        var faceColors = new Dictionary<Facet, string>();
        _ = sb.AppendLine($"<g stroke=\"black\" stroke-width=\"{strokeWidth}\" stroke-linejoin=\"round\" fill-rule=\"evenodd\" fill-opacity=\"0.3\">");
        foreach (var f in Facets)
        {
            if (f == Exterior)
            {
                continue;
            }

            var sbf = new StringBuilder();
            var colors = ColorPalette.ToList();
            foreach (var he in f.Boundary())
            {
                if (faceColors.TryGetValue(he.Twin.RefFacet, out string? col))
                {
                    _ = colors.Remove(col);
                }
                var (x, y) = vertices[he.RefVertex];
                _ = sbf.Append($" {x} {y} L");
            }
            _ = sbf.Remove(sbf.Length - 1, 1);
            string fc = colors[RandomInstance.Next(colors.Count)];
            faceColors[f] = fc;
            _ = sb.AppendLine($"<path fill=\"{fc}\" d=\"M{sbf.ToString()}Z\" >");
            //_ = sb.AppendLine($"<title>{f}\r\n{f.Id}\r\n{String.Join(',', FacetSemantics[f.Id])}</title>");
            _ = sb.AppendLine("</path>");
        }
        _ = sb.AppendLine("</g>");
        // Draw edges
        _ = sb.AppendLine(FormattableString.Invariant($"<g stroke=\"black\" stroke-width=\"{strokeWidth}\" fill=\"none\">"));
        var doneHe = new HashSet<HalfEdge>();
        foreach (var he in HalfEdges)
        {
            if (doneHe.Contains(he))
            {
                continue;
            }

            doneHe.Add(he.Twin);

            var a = he.RefVertex;
            var b = he.Twin.RefVertex;

            var (x1, y1) = vertices[a];
            var (x2, y2) = vertices[b];
            if (he.RefFacet == Exterior || he.Twin.RefFacet == Exterior)
            {
                _ = sb.AppendLine(FormattableString.Invariant($"<line stroke=\"green\" stroke-width=\"{bstrokeWidth}\" x1=\"{x1}\" y1=\"{y1}\" x2=\"{x2}\" y2=\"{y2}\">"));
            }
            else
            {
                _ = sb.AppendLine(FormattableString.Invariant($"<line x1=\"{x1}\" y1=\"{y1}\" x2=\"{x2}\" y2=\"{y2}\">"));
            }
            //_ = sb.AppendLine(FormattableString.Invariant($"<title>{he}\r\n{he.Id}|{he.Twin.Id}\r\n{String.Join(',', HalfEdgeSemantics[he.Id])}\r\n{String.Join(',', HalfEdgeSemantics[he.Twin.Id])}</title>"));
            _ = sb.AppendLine("</line>");
        }
        _ = sb.AppendLine("</g>");
        // draw points
        _ = sb.AppendLine("<g stroke=\"none\" >");
        foreach (var v in Vertices)
        {
            var (x, y) = vertices[v];
            _ = sb.AppendLine(FormattableString.Invariant($"<circle fill=\"{(v.Point is null ? "blue" : "black")}\" cx=\"{x}\" cy=\"{y}\" r=\"{pointRadius}\">"));
            //_ = sb.AppendLine(FormattableString.Invariant($"<title>{v}\r\n{v.Id}\r\n{String.Join(',', VertexSemantics[v.Id])}</title>"));
            _ = sb.AppendLine("</circle>");
        }
        _ = sb.AppendLine("</g>");
        _ = sb.AppendLine("</svg>");
        File.WriteAllText(filename + ".svg", sb.ToString());
    } 
    #endregion
}
