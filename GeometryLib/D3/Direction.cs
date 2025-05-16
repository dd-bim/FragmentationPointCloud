using System;
using System.Globalization;
using static GeometryLib.Constants;
using static GeometryLib.Helper;
using static System.Math;

namespace GeometryLib.D3;

public readonly struct Direction : IEquatable<Direction>
{
    private static readonly Direction unitZ = new(0.0, 0.0, 1.0, true);
    private static readonly Direction negUnitZ = new(0.0, 0.0, -1.0, true);

    /// <summary>
    ///     Unit vector of z-axis
    /// </summary>
    public static ref readonly Direction UnitZ => ref unitZ;

    /// <summary>
    ///     Negative unit vector of z-axis
    /// </summary>
    public static ref readonly Direction NegUnitZ => ref negUnitZ;

    /// <summary>X Axis Value</summary>
    public double x { get; }

    /// <summary>Y Axis Value</summary>
    public double y { get; }

    /// <summary>Z Axis Value</summary>
    public double z { get; }

    private Direction(in double x, in double y, in double z, bool dummy)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public Direction(in double x, in double y, in double z)
    {
        double tx = x, ty = y, tz = z;
        Normalize(ref tx, ref ty, ref tz);
        this.x = tx;
        this.y = ty;
        this.z = tz;
    }

    public static explicit operator Direction(in Vector v)
    {
        return new Direction(v.x, v.y, v.z);
    }

    private Direction(in (double x, double y, double z) tuple) : this(tuple.x, tuple.y, tuple.z)
    {
    }

    public Direction(in double azimuth, in double inclination)
    {
        double sin = Sin(inclination);
        double sincos = sin * Cos(azimuth);
        double sinsin = sin * Sin(azimuth);
        double cos = Cos(inclination);
        Normalize(ref sincos, ref sinsin, ref cos);
        this.x = sincos;
        this.y = sinsin;
        this.z = cos;
    }

    public Direction(in D2.Direction azimuth, in D2.Direction inclination)
    {
        double sincos = inclination.sin * azimuth.Cos;
        double sinsin = inclination.sin * azimuth.sin;
        double cos = inclination.Cos;
        Normalize(ref sincos, ref sinsin, ref cos);
        this.x = sincos;
        this.y = sinsin;
        this.z = cos;
    }

    public static Direction Create(in double x, in double y, in double z, out double length)
    {
        double tx = x, ty = y, tz = z;
        length = Normalize(ref tx, ref ty, ref tz);
        return new Direction(tx, ty, tz, true);
    }

    public static implicit operator Vector(in Direction d)
    {
        return new Vector(d.x, d.y, d.z);
    }

    public double Dot(in Direction other)
    {
        (double x, double y, double z) a = (x, y, z);
        (double x, double y, double z) b = (other.x, other.y, other.z);
        return Sum(a.x * b.x, a.y * b.y, a.z * b.z);
    }

    public double Dot(in Vector other)
    {
        (double x, double y, double z) a = (x, y, z);
        (double x, double y, double z) b = (other.x, other.y, other.z);
        return Sum(a.x * b.x, a.y * b.y, a.z * b.z);
    }

    public Vector Cross(in Direction other)
    {
        return new Vector(
            y * other.z - other.y * z, 
            z * other.x - other.z * x, 
            x * other.y - other.x * y);
    }

    public Vector Cross(in Vector other)
    {
        return new Vector(
            y * other.z - other.y * z,
            z * other.x - other.z * x,
            x * other.y - other.x * y);
    }


    public Direction Perp(out Direction third)
    {
        var vx = new Vector(
            y * y - z * x,
            z * z - x * y,
            x * x - y * z);
        var vy = Cross(vx);
        // nochmaliges Cross um nmerische Grenzfälle auszugleichen
        // im Normalfall ist das Ergebniss gleich zu vx
        var second = (Direction)vy.Cross(this);
        third = (Direction)Cross(second);
        return second;
    }

    public Direction MakePerp(in Vector second, out Direction third)
    {
        third = (Direction)Cross(second);
        return (Direction)(third.Cross(this));
    }

    public static Vector operator *(in double left, in Direction right)
    {
        return new Vector(
            left * right.x,
            left * right.y,
            left * right.z);
    }

    public static Vector operator *(in Direction left, in double right)
    {
        return new Vector(
            right * left.x,
            right * left.y,
            right * left.z);
    }

    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:G17}{3}{1:G17}{3}{2:G17}", x, y, z, " ");
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(x, y, z);
    }

    public bool Equals(Direction other)
    {
        return x.Equals(other.x) && y.Equals(other.y) && z.Equals(other.z);
    }

    public override bool Equals(object? obj)
    {
        return obj is Direction other && Equals(other);
    }
}