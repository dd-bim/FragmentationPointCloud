using System;
using System.Collections.Generic;
using System.Globalization;
using GeometryLib.D3;
using GeometryLib.Double;
using static GeometryLib.Double.Constants;

namespace GeometryLib.D2;

public readonly struct Vector : IEquatable<Vector>, IComparable<Vector>
{
    private static readonly Vector zero = new(0.0, 0.0);
    private static readonly Vector nan = new(double.NaN, double.NaN);
    private static readonly Vector negativeInfinity = new(double.NegativeInfinity, double.NegativeInfinity);
    private static readonly Vector positiveInfinity = new(double.PositiveInfinity, double.PositiveInfinity);

    /// <summary>
    ///     Zero length Vector
    /// </summary>
    public static ref readonly Vector Zero => ref zero;

    /// <summary>
    ///     NaN vector
    /// </summary>
    public static ref readonly Vector NaN => ref nan;

    /// <summary>Negative infinity vector</summary>
    public static ref readonly Vector NegativeInfinity => ref negativeInfinity;

    /// <summary>Positive infinity vector</summary>
    public static ref readonly Vector PositiveInfinity => ref positiveInfinity;

    /// <summary>X Axis Value</summary>
    public double x { get; }

    /// <summary>Y Axis Value</summary>
    public double y { get; }

    public bool IsNaN => double.IsNaN(x) || double.IsNaN(y);

    public bool IsInfinity => double.IsInfinity(x) || double.IsInfinity(y);

    /// <summary>
    ///     Initializes a new instance of the <see cref="Vector" /> struct.
    /// </summary>
    public Vector(double x, double y)
    {
        this.x = x;
        this.y = y;
    }

    public Vector(D3.Vector vector)
    {
        x = vector.x;
        y = vector.y;
    }

    public Vector((double x, double y) tuple) : this(tuple.x, tuple.y)
    {
    }

    /// <summary>Deconstructs the vector.</summary>
    public void Deconstruct(out double outX, out double outY)
    {
        (outX, outY) = (x, y);
    }

    /// <summary>Gets the tuple.</summary>
    public (double x, double y) xy => (x, y);

    //public static implicit operator Vector(IVector2<double> v) => v is Vector vec ? vec : new Vector(v.x,v.y);

    /// <summary>The length (magnitude) of the vector.</summary>
    public double Length()
    {
        return double.Hypot(x, y);
    }

    /// <summary>The angle of the vector direction.</summary>
    public double Angle()
    {
        return Math.Atan2(y, x);
    }

    public double Sum()
    {
        return x + y;
    }

    public double AbsSum()
    {
        return Math.Abs(x) + Math.Abs(y);
    }

    public Vector Neg()
    {
        return new Vector(-x, -y);
    }

    public Vector Square()
    {
        return new Vector(x * x, y * y);
    }

    public Vector Abs()
    {
        return new Vector(Math.Abs(x), Math.Abs(y));
    }

    public double SumSq()
    {
        return Helper.Dot(xy, xy);
    }

    public bool ApproxEquals(in Vector other, double tol = EPS)
    {
        Vector diff = Sub(other).Abs();
        return diff.x <= tol && diff.y <= tol;
    }

    public Vector Add(in Vector other)
    {
        return new Vector(x + other.x, y + other.y);
    }

    public Vector Add(in double other)
    {
        return new Vector(x + other, y + other);
    }

    public Vector Sub(in Vector other)
    {
        return new Vector(x - other.x, y - other.y);
    }

    public Vector Sub(in double other)
    {
        return new Vector(x - other, y - other);
    }

    public Vector Mul(in double other)
    {
        return new Vector(other * x, other * y);
    }

    public Vector Mid(in Vector other)
    {
        return new Vector(
            0.5 * (x + other.x),
            0.5 * (y + other.y));
    }

    public Vector Min(in Vector other)
    {
        return new Vector(
            Math.Min(x, other.x),
            Math.Min(y, other.y));
    }

    public Vector Max(in Vector other)
    {
        return new Vector(
            Math.Max(x, other.x),
            Math.Max(y, other.y));
    }

    /// <summary>
    ///     Scalar product (dot)
    /// </summary>
    public double Dot(in Vector other)
    {
        return Helper.Dot(xy, other.xy);
    }

    public double Det(in Vector a, in Vector b)
    {
        return a.Sub(this).Det(b.Sub(this));
    }

    /// <summary>
    ///     Determinant of a 2x2 matrix of the vectors
    /// </summary>
    public double Det(in Vector other)
    {
        return Helper.Det(xy, other.xy);
    }

    public SpdMatrix Outer()
    {
        return new SpdMatrix(x * x, x * y, y * y);
    }

    public double SignedDistanceToLine(Vector lineA, Vector lineB)
    {
        Vector l = lineB - lineA;
        return l.Det(Sub(lineA)) / l.Length();
    }


    public static Vector operator +(in Vector left, in Vector right)
    {
        return left.Add(right);
    }

    public static Vector operator +(in Vector left, in double right)
    {
        return left.Add(right);
    }

    public static Vector operator -(in Vector left, in Vector right)
    {
        return left.Sub(right);
    }

    public static Vector operator -(in Vector left, in double right)
    {
        return left.Sub(right);
    }

    public static Vector operator -(in Vector value)
    {
        return value.Neg();
    }

    public static double operator *(in Vector left, in Vector right)
    {
        return left.Dot(right);
    }

    public static Vector operator *(in double left, in Vector right)
    {
        return right.Mul(left);
    }

    public static Vector operator *(in Vector left, in double right)
    {
        return left.Mul(right);
    }

    public static Vector operator /(in Vector left, in double right)
    {
        return left.Mul(1.0 / right);
    }

    public static Vector Mean(in Vector a, in Vector b)
    {
        return a.Mid(b);
    }

    public static Vector Mean(in Vector a, in Vector b, in Vector c)
    {
        return new Vector(
            Helper.Sum(a.x, b.x, c.x) * THIRD,
            Helper.Sum(a.y, b.y, c.y) * THIRD);
    }

    public static Vector Mean(in IReadOnlyCollection<Vector> vectors, bool isRing = false)
    {
        using var it = vectors.GetEnumerator();
        if (!it.MoveNext()) return Zero;
        Vector a = it.Current;
        if (!it.MoveNext()) return a;
        Vector b = it.Current;
        if (!it.MoveNext()) return isRing ? a : Mean(a, b);
        Vector c = it.Current;
        if (!it.MoveNext()) return isRing ? Mean(a, b) : Mean(a, b, c);
        Vector sum = Sum(vectors, isRing);
        return sum / (isRing ? vectors.Count - 1 : vectors.Count);
    }

    public static Vector Sum(in IReadOnlyCollection<Vector> vectors, bool isRing = false)
    {
        int len = isRing ? vectors.Count - 1 : vectors.Count;
        if (len < 1) return Zero;
        using var it = vectors.GetEnumerator();
        _ = it.MoveNext();
        if (len == 1) return it.Current;
        var xs = new double[len];
        var axs = new double[len];
        var ys = new double[len];
        var ays = new double[len];
        for (var i = 0; i < len; i++)
        {
            (double x, double y) = it.Current;
            xs[i] = x;
            ys[i] = y;
            axs[i] = Math.Abs(x);
            ays[i] = Math.Abs(y);
            it.MoveNext();
        }

        Array.Sort(axs, xs);
        Array.Sort(ays, ys);
        double sx = xs[0];
        double sy = ys[0];
        for (var i = 1; i < len; i++)
        {
            sx += xs[i];
            sy += ys[i];
        }

        return new Vector(sx, sy);
    }

    public D3.Vector Transform(Quaternion rotation, D3.Vector translation)
    {
        return translation + rotation.Transform(this);
    }

    //public Vector BackTransform(Quaternion rotation, Vector translation) =>
    //    rotation.Conj() * Sub(translation);

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
        return x == point.x && y == point.y;
    }

    public override bool Equals(object? obj)
    {
        return obj is Vector point && Equals(point);
    }

    public override int GetHashCode()
    {
#if NETSTANDARD2_0 || NET472 || NET48 || NET462
            return x.GetHashCode() ^ y.GetHashCode();
#else
        return HashCode.Combine(x, y);
#endif
    }

    public int CompareTo(Vector other)
    {
        int comp = x.CompareTo(other.x);
        return comp != 0 ? comp : y.CompareTo(other.y);
    }

    public Vector OtherY(in Vector other)
    {
        return new Vector(x, other.y);
    }

    public Vector AddOne()
    {
        return new Vector(x + 1, y + 1);
    }

    public Vector SubOne()
    {
        return new Vector(x - 1, y - 1);
    }

    public Vector ToDouble()
    {
        return this;
    }

    public static bool TryParse(in string input, out Vector vector, char separator = ' ')
    {
        string str = input.Trim();
        if (!string.IsNullOrEmpty(str))
        {
            string[] split = str.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
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

    public class XComparer : IComparer<Vector>
    {
        public int Compare(Vector x, Vector y)
        {
            return x.x.CompareTo(y.x);
        }
    }

    public class YComparer : IComparer<Vector>
    {
        public int Compare(Vector x, Vector y)
        {
            return x.y.CompareTo(y.y);
        }
    }
}