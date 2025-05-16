using System;
using System.Globalization;
using static GeometryLib.Constants;
using static GeometryLib.Helper;

namespace GeometryLib.D3;

public readonly record struct Vector
{
    private static readonly Vector zero = new(0.0, 0.0, 0.0);
 
    private static readonly Vector negativeInfinity =
        new(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);

    private static readonly Vector positiveInfinity =
        new(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);

    /// <summary>
    ///     Zero length Vector
    /// </summary>
    public static ref readonly Vector Zero => ref zero;

    /// <summary>Negative infinity vector</summary>
    public static ref readonly Vector NegativeInfinity => ref negativeInfinity;

    /// <summary>Positive infinity vector</summary>
    public static ref readonly Vector PositiveInfinity => ref positiveInfinity;

    /// <summary>X Axis Value</summary>
    public double x { get; }

    /// <summary>Y Axis Value</summary>
    public double y { get; }

    /// <summary>Z Axis Value</summary>
    public double z { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Vector" /> struct.
    /// </summary>
    public Vector(double x, double y, double z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public Vector((double x, double y, double z) tuple) : this(tuple.x, tuple.y, tuple.z)
    {
    }

    public Vector Min(in Vector other)
    {
        return new Vector(
            Math.Min(x, other.x),
            Math.Min(y, other.y),
            Math.Min(z, other.z));
    }

    public Vector Max(in Vector other)
    {
        return new Vector(
            Math.Max(x, other.x),
            Math.Max(y, other.y),
            Math.Max(z, other.z));
    }

    public double Dot(in Vector other)
    {
        (double x, double y, double z) a = (x, y, z);
        (double x, double y, double z) b = (other.x, other.y, other.z);
        return Sum(a.x * b.x, a.y * b.y, a.z * b.z);
    }


    public Vector Cross(in Vector other)
    {
        return new Vector(
            y * other.z - other.y * z,
            z * other.x - other.z * x,
            x * other.y - other.x * y);
    }

    public Vector Cross(in Direction other)
    {
        return new Vector(
            y * other.z - other.y * z,
            z * other.x - other.z * x,
            x * other.y - other.x * y);
    }

    public bool ApproxEquals(in Vector other, double tol = EPS)
    {
        var tempQualifier = new Vector(
            x - other.x,
            y - other.y,
            z - other.z);
        var diff = new Vector(Math.Abs(tempQualifier.x), Math.Abs(tempQualifier.y), Math.Abs(tempQualifier.z));
        return diff.x <= tol && diff.y <= tol && diff.z <= tol;
    }

    public static Vector operator +(in Vector left, in Vector right)
    {
        return new Vector(left.x + right.x, left.y + right.y, left.z + right.z);
    }

    public static Vector operator +(in Vector left, in double right)
    {
        return new Vector(left.x + right, left.y + right, left.z + right);
    }

    public static Vector operator -(in Vector left, in Vector right)
    {
        return new Vector(left.x - right.x, left.y - right.y, left.z - right.z);
    }

    public static Vector operator -(in Vector left, in double right)
    {
        return new Vector(left.x - right, left.y - right, left.z - right);
    }

    public static Vector operator -(in Vector value)
    {
        return new Vector(-value.x, -value.y, -value.z);
    }

    public static double operator *(in Vector left, in Vector right)
    {
        return left.Dot(right);
    }

    public static Vector operator *(in double left, in Vector right)
    {
        return new Vector(
            left * right.x,
            left * right.y,
            left * right.z);
    }

    public static Vector operator *(in Vector left, in double right)
    {
        return new Vector(
            right * left.x,
            right * left.y,
            right * left.z);
    }

    public static Vector operator /(in Vector left, in double right)
    {
        double other = 1.0 / right;
        return new Vector(
            other * left.x,
            other * left.y,
            other * left.z);
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
        return $"POINT Z({this})";
    }

    public static bool TryParse(in string input, out Vector vector, char separator = ' ')
    {
        string str = input.Trim();
        if (!string.IsNullOrEmpty(str))
        {
            string[] split = str.Split([separator], StringSplitOptions.RemoveEmptyEntries);
            if (split.Length >= 3
                && double.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double x)
                && double.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y)
                && double.TryParse(split[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double z))
            {
                vector = new Vector(x, y, z);
                return true;
            }
        }

        vector = default;
        return false;
    }

    public static bool TryParseWkt(in string input, out Vector vector)
    {
        if (!string.IsNullOrEmpty(input) && input.Length > 7)
        {
            int start = input.IndexOf("POINT", StringComparison.InvariantCultureIgnoreCase);
            int end = start + 5;
            if (start >= 0 && end < input.Length)
            {
                start = input.IndexOf("Z", end, StringComparison.InvariantCultureIgnoreCase);
                end = start + 1;
                if (start > 4 && end < input.Length)
                {
                    start = input.IndexOf('(', end) + 1;
                    if (start > 6 && start < input.Length)
                    {
                        end = input.IndexOf(')', start);
                        if (end > start + 4) return TryParse(input[start..end], out vector);
                    }
                }
            }
        }

        vector = default;
        return false;
    }
}