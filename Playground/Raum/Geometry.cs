using DelaunatorSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Playground.Raum.Geometry;

public readonly record struct VecI(long X, long Y):IPoint
{
    public VecI(Epsilon epsilon, double x, double y) 
        :this((long)((x - epsilon.X) * epsilon.Scale), (long)((y - epsilon.Y) * epsilon.Scale)) { }

    double IPoint.X
    {
        get => (double)X;
        set { throw new NotSupportedException(); }
    }

    double IPoint.Y
    {
        get => (double)Y;
        set { throw new NotSupportedException(); }
    }

    public (double x, double y) ToDouble(Epsilon epsilon) =>
        (X / epsilon.Scale + epsilon.X, Y / epsilon.Scale + epsilon.Y);
}

public readonly record struct Epsilon(double Scale, double X, double Y)
{
    public Epsilon(byte digits, BoundingBox box) : this(scaleFromDigits(digits), box.MinX, box.MinY);

    private static double scaleFromDigits(byte digits)
    {
        return (double)(1m / new decimal(1, 0, 0, false, digits));
    }
}

public record BoundingBox
{
    private const long MaxValidDouble = 1L << 53;


    public double MinX { get; private set; } = double.PositiveInfinity;

    public double MinY { get; private set; } = double.PositiveInfinity;

    public double MaxX { get; private set; } = double.NegativeInfinity;

    public double MaxY { get; private set; } = double.NegativeInfinity;

    public void Extend(double x, double y) { 
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
    private static readonly Fraction zero = new(Int128.Zero, Int128.One);
    public static ref readonly Fraction Zero => ref zero;

    private static readonly Fraction one = new(Int128.One, Int128.One);
    public static ref readonly Fraction One => ref one;

    public bool IsNaN => Den == 0;

    public bool IsZero => Num == 0;

    public bool IsOne => Num == Den;

    public bool IsInt => IsZero || IsOne;

    public static bool Create(in Int128 numerator, in Int128 denominator, out Fraction fraction, bool log)
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

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal static bool Gcd(in Int128 num, in Int128 den, out Int128 div)
    {
        // Euclidean algorithm to find the gcd
        div = den;
        Int128 r = num;
        while (r != 0)
        {
            (div, r) = (r, div % r);
        }

        return !(div == 0 || div == 1);
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
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
 
    public void Deconstruct(out Int128 num, out Int128 den) { (num, den) = (Num, Den); }

    public Fraction Reverse()
    {
        return new(Den - Num, Den);
    }

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

    int IComparable<Fraction>.CompareTo(Fraction other)
    {
        return CompareTo(other);
    }

    public bool Equals(in Fraction other)
    {
        return (Den == other.Den) && (Num == other.Num);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Num, Den);
    }

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

    public override string ToString()
    {
        return $"{S(6)} ({Num}/{Den})";
    }

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
}