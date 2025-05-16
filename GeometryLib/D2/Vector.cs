using System;
using System.Globalization;

namespace GeometryLib.D2;

public readonly struct Vector : IEquatable<Vector>, IComparable<Vector>
{
    private static readonly Vector negativeInfinity = new(double.NegativeInfinity, double.NegativeInfinity);
    private static readonly Vector positiveInfinity = new(double.PositiveInfinity, double.PositiveInfinity);

    /// <summary>Negative infinity vector</summary>
    public static ref readonly Vector NegativeInfinity => ref negativeInfinity;

    /// <summary>Positive infinity vector</summary>
    public static ref readonly Vector PositiveInfinity => ref positiveInfinity;

    /// <summary>X Axis Value</summary>
    public double x { get; }

    /// <summary>Y Axis Value</summary>
    public double y { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Vector" /> struct.
    /// </summary>
    public Vector(double x, double y)
    {
        this.x = x;
        this.y = y;
    }

    /// <summary>Deconstructs the vector.</summary>
    public void Deconstruct(out double outX, out double outY)
    {
        (outX, outY) = (x, y);
    }

    public override string ToString()
    {
        return ToString(" ");
    }

    public string ToString(string separator)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:G17}{2}{1:G17}", x, y, separator);
    }

    public bool Equals(Vector point)
    {
        return Math.Abs(x - point.x) < Constants.EPS && Math.Abs(y - point.y) < Constants.EPS;
    }

    public override bool Equals(object? obj)
    {
        return obj is Vector point && Equals(point);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(x, y);
    }

    public int CompareTo(Vector other)
    {
        int comp = x.CompareTo(other.x);
        return comp != 0 ? comp : y.CompareTo(other.y);
    }

    public static bool TryParse(in string input, out Vector vector, char separator = ' ')
    {
        string str = input.Trim();
        if (!string.IsNullOrEmpty(str))
        {
            string[] split = str.Split([separator], StringSplitOptions.RemoveEmptyEntries);
            if (split.Length >= 2
                && double.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double x)
                && double.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
            {
                vector = new Vector(x, y);
                return true;
            }
        }

        vector = default;
        return false;
    }

    public static bool operator ==(Vector left, Vector right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Vector left, Vector right)
    {
        return !(left == right);
    }

    public static bool operator <(Vector left, Vector right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(Vector left, Vector right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >(Vector left, Vector right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator >=(Vector left, Vector right)
    {
        return left.CompareTo(right) >= 0;
    }

}