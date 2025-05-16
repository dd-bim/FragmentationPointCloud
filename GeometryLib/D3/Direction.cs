using System;
using System.Globalization;
using static GeometryLib.Double.Constants;
using static GeometryLib.Double.Helper;
using static System.Math;

namespace GeometryLib.Double.D3;

public readonly struct Direction
{
    private static readonly Direction unitX = new(1.0, 0.0, 0.0, true);
    private static readonly Direction unitY = new(0.0, 1.0, 0.0, true);
    private static readonly Direction unitZ = new(0.0, 0.0, 1.0, true);
    private static readonly Direction negUnitX = new(-1.0, 0.0, 0.0, true);
    private static readonly Direction negUnitY = new(0.0, -1.0, 0.0, true);
    private static readonly Direction negUnitZ = new(0.0, 0.0, -1.0, true);

    /// <summary>
    ///     Unit vector of x-axis
    /// </summary>
    public static ref readonly Direction UnitX => ref unitX;

    /// <summary>
    ///     Unit vector of y-axis
    /// </summary>
    public static ref readonly Direction UnitY => ref unitY;

    /// <summary>
    ///     Unit vector of z-axis
    /// </summary>
    public static ref readonly Direction UnitZ => ref unitZ;

    /// <summary>
    ///     Negative unit vector of x-axis
    /// </summary>
    public static ref readonly Direction NegUnitX => ref negUnitX;

    /// <summary>
    ///     Negative unit vector of y-axis
    /// </summary>
    public static ref readonly Direction NegUnitY => ref negUnitY;

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

    public Direction(in double x, in double y, in double z, bool intern)
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

    public Direction(in (double x, double y, double z) tuple) : this(tuple.x, tuple.y, tuple.z)
    {
    }

    public Direction(in double azimuth, in double inclination)
    {
        double sin = Sin(inclination);
        double x = sin * Cos(azimuth);
        double y = sin * Sin(azimuth);
        double z = Cos(inclination);
        Normalize(ref x, ref y, ref z);
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public Direction(in D2.Direction azimuth, in D2.Direction inclination)
    {
        double x = inclination.sin * azimuth.cos;
        double y = inclination.sin * azimuth.sin;
        double z = inclination.cos;
        Normalize(ref x, ref y, ref z);
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public Direction(in (D2.Direction azimuth, D2.Direction inclination) angles) : this(angles.azimuth,
        angles.inclination)
    {
    }

    public void Deconstruct(out double outX, out double outY, out double outZ)
    {
        outX = x;
        outY = y;
        outZ = z;
    }

    public (double x, double y, double z) xyz => (x, y, z);

    public static Direction Create(in double x, in double y, in double z, out double length)
    {
        double tx = x, ty = y, tz = z;
        length = Normalize(ref tx, ref ty, ref tz);
        return new Direction(tx, ty, tz, true);
    }

    public static Direction Create(in Vector vector, out double length)
    {
        return Create(vector.x, vector.y, vector.z, out length);
    }

    public static implicit operator Vector(in Direction d)
    {
        return new Vector(d.x, d.y, d.z);
    }

    public double Azimuth()
    {
        return Atan2(y, x);
    }

    public double Inclination()
    {
        return Acos(z);
    }

    public D2.Direction InclinationDirection()
    {
        return new D2.Direction(z, Sqrt(1.0 - z * z), true);
    }

    public Direction Turn()
    {
        return new Direction(-x, -y, -z, true);
    }

    public Vector Mul(in double other)
    {
        return new Vector(
            other * x,
            other * y,
            other * z);
    }

    public double Dot(in Direction other)
    {
        return Helper.Dot(xyz, other.xyz);
    }

    public double Dot(in Vector other)
    {
        return Helper.Dot(xyz, other.xyz);
    }

    public Vector Cross(in Direction other)
    {
        return new Vector(Helper.Cross(xyz, other.xyz));
    }

    public Vector Cross(in Vector other)
    {
        return new Vector(Helper.Cross(xyz, other.xyz));
    }

    public D2.Direction Diff(in Direction other)
    {
        return D2.Direction.Diff(this, other);
    }

    public double DiffCos(in Direction other)
    {
        return Dot(other);
    }

    public double DiffSin(in Direction other)
    {
        (double x, double y, double z) = Helper.Cross(other.xyz, xyz);
        return Hypot(x, y, z);
    }

    private Vector perp()
    {
        return new Vector(
            y * y - z * x,
            z * z - x * y,
            x * x - y * z);
    }

    public Direction Perp()
    {
        Vector vx = perp();
        Vector vy = Cross(vx);
        return (Direction)vy.Cross(this);
        // nochmaliges Cross um nmerische Grenzfälle auszugleichen
        // im Normalfall ist das Ergebniss gleich zu vx
    }

    public Direction Perp(out Direction third)
    {
        Direction second = Perp();
        third = (Direction)Cross(second);
        return second;
    }

    public Direction MakePerp(in Vector second, out Direction third)
    {
        third = new Direction(Helper.Cross(xyz, second.xyz));
        return new Direction(Helper.Cross(third.xyz, xyz));
    }

    public Direction BiSector(in Direction other)
    {
        double xx = x + other.x;
        double yy = y + other.y;
        double zz = z + other.z;
        double len = Hypot(xx, yy, zz);
        if (len > TRIGTOL)
        {
            len = 1.0 / len;
            return new Direction(xx * len, yy * len, zz * len, true);
        }

        return Perp(); // Richtungen entgegengesetzt kollinear
    }

    public static Vector operator *(in double left, in Direction right)
    {
        return right.Mul(left);
    }

    public static Vector operator *(in Direction left, in double right)
    {
        return left.Mul(right);
    }

    public static Vector operator /(in Direction left, in double right)
    {
        return left.Mul(1.0 / right);
    }

    public static Direction operator -(in Direction value)
    {
        return value.Turn();
    }


    public override string ToString()
    {
        return ToString(" ");
    }

    public string ToString(string separator)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:G17}{3}{1:G17}{3}{2:G17}", x, y, z, separator);
    }

    public string ToWktString()
    {
        return $"POINT Z({ToString()})";
    }


    public override int GetHashCode()
    {
#if NETSTANDARD2_0 || NET472 || NET48 || NET462
            return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();
#else
        return HashCode.Combine(x, y, z);
#endif
    }
}