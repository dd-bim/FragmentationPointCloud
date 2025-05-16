using System.Collections.Generic;
using System.Linq;

namespace GeometryLib.D3;

/// <summary>
///     Bounding Box
/// </summary>
public readonly struct BBox(in Vector min, in Vector max)
{
    private static readonly BBox empty = new(
        Vector.PositiveInfinity,
        Vector.NegativeInfinity
    );

    /// <summary>
    ///     Minimal point
    /// </summary>
    public Vector Min { get; } = min;

    /// <summary>
    ///     Maximal point
    /// </summary>
    public Vector Max { get; } = max;

    /// <summary>
    ///     Bounding Box with no extent
    /// </summary>
    public static ref readonly BBox Empty => ref empty;

    public static BBox operator +(in BBox box, in Vector vector)
    {
        return new BBox(
            vector.Min(box.Min),
            vector.Max(box.Max));
    }

    public override string ToString()
    {
        return $"Min({Min}) Max({Max})";
    }
}