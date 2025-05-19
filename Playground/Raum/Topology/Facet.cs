using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Playground.Raum.Topology
{
    internal class Facet : Element
    {
        internal HalfEdge _refHalfEdge;

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
        }



    }
}
