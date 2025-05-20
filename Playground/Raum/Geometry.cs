using System.Numerics;
using System.Runtime.CompilerServices;

namespace Playground.Raum.Geometry;

public readonly record struct VecI(long X, long Y)
{
    public static VecI operator -(in VecI left, in VecI right) => new(left.X - right.X, left.Y - right.Y);
    public static VecI operator +(in VecI left, in VecI right) => new(left.X + right.X, left.Y + right.Y);

    /// <summary>
    /// Determinant
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static Int128 operator ^(in VecI left, in VecI right) => ((Int128)left.X * right.Y) - ((Int128)left.Y * right.X);

    /// <summary>
    /// Returns a new <see cref="VecI"/> instance with the minimum X and Y values between this instance and the
    /// specified <paramref name="other"/> instance.
    /// </summary>
    /// <param name="other">The <see cref="VecI"/> instance to compare with this instance.</param>
    /// <returns>A new <see cref="VecI"/> instance where each component is the smaller of the corresponding components in this
    /// instance and <paramref name="other"/>.</returns>
    public VecI Min(in VecI other) => new(long.Min(X, other.X), long.Min(Y, other.Y));

    /// <summary>
    /// Returns a new vector containing the maximum values of the corresponding components from this vector and the
    /// specified vector.
    /// </summary>
    /// <param name="other">The vector to compare with this vector.</param>
    /// <returns>A new <see cref="VecI"/> where each component is the greater of the corresponding components of this vector and
    /// <paramref name="other"/>.</returns>
    public VecI Max(in VecI other) => new(long.Max(X, other.X), long.Max(Y, other.Y));

    /// <summary>
    /// Determines the relative position of this point with respect to the directed edge defined by the specified source
    /// and target points.
    /// </summary>
    /// <param name="edgeSource">The starting point of the directed edge.</param>
    /// <param name="edgeTarget">The ending point of the directed edge.</param>
    /// <returns>An integer indicating the relative position of this point: <list type="bullet"> <item><description><c>1</c> if
    /// this point is to the left of the directed edge.</description></item> <item><description><c>-1</c> if this point
    /// is to the right of the directed edge.</description></item> <item><description><c>0</c> if this point lies
    /// exactly on the directed edge.</description></item> </list></returns>
    public int SideSign(in VecI edgeSource, in VecI edgeTarget) => Int128.Sign((edgeSource - this) ^ (edgeTarget - this));

    /// <summary>
    /// Determines whether a flip operation is required based on the positions of three vertices and the current point.
    /// </summary>
    /// <remarks>This method evaluates the relative positions of the vertices and the current point to
    /// determine  whether the point lies within the circumcircle of the triangle formed by the vertices. If the point 
    /// is inside the circumcircle, a flip operation is required to maintain the Delaunay condition.</remarks>
    /// <param name="a">The first vertex of the triangle.</param>
    /// <param name="b">The second vertex of the triangle.</param>
    /// <param name="c">The third vertex of the triangle.</param>
    /// <returns><see langword="true"/> if a flip operation is required; otherwise, <see langword="false"/>.</returns>
    public bool DoFlip(in VecI a, in VecI b, in VecI c) // => inCircle(sa - this, ta - this, c - this);
    {
        // Cline_Renka_test
        //            c
        //           /1\
        //          /---\
        //         /     \
        //        sa-------ta
        //         \     /
        //          \___/
        //           \2/
        //            P
        // P muss rechts von sa-ta liegen!

        Int128 xca = a.X - c.X;
        Int128 xcb = b.X - c.X;
        Int128 xpa = a.X - X;
        Int128 xpb = b.X - X;
        Int128 yca = a.Y - c.Y;
        Int128 ycb = b.Y - c.Y;
        Int128 ypa = a.Y - Y;
        Int128 ypb = b.Y - Y;

        var cos1 = (xca * xcb) + (yca * ycb);
        var cos2 = (xpb * xpa) + (ypb * ypa);

        switch ((Int128.IsNegative(cos1), Int128.IsNegative(cos2)))
        {
            case (true, true):
                // 1 > 90° und 2 > 90° == P im Umkreis = Flip sa-ta
                return true;
            case (false, false):
                // 1 <= 90° und 2 <= 90° == P sicher außerhalb bzw. auf Kreis (beide 90°) = kein Flip nötig
                return false;
            default:
                var msin1 = (xcb * yca) - (xca * ycb); // -sin1
                var sin2 = (xpb * ypa) - (xpa * ypb);
                return BigInteger.Multiply(cos1, sin2) < BigInteger.Multiply(msin1, cos2);
        }

    }

    /// <summary>
    /// Determines whether the current segment intersects or touches the specified segment from the right.
    /// </summary>
    /// <remarks>This method checks for an intersection or touch point between the current segment and the
    /// specified segment within the given fractional range of the other segment. The intersection or touch is
    /// considered valid only if it occurs from the right side of the current segment.</remarks>
    /// <param name="target">The endpoint of the current segment.</param>
    /// <param name="otherSource">The starting point of the other segment.</param>
    /// <param name="otherTarget">The endpoint of the other segment.</param>
    /// <param name="otherStart">The fractional position representing the start of the other segment.</param>
    /// <param name="otherEnd">The fractional position representing the end of the other segment.</param>
    /// <param name="otherPos">When this method returns <see langword="true"/>, contains the fractional position of the intersection or touch
    /// point on the other segment. If no intersection or touch occurs, this is set to the default value.</param>
    /// <returns><see langword="true"/> if the current segment intersects or touches the other segment from the right within the
    /// specified fractional range; otherwise, <see langword="false"/>.</returns>
    public bool IntersectsOrTouchesFromRight(in VecI target, in VecI otherSource, in VecI otherTarget,
     in Fraction otherStart, in Fraction otherEnd, out Fraction otherPos)
    {
        var sata = target - this;
        var sbtb = otherTarget - otherSource;
        var tbsa = this - otherTarget;
        Int128 den = sata ^ sbtb, numb, revnumb, numa;
        if (den > 0L
            && Int128.Sign(revnumb = tbsa ^ sata) >= 0
            && (numa = sbtb ^ tbsa) <= den
            && Int128.Sign(numa) >= 0
            && Int128.Sign(numb = den - revnumb) >= 0
            && Fraction.Create(numb, den, out var iposB)
            && iposB is Fraction posB
            && otherStart < posB
            && posB <= otherEnd)
        {
            otherPos = posB;
            return true;
        }
        otherPos = default;
        return false;
    }

    /// <summary>
    /// Determines the orientation of the edge defined by the current point and the specified <paramref name="target"/> 
    /// relative to the edge defined by <paramref name="otherSource"/> and <paramref name="otherTarget"/>.
    /// </summary>
    /// <param name="target">The endpoint of the edge originating from the current point.</param>
    /// <param name="otherSource">The starting point of the other edge.</param>
    /// <param name="otherTarget">The endpoint of the other edge.</param>
    /// <param name="sameDirection">When this method returns, contains <see langword="true"/> if the two edges point in the same general direction; 
    /// otherwise, <see langword="false"/>.</param>
    /// <returns>An integer indicating the orientation of the current edge relative to the other edge: <list type="bullet">
    /// <item><description><c>-1</c> if the current edge is counterclockwise relative to the other
    /// edge.</description></item> <item><description><c>0</c> if the two edges are collinear.</description></item>
    /// <item><description><c>1</c> if the current edge is clockwise relative to the other edge.</description></item>
    /// </list></returns>
    public int GetEdgeOrientation(in VecI target, in VecI otherSource, in VecI otherTarget, out bool sameDirection)
    {
        (long xt1, long yt1) = target;
        (long xs2, long ys2) = otherSource;
        (long xt2, long yt2) = otherTarget;

        long dx1 = xt1 - X;
        long dy1 = yt1 - Y;
        long dx2 = xt2 - xs2;
        long dy2 = yt2 - ys2;

        sameDirection = ((Int128)dx1 * dx2) + ((Int128)dy1 * dy2) > 0;
        return Int128.Sign(((Int128)dx1 * dy2) - ((Int128)dy1 * dx2));
    }

    /// <summary>
    /// Calculates the intersection position of this edge with another edge in terms of a fractional parameter.
    /// </summary>
    /// <param name="target">The endpoint of this edge, represented as a <see cref="VecI"/>.</param>
    /// <param name="otherSource">The starting point of the other edge, represented as a <see cref="VecI"/>.</param>
    /// <param name="otherTarget">The endpoint of the other edge, represented as a <see cref="VecI"/>.</param>
    /// <returns>A <see cref="Fraction"/> representing the relative position of the intersection along this edge. The value is
    /// normalized such that 0 represents the starting point of this edge, and 1 represents the endpoint.</returns>
    /// <exception cref="ArithmeticException">Thrown if the intersection calculation fails, such as when the edges are parallel or overlapping.</exception>
    public Fraction IntersectEdge(in VecI target, in VecI otherSource, in VecI otherTarget)
    {
        var sasb = otherSource - this;
        var sata = target - this;
        var sbtb = otherTarget - otherSource;

        var numa = sbtb ^ sasb;
        var den = sbtb ^ sata;

        // Denominator muss positiv sein
        if (den < 0)
        {
            numa = -numa;
            den = -den;
        }

        return !Fraction.Create(numa, den, out var position) 
            ? throw new ArithmeticException($"Intersection failed.") : position;
    }


}

public readonly record struct Epsilon(double Scale, double OriginX, double OriginY)
{
    public static bool Create(byte digits, BoundingBox box, out Epsilon epsilon)
    {
        if (digits > 28)
        {
            epsilon = default;
            return false;
        }
        double scale = scaleFromDigits(digits);
        double range = double.Max(box.RangeX, box.RangeY);
        if (double.Ceiling(range * scale) > long.MaxValue)
        {
            epsilon = default;
            return false;
        }
        epsilon = new Epsilon(scale, box.MinX, box.MinY);
        return true;
    }

    private static double scaleFromDigits(byte digits) => (double)(1m / new decimal(1, 0, 0, false, digits));

    public VecI Convert(double x, double y)
    {
        long xi = (long)double.Round((x - OriginX) * Scale);
        long yi = (long)double.Round((y - OriginY) * Scale);
        return new VecI(xi, yi);
    }


    public (double x, double y) Convert(VecI xy)
    {
        double x = xy.X / Scale + OriginX;
        double y = xy.Y / Scale + OriginY;
        return (x, y);
    }
}

public record BoundingBox
{
    public double MinX { get; private set; } = double.PositiveInfinity;

    public double MinY { get; private set; } = double.PositiveInfinity;

    public double MaxX { get; private set; } = double.NegativeInfinity;

    public double MaxY { get; private set; } = double.NegativeInfinity;

    public double RangeX => MaxX - MinX;

    public double RangeY => MaxY - MinY;

    public void Extend(double x, double y)
    {
        MinX = double.Min(MinX, x);
        MinY = double.Min(MinY, y);
        MaxX = double.Max(MaxX, x);
        MaxY = double.Max(MaxY, y);
    }

    public void Extend((double x, double y)[][][] multiPolygon)
    {
        foreach (var polygon in multiPolygon)
        {
            foreach (var contour in polygon)
            {
                foreach (var (x, y) in contour)
                {
                    Extend(x, y);
                }
            }
        }
    }

}

public readonly record struct Fraction(Int128 Num, Int128 Den) : IComparable<Fraction>, IEquatable<Fraction>
{
    public const double MaxDoubleInt = (double)(1L << 53);

    private static readonly Fraction zero = new(Int128.Zero, Int128.One);
    public static ref readonly Fraction Zero => ref zero;

    private static readonly Fraction one = new(Int128.One, Int128.One);
    public static ref readonly Fraction One => ref one;

    public bool IsNaN => Den == 0;

    public bool IsZero => Num == 0;

    public bool IsOne => Num == Den;

    public bool IsInt => IsZero || IsOne;

    public static bool Create(in Int128 numerator, in Int128 denominator, out Fraction fraction)
    {
        if (Int128.Sign(numerator) <= 0)
        {
            fraction = default;
            return false;
        }
        if (numerator > denominator)
        {
            fraction = default;
            return false;
        }

        fraction = numerator == 0 ? Zero
            : numerator == denominator ? One
            : Gcd(numerator, denominator, out var div)
            ? new Fraction(numerator / div, denominator / div)
            : new Fraction(numerator, denominator);

        return true;
    }

    public static bool Create(in long numerator, in long denominator, out Fraction fraction)
    {
        if (numerator <= 0L)
        {
            fraction = default;
            return false;
        }
        if (numerator > denominator)
        {
            fraction = default;
            return false;
        }

        fraction = numerator == 0L ? Zero
            : numerator == denominator ? One
            : Gcd(numerator, denominator, out long div)
            ? new Fraction(numerator / div, denominator / div)
            : new Fraction(numerator, denominator);

        return true;
    }

    internal static bool Gcd(in Int128 num, in Int128 den, out Int128 div)
    {
        // Euclidean algorithm to find the gcd
        div = den;
        var r = num;
        while (r != 0)
        {
            (div, r) = (r, div % r);
        }

        return !(div == 0 || div == 1);
    }

    internal static bool Gcd(in long num, in long den, out long div)
    {
        // Euclidean algorithm to find the gcd
        div = den;
        long r = num;
        while (r != 0L)
        {
            (div, r) = (r, div % r);
        }

        return div is not (0L or 1L);
    }

    public void Deconstruct(out Int128 num, out Int128 den) => (num, den) = (Num, Den);

    public Fraction Reverse() => new(Den - Num, Den);

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Comp(in Fraction a, in Fraction b)
    {
        (Int128 ad, Int128 an) = a;
        (Int128 bd, Int128 bn) = b;
        // Comparison as continued fraction
        int retVal = -1;
        do
        {
            Int128 qa = an / ad;
            Int128 qb = bn / bd;

            // compare if quotients different
            if (qa != qb)
            {
                return qa < qb ? -retVal : retVal;
            }

            // change return sign
            retVal = -retVal;

            // new remainders
            (an, ad) = (ad, an % ad);
            (bn, bd) = (bd, bn % bd);
        } while (ad != 0 && bd != 0);

        return ad == bd ? 0 :
            ad == 0 ? retVal : -retVal;
    }

    public int CompareTo(in Fraction other)
    {
        return Den == other.Den ? Num.CompareTo(other.Num) :
        Num == other.Num ? other.Den.CompareTo(Den) :
        IsZero ? other.IsZero ? 0 : -1 :
        IsOne ? other.IsOne ? 0 : 1 :
        other.IsZero ? 1 :
        other.IsOne ? -1 : Comp(this, other);
    }

    int IComparable<Fraction>.CompareTo(Fraction other) => CompareTo(other);

    public bool Equals(in Fraction other) => (Den == other.Den) && (Num == other.Num);

    public override int GetHashCode() => HashCode.Combine(Num, Den);

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Fraction Mul(in Fraction other)
    {
        Int128 an = Num;
        Int128 ad = Den;
        Int128 bn = other.Num;
        Int128 bd = other.Den;

        if (Gcd(bn, ad, out var q))
        {
            bn /= q;
            ad /= q;
        }

        if (Gcd(an, bd, out q))
        {
            an /= q;
            bd /= q;
        }

        return new Fraction(an * bn, ad * bd);
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public long Mul(in long other, out Fraction rem)
    {
        Int128 an = Num;
        Int128 ad = Den;
        Int128 bn = Math.Abs(other);

        if (Gcd(bn, ad, out var q))
        {
            bn /= q;
            ad /= q;
        }
        var (integer, irem) = Int128.DivRem(bn * an, ad);
        rem = new Fraction(irem, ad);

        if (rem.IsOne)
        {
            rem = Zero;
            integer++;
        }

        if (other < 0)
        {
            integer = -(integer + 1);
            rem = rem.Reverse();
        }

        return (long)integer;
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public long Round()
    {
        var dn = Num + Num;
        int comp = dn.CompareTo(Den);
        return comp < 0 ? 0 : comp > 0 ? 1 : Int128.IsEvenInteger(dn) ? 0 : 1;
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Fraction Div(in Fraction other)
    {
        if (other.IsOne || IsZero)
            return this;
        if (other.IsZero)
            throw new DivideByZeroException();
        if (IsOne)
            throw new ArithmeticException($"Fraction invalid, The numerator ({other.Den}) is greater as the denominator ({other.Num}).");

        Int128 an = Num;
        Int128 ad = Den;
        Int128 bn = other.Den;
        Int128 bd = other.Num;

        if (Gcd(bn, ad, out var q))
        {
            bn /= q;
            ad /= q;
        }

        if (Gcd(an, bd, out q))
        {
            an /= q;
            bd /= q;
        }

        return new Fraction(an * bn, ad * bd);
    }

    public static Fraction operator *(in Fraction a, in Fraction b) => a.Mul(b);

    public static Fraction operator /(in Fraction a, in Fraction b) => a.Div(b);

    public static bool operator ==(in Fraction a, in Fraction b) => a.Equals(b);

    public static bool operator !=(in Fraction a, in Fraction b) => !a.Equals(b);

    public static bool operator <(in Fraction a, in Fraction b) => a.CompareTo(b) < 0;

    public static bool operator >(in Fraction a, in Fraction b) => a.CompareTo(b) > 0;

    public static bool operator <=(in Fraction a, in Fraction b) => a.CompareTo(b) <= 0;

    public static bool operator >=(in Fraction a, in Fraction b) => a.CompareTo(b) >= 0;

    public override string ToString() => $"{S(6)} ({Num}/{Den})";

    public string S(int decimals)
    {
        if (decimals < 1)
        {
            return (Num / Den).ToString();
        }

        string s = Num == Den ? "1." : "0.";
        if (IsZero || IsOne)
        {
            return s.PadRight(decimals, '0');
        }
        var n = Num;
        Int128 c;
        for (int i = 0; i < decimals; i++)
        {
            n *= 10;
            (c, n) = Int128.DivRem(n, Den);
            s += c.ToString();
        }
        return s;
    }

    public double Value
    {
        get
        {
            if (Den == 0)
                return double.NaN;
            if (Num == 0)
                return 0d;
            if (Num == Den)
                return 1d;

            var s = 0d;
            Int128 n = Num;
            Int128 c;
            var scale = 1d;
            while (scale < MaxDoubleInt || n != 0)
            {
                n *= 10;
                (c, n) = Int128.DivRem(n, Den);
                s += (double)c * scale;
                scale *= 10d;
            }
            return s / scale;
        }
    }

}