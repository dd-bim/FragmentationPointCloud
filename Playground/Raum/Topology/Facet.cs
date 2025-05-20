using System.Reflection.Emit;

namespace Playground.Raum.Topology
{
    internal class Facet : Element, IDisposable
    {
        private static readonly UniqueCounter _idCounter = new();
        internal HalfEdge _refHalfEdge;

        public long Id { get; }

        public HalfEdge RefHalfEdge
        {
            get => _refHalfEdge;
            set
            {
                value._refFacet = this;
                _refHalfEdge = value;
            }
        }


        internal Facet(HalfEdge halfEdge)
        {
            _refHalfEdge = halfEdge;
            Id = _idCounter.GetNextId();
        }

        public void Dispose()
        {
            _idCounter.ReleaseId(Id);
            GC.SuppressFinalize(this);
        }

        ~Facet()
        {
            _idCounter.ReleaseId(Id);
        }

        public IEnumerable<HalfEdge> Boundary()
        {
            var curr = _refHalfEdge;
            do
            {
#if DEBUG
                if (curr._refFacet != this)
                {
                    throw new Exception("Error in algorithm.");
                }
#endif
                yield return curr;
                curr = curr.Next;
            } while (curr != _refHalfEdge);
        }


    }
}
