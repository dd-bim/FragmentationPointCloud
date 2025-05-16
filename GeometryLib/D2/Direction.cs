using System.Globalization;
using static GeometryLib.Double.Constants;
using static GeometryLib.Double.Helper;
using static System.Math;

namespace GeometryLib.Double.D2;

public readonly struct Direction
{
    private static readonly Direction unitX = new(1.0, 0.0, true);
    private static readonly Direction unitY = new(0.0, 1.0, true);
    private static readonly Direction negUnitX = new(-1.0, 0.0, true);
    private static readonly Direction negUnitY = new(0.0, -1.0, true);

    /// <summary>
    ///     Unit vector of x-axis
    /// </summary>
    public static ref readonly Direction UnitX => ref unitX;

    /// <summary>
    ///     Unit vector of y-axis
    /// </summary>
    public static ref readonly Direction UnitY => ref unitY;

    /// <summary>
    ///     Negative unit vector of x-axis
    /// </summary>
    public static ref readonly Direction NegUnitX => ref negUnitX;

    /// <summary>
    ///     Negative unit vector of y-axis
    /// </summary>
    public static ref readonly Direction NegUnitY => ref negUnitY;

    /// <summary>X Axis Value</summary>
    public double x { get; }

    /// <summary>Y Axis Value</summary>
    public double y { get; }

    /// <summary>Sine of direction angle</summary>
    public double sin => y;

    /// <summary>Cosine of direction angle</summary>
    public double cos => x;

    internal Direction(in double x, in double y, bool intern)
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

    public Direction(in Vector v) : this(v.x, v.y)
    {
    }

    public Direction(in double angle) : this(Cos(angle), Sin(angle))
    {
    }

    public void Deconstruct(out double outX, out double outY)
    {
        (outX, outY) = (x, y);
    }

    public static Direction Create(in double x, in double y, out double length)
    {
        double tx = x, ty = y;
        length = Normalize(ref tx, ref ty);
        return new Direction(x, y, true);
    }

    public static Direction Create(in Vector vector, out double length)
    {
        return Create(vector.x, vector.y, out length);
    }

    public static implicit operator Vector(in Direction d)
    {
        return new Vector(d.x, d.y);
    }

    private (double x, double y) Tuple => (x, y);

    public double Angle()
    {
        return Atan2(y, x);
    }

    /// <summary>2 * PI - x</summary>
    private Direction Neg()
    {
        return new Direction(x, -y, true);
    }

    public Direction TwoPiSubThis()
    {
        return Neg();
    }

    /// <summary>PI/2 - x</summary>
    private Direction Complementary()
    {
        return new Direction(y, x, true);
    }

    public Direction HalfPiSubThis()
    {
        return Complementary();
    }

    /// <summary>PI - x</summary>
    private Direction Supplementary()
    {
        return new Direction(-x, y, true);
    }

    public Direction PiSubThis()
    {
        return Supplementary();
    }

    public Direction SubHalfPi()
    {
        return new Direction(y, -x, true);
    }

    private Direction AddHalfPi()
    {
        return new Direction(-y, x, true);
    }

    private Direction AddPi()
    {
        return new Direction(-x, -y, true);
    }

    public Direction SubPi()
    {
        return AddPi();
    }

    public Direction ReNormalize()
    {
        double rlen = 1.0 / double.Hypot(x, y);
        return new Direction(x * rlen, y * rlen);
    }

    internal static Direction Diff(in D3.Direction left, in D3.Direction right)
    {
        return new Direction(left.DiffCos(right), left.DiffSin(right));
    }

    public Direction Diff(in Direction other)
    {
        return new Direction(Dot(other), Abs(other.Det(this)));
    }

    public double DiffCos(in Direction other)
    {
        return Dot(other);
    }

    public double DiffSin(in Direction other)
    {
        return Abs(other.Det(this));
    }

    public Direction Sub(in Direction other)
    {
        return new Direction(Dot(other), other.Det(this));
    }

    public Direction Add(in Direction other)
    {
        return new Direction(x * other.x - y * other.y, y * other.x + x * other.y);
    }

    public Vector Mul(in double other)
    {
        return new Vector(other * x, other * y);
    }

    public double Dot(in Direction other)
    {
        return Helper.Dot(Tuple, other.Tuple);
    }

    public double Det(in Direction other)
    {
        return Helper.Det(Tuple, other.Tuple);
    }

    public double Dot(in Vector other)
    {
        return Helper.Dot(Tuple, other.xy);
    }

    public double Det(in Vector other)
    {
        return Helper.Det(Tuple, other.xy);
    }

    public Direction BiSector(in Direction other)
    {
        double xx = x + other.x;
        double yy = y + other.y;
        var len = double.Hypot(xx, yy);
        return len > TRIGTOL
            ? new Direction(xx / len, yy / len, true)
            : AddHalfPi(); // Richtungen entgegengesetzt kollinear
    }

    public Vector RotateSub(in Vector other)
    {
        return new Vector(Dot(other), Det(other));
    }

    public Vector RotateAdd(in Vector other)
    {
        return new Vector(x * other.x - y * other.y, y * other.x + x * other.y);
    }

    public override string ToString()
    {
        return ToString(" ");
    }

    public string ToString(string separator)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:G17}{2}{1:G17}", x, y, separator);
    }

    public static Direction operator +(in Direction left, in Direction right)
    {
        return left.Add(right);
    }

    public static Direction operator -(in Direction left, in Direction right)
    {
        return left.Sub(right);
    }

    public static double operator *(in Direction left, in Direction right)
    {
        return left.Dot(right);
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
}