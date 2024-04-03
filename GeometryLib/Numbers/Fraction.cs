using System;
using System.Collections.Generic;
using System.Numerics;

namespace GeometryLib.Numbers
{
    /// <summary> Positive Rational Number between 0 and 1 </summary>
    public readonly struct Fraction : IComparable<Fraction>, IEquatable<Fraction>
    {
        public readonly ulong Num;

        public readonly ulong Den;

        private const ulong MaxDbl = 1L << 53;

        public static readonly Fraction Zero = new Fraction(0, 1);

        public static readonly Fraction One = new Fraction(1, 1);

        public bool IsZero => Num == 0;

        public bool IsOne => Num == Den;

        public static Comparer<Fraction> Desc => Comparer<Fraction>.Create((a, b) => b.CompareTo(a));

        /// <summary> Scalar value of rational number </summary>
        public double S
        {
            get
            {
                double s;
                unchecked
                {
                    s = Den > MaxDbl ? Math.Exp(BigInteger.Log(Num) - BigInteger.Log(Den)) : (double)Num / Den;
                }

                return s;
            }
        }

        public Fraction(ulong numerator, ulong denominator)
        {
            (Num, Den) = (denominator == 0) || (numerator > denominator) ?
                throw new ArithmeticException("Invalid Fraction") :
                numerator == 0 ? (0, 1) :
                    numerator == denominator ? (1, 1) : Divide(numerator, denominator, Gcd(numerator, denominator));
        }

        internal Fraction((BigInteger num, BigInteger den) fraction)
        {
            var fac = BigInteger.GreatestCommonDivisor(fraction.num, fraction.den);
            Num = (ulong)(fraction.num / fac);
            Den = (ulong)(fraction.den / fac);
            if ((Den <= 0) || (Num > Den))
            {
                throw new ArithmeticException("Invalid Fraction");
            }
        }


        internal static ulong Gcd(in ulong num, in ulong den)
        {
            // Euclidean algorithm to find the gcd
            var q = den;
            var r = num;
            while (r > 0)
            {
                (q, r) = (r, q % r);
            }

            return Math.Max(1UL, q);
        }

        public void Deconstruct(out ulong num, out ulong den) { (num, den) = (Num, Den); }

        //       public void Deconstruct(out BigInteger num, out BigInteger den) => (num, den) = (Num, Den);

        private static (ulong, ulong) Divide(in ulong num, in ulong den, in ulong div) => (num / div, den / div);

        public Fraction Reverse() => new Fraction(Den - Num, Den);

   //     public int Rounded => (Den - Num) > Num ? 0 : 1;

        public (ulong num, ulong den) Tuple => (Num, Den);

        public (ulong num, ulong den) TupleReverse => (Den - Num, Den);

        private static int Comp(ulong ad, ulong an, ulong bd, ulong bn)
        {
            // Comparison as continued fraction
            var retVal = -1;
            do
            {
                var qa = an / ad;
                var qb = bn / bd;

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
            } while ((ad != 0) && (bd != 0));

            return ad == bd ? 0 :
                ad == 0 ? retVal : -retVal; // NOTE: check correctness
        }

        public int CompareTo(Fraction other) =>
            Den == other.Den ? Num.CompareTo(other.Num) :
            Num == other.Num ? other.Den.CompareTo(Den) :
            IsZero ? other.IsZero ? 0 : -1 :
            IsOne ? other.IsOne ? 0 : 1 :
            other.IsZero ? 1 :
            other.IsOne ? -1 : Comp(Num, Den, other.Num, other.Den);

        public bool Equals(Fraction other) => (Den == other.Den) && (Num == other.Num);

        public override bool Equals(object? obj) => obj is Fraction other && Equals(other);

        public override int GetHashCode()
        {
#if NETSTANDARD2_0 || NET472 || NET48 || NET462
            return Num.GetHashCode() ^ Den.GetHashCode();
#else
            return HashCode.Combine(Num, Den);
#endif
        }

        public static bool operator ==(Fraction left, Fraction right) => left.Equals(right);

        public static bool operator !=(Fraction left, Fraction right) => !left.Equals(right);

        public static bool operator <(Fraction left, Fraction right) => left.CompareTo(right) < 0;

        public static bool operator >(Fraction left, Fraction right) => left.CompareTo(right) > 0;

        public static bool operator <=(Fraction left, Fraction right) => left.CompareTo(right) <= 0;

        public static bool operator >=(Fraction left, Fraction right) => left.CompareTo(right) >= 0;

        public override string ToString() => $"{S:f3} ({Num}/{Den})";

        internal static (BigInteger num, BigInteger den) Mul((BigInteger num, BigInteger den) a,
            (BigInteger num, BigInteger den) b) =>
            (a.num * b.num, a.den * b.den);

        internal static (BigInteger num, BigInteger den) Div((BigInteger num, BigInteger den) a,
            (BigInteger num, BigInteger den) b) =>
            (a.num * b.den, a.den * b.num);

        internal static (BigInteger num, BigInteger den) Add((BigInteger num, BigInteger den) a,
            (BigInteger num, BigInteger den) b) =>
            ((a.num * b.den) + (b.num * a.den), a.den * b.den);

        internal static (BigInteger num, BigInteger den) Sub((BigInteger num, BigInteger den) a,
            (BigInteger num, BigInteger den) b) =>
            ((a.num * b.den) - (b.num * a.den), a.den * b.den);


    }
}
