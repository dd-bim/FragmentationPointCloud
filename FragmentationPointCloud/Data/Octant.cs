using System;

namespace Revit.Data
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
