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
        ZNeg = 32,
        PPP = XPlus | YPlus | ZPlus,
        NPP = XNeg | YPlus | ZPlus,
        PNP = XPlus | YNeg | ZPlus,
        NNP = XNeg | YNeg | ZPlus,
        PPN = XPlus | YPlus | ZNeg,
        NPN = XNeg | YPlus | ZNeg,
        PNN = XPlus | YNeg | ZNeg,
        NNN = XNeg | YNeg | ZNeg,
    }
}
