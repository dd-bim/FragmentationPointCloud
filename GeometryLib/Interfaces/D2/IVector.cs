using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeometryLib.Interfaces.D2
{
    public interface IVector<T> where T: struct
    {
        T x { get; }
        T y { get; }

        IVector<T> Min(IVector<T> other);

        IVector<T> Max(IVector<T> other);

        int SideSign(IVector<T> v2, IVector<T> v3);
    }
}
