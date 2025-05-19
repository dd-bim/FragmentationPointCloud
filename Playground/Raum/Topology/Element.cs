using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Playground.Raum.Topology
{
    internal abstract class Element
    {
        public HashSet<Guid> RefIds { get; } = new HashSet<Guid>();
    }
}
