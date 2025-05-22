using Playground.Raum.Geometry;

using System.Diagnostics;

namespace Playground.Raum.Topology
{
    internal class HalfEdge : Element, IDisposable
    {
        private static readonly UniqueCounter _idCounter = new();
        internal Vertex _refVertex;
        internal HalfEdge _prev;
        internal HalfEdge _next;
        internal HalfEdge _twin;
        internal Facet _refFacet;
        private HalfEdge? _inFront = null;
        private HalfEdge? _behind = null;

        public long Id { get; }

        public Fraction Position { get; set; }

        public Vertex RefVertex
        {
            get => _refVertex;
            set
            {
                 value._refHalfEdge = this;
                _refVertex = value;
            }
        }

        public HalfEdge Prev
        {
            get => _prev;
            set
            {
                value._next = this;
                _prev = value;
            }
        }

        public HalfEdge Next
        {
            get => _next;
            set
            {
                value._prev = this;
                _next = value;
            }
        }

        public HalfEdge Twin
        {
            get => _twin;
            set
            {
                value._twin = this;
                _twin = value;
            }
        }

        public Facet RefFacet
        {
            get => _refFacet;
            set
            {
                value._refHalfEdge = this;
                _refFacet = value;
            }
        }

        public HalfEdge? InFront
        {
            get => _inFront; set
            {
                _inFront = value;
                if (value is not null)
                {
                    value._behind = this;
                }
            }
        }
        public HalfEdge? Behind
        {
            get => _behind; set
            {
                _behind = value;
                if (value is not null)
                {
                    value._inFront = this;
                }
            }
        }


        public HalfEdge Right => Twin.Next;

        public HalfEdge Left => Prev.Twin;

        public HalfEdge(long? refFacetId = null)
        {
            Position = Fraction.Zero;
            _refVertex = new Vertex(this);
            _refFacet = new Facet(this, refFacetId);
            _twin = new HalfEdge(_refVertex, _refFacet, this);
            _prev = _twin;
            _next = _twin;
            Id = _idCounter.GetNextId();
        }

        public HalfEdge(Vertex refVertex, Facet refFacet, HalfEdge twin)
        {
            Position = Fraction.Zero;
            _refVertex = refVertex;
            _refFacet = refFacet;
            _twin = twin;
            _prev = _twin;
            _next = _twin;
            Id = _idCounter.GetNextId();
        }

        public void Dispose()
        {
            _idCounter.ReleaseId(Id);
            GC.SuppressFinalize(this);
        }

        ~HalfEdge()
        {
            _idCounter.ReleaseId(Id);
        }

        public override string ToString() => $"HalfEdge {Id}: Position {Position}, RefVertex {RefVertex.Id}, RefFacet {RefFacet.Id}, Twin {Twin.Id}, Next {Next.Id}{(InFront is HalfEdge hi ? $", InFront {hi.Id}" : "")}{(Behind is HalfEdge hb ? $", Behind {hb.Id}" : "")}";

        public IEnumerable<HalfEdge> RightStar()
        {
            var cur = this;
            do
            {
                yield return cur;
                cur = cur.Right;
            } while (cur != this);
        }


        /// <summary>
        /// Gets the source point of the edge along the half-edge.
        /// </summary>
        public VecI EdgeSource
        {
            get
            {
                var current = this;
                while (current.RefVertex.Point is null)
                {
                    current = current.InFront ?? throw new Exception($"HalfEdge {current} has no halfedge in front.");
                }
                return current.RefVertex.Point.Value;
            }
        }

        /// <summary>
        /// Gets the target point of the edge along the half-edge.
        /// </summary>
        public VecI EdgeTarget
        {
            get
            {
                var current = this;
                while (current.Twin.RefVertex.Point is null)
                {
                    current = current.Behind
                        ?? throw new Exception($"HalfEdge {current} has no halfedge behind.");
                }
                return current.Twin.RefVertex.Point.Value;
            }
        }

        /// <summary>
        /// Gets the edge along the halfedge represented as a tuple of source and target points.
        /// </summary>
        public (VecI source, VecI target) Edge => (EdgeSource, EdgeTarget);

        /// <summary>
        /// Gets the next <see cref="HalfEdge"/> that is not positioned directly behind the current one.
        /// </summary>
        /// <remarks>This property traverses the linked structure of <see cref="HalfEdge"/> instances to
        /// find the next edge that is not in the "behind" relationship with the current edge. It skips over any edges
        /// where <see cref="Behind"/> is equal to <see cref="Next"/>.</remarks>
        public HalfEdge NextNotBehind
        {
            get
            {
                var current = this;
                while (current.Behind is not null && current.Behind == current.Next)
                {
                    current = current.Next;
                }
                return current.Next;
            }
        }

        public bool IsBehind(in HalfEdge he)
        {
            if(_behind is null)
            {
                if(Twin != he && EdgeSource.SideSign(EdgeTarget, he.EdgeTarget) == 0)
                {
                    Behind = he;
                    return true;
                }
                return false;
            }
            return _behind == he;
        }


        /// <summary>
        /// Determines the right half-edge relative to the specified source and target points.
        /// </summary>
        /// <param name="source">The source point used to determine the orientation of the edge.</param>
        /// <param name="target">The target point used to determine the orientation of the edge.</param>
        /// <param name="right">When this method returns, contains the right half-edge relative to the source and target points,  if the
        /// operation is successful. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the right half-edge is collinear with the source and target points and  has the
        /// same direction; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="Exception">Thrown if an unexpected error occurs during the operation.</exception>
        public bool RightHalfEdge(in VecI source, in VecI target, out HalfEdge right)
        {
            var curr = this;
            var (hSrc, hTgt) = curr.Left.Edge;
            int lastSide = source.GetEdgeOrientation(in target, in hSrc, in hTgt, out _);
            do
            {
                (hSrc, hTgt) = curr.Edge;
                int currSide = source.GetEdgeOrientation(in target, in hSrc, in hTgt, out bool sameDirection);
                if ((lastSide > 0 && currSide < 1) || (currSide == 0 && sameDirection))
                {
                    right = curr;
                    return currSide == 0;
                }
                lastSide = currSide;
                curr = curr.Right;
            } while (curr != this);
            throw new Exception("Unexpected error.");
        }

    }
}
