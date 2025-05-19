using Playground.Raum.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Playground.Raum.Topology
{
    internal class HalfEdge : Element
    {
        internal Vertex _refVertex;
        internal HalfEdge _prev;
        internal HalfEdge _next;
        internal HalfEdge _twin;
        internal Facet _refFacet;


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

        public HalfEdge Right => Twin.Next;

        public HalfEdge Left => Prev.Twin;

        public HalfEdge()
        {
            Position = Fraction.Zero;
            _refVertex = new Vertex(this);
            _refFacet = new Facet(this);
            _twin = new HalfEdge(_refVertex, _refFacet, this);
            _prev = _twin;
            _next = _twin;

        }

        public HalfEdge(Vertex refVertex, Facet refFacet, HalfEdge twin)
        {
            Position = Fraction.Zero;
            _refVertex = refVertex);
            _refFacet = refFacet;
            _twin = twin;
            _prev = _twin;
            _next = _twin;
        }

    }
}
