using System;
using System.Globalization;

using I = GeometryLib.Int32.D2;

/* Unmerged change from project 'GeometryLib (netstandard2.1)'
Before:
using I = GeometryLib.Int32.D2;
After:
using N = GeometryLib.Numbers;
*/


using N = GeometryLib.Numbers;



namespace GeometryLib.Int32.Fraction.D2
{
    public readonly struct Vector : IEquatable<Vector>, IEquatable<I.Vector>, IIntegerPoint
    {
        internal readonly ulong NumX, NumY, Den;

        public int x { get; }
        public int y { get; }


        public double DoubleX => x + (double) NumX / Den;

        public double DoubleY => y + (double) NumY / Den;

        public decimal DecimalX => x + (decimal)NumX / Den;

        public decimal DecimalY => y + (decimal)NumY / Den;

        #region Constructors

        //public Vector(I.Vector p)
        //{
        //    x = p.x;
        //    y = p.y;
        //    NumX = 0;
        //    NumY = 0;
        //    Den = 1;
        //}

        private Vector(int x, int y, ulong numX, ulong numY, ulong den)
        {
            this.x = x;
            this.y = y;
            this.NumX = numX;
            this.NumY = numY;
            this.Den = den;
        }

        public static IIntegerPoint Create(in I.Vector start, in I.Vector dest, in N.Fraction pos)
        {
            int dx = dest.x - start.x, sx;
            int dy = dest.y - start.y, sy;
            (sx, dx) = dx < 0 ? (-1, -dx) : (1, dx);
            (sy, dy) = dy < 0 ? (-1, -dy) : (1, dy);
            // calculate nearest integer positions and remainders
            ulong remX, remY;
            if (pos.Num > int.MaxValue || pos.Den > long.MaxValue)
            {
                // use decimal to avoid overflow
                var dxn = (decimal) dx * pos.Num;
                var dyn = (decimal) dy * pos.Num;
                remX = (ulong) (dxn % pos.Den);
                remY = (ulong) (dyn % pos.Den);
                dx = (int) (dxn / pos.Den);
                dy = (int) (dyn / pos.Den);
            }
            else
            {
                dx = (int) Math.DivRem(dx * (long) pos.Num, (long) pos.Den, out var lremX);
                dy = (int) Math.DivRem(dy * (long) pos.Num, (long) pos.Den, out var lremY);
                remX = (ulong) lremX;
                remY = (ulong) lremY;
            }

            // adjust if negative because Fraction is always positive
            if (sx < 0 && remX != 0)
            {
                dx++;
                remX = pos.Den - remX;
            }

            if (sy < 0 && remY != 0)
            {
                dy++;
                remY = pos.Den - remY;
            }

            var x = start.x + sx * dx;
            var y = start.y + sy * dy;

            if(remX == 0 && remY == 0)
            {
                return new I.Vector(x, y);
            }

            // reduce Fractions if possible, to make equality unique
            var r = N.Fraction.Gcd(remX, N.Fraction.Gcd(remY, pos.Den));

            return new Vector(x,y, remX / r, remY / r, pos.Den / r);
        }

        #endregion

        public int RoundX => x + (Den - NumX > NumX ? 0 : 1);

        public int RoundY => y + (Den - NumY > NumY ? 0 : 1);

        public I.Vector Ceiling => new I.Vector(x + (NumX == 0 ? 0 : 1), y + (NumY == 0 ? 0 : 1));


        public (int x, int y) xy => (x, y);

        public bool Equals(Vector other)
        {
            return x == other.x && y == other.y && NumX == other.NumX && NumY == other.NumY && Den == other.Den;
        }

        public bool Equals(I.Vector other) => false;

        public bool Equals(IIntegerPoint? other)
        {
            return other is Vector fvec ? Equals(fvec) : (other is I.Vector ivec && Equals(ivec));
        }

        public override bool Equals(object? obj)
        {
            return obj is Vector fvec ? Equals(fvec) : (obj is I.Vector ivec && Equals(ivec));
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
#if NETSTANDARD2_0 || NET472 || NET48 || NET462
            return x ^ y ^ NumX.GetHashCode() ^ NumY.GetHashCode() ^ Den.GetHashCode();
#else
            return HashCode.Combine(x, y, NumX, NumY, Den);
#endif
        }

        public void Deconstruct(out int outX, out int outY)
        {
            outX = x;
            outY = y;
        }

        public override string ToString()
        {
            return ToString(" ");//$"{x} {y}+({NumX} {NumY})/{Den}";
        }

        public string ToString(string separator)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:G17}{2}{1:G17}", DoubleX, DoubleY, separator);
        }

        public string ToWktString()
        {
            return $"POINT({ToString()})";
        }

        public Double.D2.Vector ToDouble()
        {
            return new Double.D2.Vector(DoubleX, DoubleY);
        }

        public static bool operator ==(in Vector left, in Vector right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(in Vector left, in Vector right)
        {
            return !left.Equals(right);
        }

        public static bool operator ==(in I.Vector left, in Vector right)
        {
            return right.Equals(left);
        }

        public static bool operator !=(in I.Vector left, in Vector right)
        {
            return !right.Equals(left);
        }

        public static bool operator ==(in Vector left, in I.Vector right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(in Vector left, in I.Vector right)
        {
            return !left.Equals(right);
        }
    }
}

//private static (BigInteger x, BigInteger y) ToBig(in Vector v, in BigInteger den, in ulong den1, in ulong den2)
//{
//    var fac = BigInteger.Multiply(den1, den2);
//    var x = v.x * den + v.NumX * fac;
//    var y = v.y * den + v.NumY * fac;
//    return (x, y);
//}

//private static BigInteger Det(BigInteger ax, BigInteger ay, BigInteger bx, BigInteger by)
//{
//    return (ax * by) - (ay * bx);
//}

//private static BigInteger Dot(BigInteger ax, BigInteger ay, BigInteger bx, BigInteger by)
//{
//    return (ax * bx) + (ay * by);
//}

//public bool IsBetween(in Vector a, in Vector b)
//{
//    var den = BigInteger.Multiply(Den, a.Den) * b.Den;
//    var pv = ToBig(this, den, a.Den, b.Den);
//    var av = ToBig(a, den, Den, b.Den);
//    var bv = ToBig(b, den, a.Den, Den);

//    var abx = bv.x - av.x;
//    var aby = bv.y - av.y;
//    var apx = pv.x - av.x;
//    var apy = pv.y - av.y;

//    if (Det(abx, aby, apx, apy) != 0) 
//        return false;

//    var lnum = Dot(abx, aby, apx, apy);
//    var lden = Dot(abx, aby, abx, aby);

//    return lnum > 0 && lnum < lden;
//}


//public bool IsBetween(in IIntegerPoint a, in IIntegerPoint b)
//{
//    switch ((a,b))
//    {
//        case (I.Vector av, I.Vector bv):
//            if (IsInteger)
//            {
//                return new I.Vector(x,y).IsBetween(av,bv);
//            }
//            return IsBetween(new Vector(av), new Vector(bv));
//        case (I.Vector av, Vector bv):
//            if (IsInteger && bv.IsInteger)
//            {
//                return new I.Vector(x, y).IsBetween(av, new I.Vector(bv.x,bv.y));
//            }
//            return IsBetween(new Vector(av), bv);
//        case (Vector av, I.Vector bv):
//            if (IsInteger && av.IsInteger)
//            {
//                return new I.Vector(x, y).IsBetween(new I.Vector(av.x, av.y), bv);
//            }
//            return IsBetween(av, new Vector(bv));
//        case (Vector av, Vector bv):
//            if (IsInteger && av.IsInteger && bv.IsInteger)
//            {
//                return new I.Vector(x, y).IsBetween(new I.Vector(av.x, av.y), new I.Vector(bv.x, bv.y));
//            }
//            return IsBetween(av, bv);
//        default:
//            throw new NotImplementedException();
//    }
//}

//public int EdgeSign(in Vector a, in Vector b)
//{
//    var den = BigInteger.Multiply(Den, a.Den) * b.Den;
//    var pv = ToBig(this, den, a.Den, b.Den);
//    var av = ToBig(a, den, Den, b.Den);
//    var bv = ToBig(b, den, a.Den, Den);

//    return ((av.x - pv.x) * (bv.y - pv.y) - (av.y - pv.y) * (bv.x - pv.x)).Sign;
//}

//public int EdgeSign(in IIntegerPoint a, in IIntegerPoint b)
//{
//    switch ((a, b))
//    {
//        case (I.Vector av, I.Vector bv):
//            if (IsInteger)
//                return new I.Vector(x, y).EdgeSign(av, bv);
//            return EdgeSign(new Vector(av), new Vector(bv));
//        case (I.Vector av, Vector bv):
//            if (IsInteger && bv.IsInteger)
//                return new I.Vector(x, y).EdgeSign(av, new I.Vector(bv.x, bv.y));
//            return EdgeSign(new Vector(av), bv);
//        case (Vector av, I.Vector bv):
//            if (IsInteger && av.IsInteger)
//                return new I.Vector(x, y).EdgeSign(new I.Vector(av.x, av.y), bv);
//            return EdgeSign(av, new Vector(bv));
//        case (Vector av, Vector bv):
//            if (IsInteger && av.IsInteger && bv.IsInteger)
//                return new I.Vector(x, y).EdgeSign(new I.Vector(av.x, av.y), new I.Vector(bv.x, bv.y));
//            return EdgeSign(av, bv);
//        default:
//            throw new NotImplementedException();
//    }
//}

//public bool InCircle(in IIntegerPoint a, in IIntegerPoint b, in IIntegerPoint c)
//{
//    throw new NotImplementedException();
//}
