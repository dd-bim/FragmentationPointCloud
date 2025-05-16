using System.Globalization;
using static GeometryLib.Helper;
using static System.Math;

namespace GeometryLib.D2;

public readonly struct Direction
{
    private static readonly Direction unitX = new(1.0, 0.0, true);

    /// <summary>
    ///     Unit vector of x-axis
    /// </summary>
    public static ref readonly Direction UnitX => ref unitX;

    /// <summary>X Axis Value</summary>
    public double x { get; }

    /// <summary>Y Axis Value</summary>
    public double y { get; }

    /// <summary>Sine of direction angle</summary>
    public double sin => y;

    /// <summary>Cosine of direction angle</summary>
    public double Cos => x;

    private Direction(in double x, in double y, bool intern)
    {
        this.x = x;
        this.y = y;
    }

    private Direction(in double x, in double y)
    {
        double tx = x, ty = y;
        Normalize(ref tx, ref ty);
        this.x = tx;
        this.y = ty;
    }

    public Direction(in double angle) : this(Cos(angle), Sin(angle))
    {
    }

    public static implicit operator Vector(in Direction d)
    {
        return new Vector(d.x, d.y);
    }

    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:G17}{2}{1:G17}", x, y, " ");
    }

    public static Direction operator +(in Direction left, in Direction right)
    {
        return new Direction(left.x * right.x - left.y * right.y, left.y * right.x + left.x * right.y);
    }

}