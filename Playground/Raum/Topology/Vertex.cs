using Playground.Raum.Geometry;

namespace Playground.Raum.Topology
{
    internal class Vertex : Element, IDisposable
    {
        private static readonly UniqueCounter _idCounter = new();
        internal HalfEdge _refHalfEdge;

        public long Id { get; }

        public VecI? Point { get; set; }

        public HalfEdge RefHalfEdge
        {
            get => _refHalfEdge;
            set
            {
                value._refVertex = this;
                _refHalfEdge = value;
            }
        }

        internal Vertex(HalfEdge halfEdge, VecI point)
        {
            _refHalfEdge = halfEdge;
            Point = point;
            Id = _idCounter.GetNextId();
        }

        public Vertex(HalfEdge halfEdge)
        {
            _refHalfEdge = halfEdge;
            Id = _idCounter.GetNextId();
        }

        public void Dispose()
        {
            _idCounter.ReleaseId(Id);
            GC.SuppressFinalize(this);
        }

        ~Vertex()
        {
            _idCounter.ReleaseId(Id);
        }

        public override string ToString() => $"Vertex {Id}: RefHalfEdge {RefHalfEdge.Id}{(Point.HasValue ? $", {Point}" : "")}";


        /// <summary>
        /// Determines whether the half-edge from the specified source to the target is a right half-edge.
        /// </summary>
        /// <param name="source">The starting point of the half-edge.</param>
        /// <param name="target">The ending point of the half-edge.</param>
        /// <param name="right">When this method returns, contains the right half-edge if the operation is successful; otherwise, contains
        /// the default value.</param>
        /// <returns><see langword="true"/> if the half-edge is a right half-edge; otherwise, <see langword="false"/>.</returns>
        public bool RightHalfEdge(in VecI source, in VecI target, out HalfEdge right) =>
             RefHalfEdge.RightHalfEdge(in source, in target, out right);


        public IEnumerable<HalfEdge> Star() => RefHalfEdge.RightStar();

        public List<HalfEdge> GetHalfEdgeChainTo(Vertex target)
        {
            var result = new List<HalfEdge>();
            if (Point is null || target.Point is null)
                return result;

            foreach (var he in Star())
            {
                // Prüfe, ob diese Halbkante (oder ihre Behind-Kette) zu target führt
                var current = he;
                result.Clear();
                while (true)
                {
                    result.Add(current);
                    if (current.Twin.RefVertex == target)
                        return result;
                    if (current.Behind is null)
                        break;
                    current = current.Behind;
                }
            }
            return [];
        }

    }
}
