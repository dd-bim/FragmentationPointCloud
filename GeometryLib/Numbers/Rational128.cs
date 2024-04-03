using BigMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeometryLib.Numbers
{
    public readonly record struct Rational128
    {
        public readonly Int128 Int;

        public readonly Fraction128 Frac;

        private static Rational128 zero = new Rational128(Int128.Zero, Fraction128.Zero);
        public static ref readonly Rational128 Zero => ref zero;


        public Rational128(Int128 integer, Fraction128 frac)
        {
            Int = integer;
            Frac = frac;
            if(integer.Sign < 0 && frac.IsOne)
            {
                Int++;
                Frac = Fraction128.Zero;
            }
        }
        public Rational128(Int128 integer)
        {
            Int = integer;
            Frac = Fraction128.Zero;
        }

        public bool Add(Int128 other, out Rational128 result)
        {
            checked
            {
                try
                {
                    result = new Rational128(Int + other, Frac);
                    return true;
                }
                catch (Exception)
                {
                    result = default;
                    return false;
                }
            }
        }

        public bool Add(Rational128 other, out Rational128 result)
        {
            checked
            {
                try
                {
                    if(!Frac.Add(other.Frac, out var sum))
                    {
                        result = default;
                        return false;
                    }
                    result = new Rational128(Int + other.Int, sum);
                    return true;
                }
                catch (Exception)
                {
                    result = default;
                    return false;
                }
            }
        }


    }
}
