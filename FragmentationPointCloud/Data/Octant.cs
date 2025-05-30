using System;

namespace Revit.Data;


/// <summary>
/// Represents the octants of a three-dimensional Cartesian coordinate system.
/// </summary>
/// <remarks>An octant is one of the eight divisions of a three-dimensional space, determined by the signs of the
/// X, Y, and Z coordinates. This enumeration uses the <see cref="FlagsAttribute"/> to allow bitwise combinations of its
/// values.</remarks>
[Flags]
public enum Octant
{
    None = 0,
    XPos = 1,
    XNeg = 2,
    YPos = 4,
    YNeg = 8,
    ZPos = 16,
    ZNeg = 32,
    PPP = XPos | YPos | ZPos,
    NPP = XNeg | YPos | ZPos,
    PNP = XPos | YNeg | ZPos,
    NNP = XNeg | YNeg | ZPos,
    PPN = XPos | YPos | ZNeg,
    NPN = XNeg | YPos | ZNeg,
    PNN = XPos | YNeg | ZNeg,
    NNN = XNeg | YNeg | ZNeg
}
