using Playground.Raum.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Playground.Raum.Topology
{
    internal class Vertex : Element
    {
        internal HalfEdge _refHalfEdge;


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
        }

        public Vertex(HalfEdge halfEdge)
        {
            _refHalfEdge = halfEdge;
        }
    }
}
