using System;
using System.Collections.Generic;

namespace GeometryLib.D3;

public readonly struct CoordinateSystem(in Vector? position, in RotMatrix rotation)
{
    public Vector Position { get; } = position ?? Vector.Zero;

    public RotMatrix Rotation { get; } = rotation;

    public CoordinateSystem(in Vector? position, in Direction reference, in Axes referenceAxis, Vector? next = null)
        : this(position, new RotMatrix(reference, referenceAxis, next))
    {
    }

    public override string ToString()
    {
        return $"T:{Position}\r\nR:{Rotation}";
    }
}