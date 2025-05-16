using System;
using System.Collections.Generic;

namespace GeometryLib.D2;

/// <summary>
///     Bounding Box
/// </summary>
public readonly struct BBox
{
    private static readonly BBox empty = new(
        Vector.PositiveInfinity,
        Vector.NegativeInfinity
    );

    /// <summary>
    ///     Minimal point
    /// </summary>
    public Vector Min { get; }

    /// <summary>
    ///     Maximal point
    /// </summary>
    public Vector Max { get; }

    /// <summary>
    ///     Bounding Box with no extent
    /// </summary>
    public static ref readonly BBox Empty => ref empty;

    private BBox(in Vector min, in Vector max)
    {
        Min = min;
        Max = max;
    }

    public bool Encloses(in BBox other)
    {
        return other.Min.x >= Min.x && other.Max.x <= Max.x && other.Min.y >= Min.y && other.Max.y <= Max.y;
    }

    public static BBox operator +(in BBox box, in Vector vector)
    {
        return new BBox(
            new Vector(
                Math.Min(vector.x, box.Min.x),
                Math.Min(vector.y, box.Min.y)), new Vector(
                Math.Max(vector.x, box.Max.x),
                Math.Max(vector.y, box.Max.y)));
    }

    public override string ToString()
    {
        return $"Min({Min}) Max({Max})";
    }
}