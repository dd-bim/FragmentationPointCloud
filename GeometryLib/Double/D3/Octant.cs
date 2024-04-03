using System;

namespace GeometryLib.Double.D3
{
    [Flags]
    public enum Octant
    {
        None = 0,
        XPlus = 1,
        XNeg = 2,
        YPlus = 4,
        YNeg = 8,
        ZPlus = 16,
        ZNeg = 32
    }
}
