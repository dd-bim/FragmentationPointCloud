using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeometryLib.Interfaces
{
    public interface IStochastic
    {
        ISpdMatrix Cxx { get; }
    }
}
