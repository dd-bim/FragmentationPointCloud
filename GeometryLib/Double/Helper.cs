using System;
using System.Collections.Generic;

using static System.Math;

namespace GeometryLib.Double
{
    public static class Helper
    {
#if NETSTANDARD2_0 || NETSTANDARD2_1 || NET48
        public static double FusedMultiplyAdd(double x, double y, double z) => (x * y) + z;

        public static double ScaleB(double x, int n) => n >= 0 ? x * (1L << n) : x / (1L << -n);
#endif

        //private static readonly double fs = Math.Sqrt(3 / 8.0);
        //private static readonly double a0 = 15 / 8.0;
        //private static readonly double a1 = Math.Sqrt(25 / 6.0);

        //private const double t60 =  +3003 / 1024.0;
        //private const double t61 =  -6006 / 1024.0;
        //private const double t62 =  +9009 / 1024.0;
        //private const double t63 =  -8580 / 1024.0;
        //private const double t64 =  +5005 / 1024.0;
        //private const double t65 =  -1638 / 1024.0;
        //private const double t66 =   +231 / 1024.0;

        //private static double ApproxRSqrt(in double s)
        //{
        //    var ts = fs * s;
        //    var y = ts * s * s;
        //    var x = s * (a0 - y * (a1 - y));
        //    y = ts * x * x;
        //    //x *= a0 - y * (a1 - y);
        //    //y = ts * x * x;
        //    //x *= a0 - y * (a1 - y);
        //    //y = ts * x * x;
        //    return x * (a0 - y * (a1 - y));
        //}


        //public static double ApproxRSqrt0(double s) => 1.0 / Sqrt(s);

        //public static double ApproxRSqrt1(double s)
        //{
        //    var ts = fs * s;
        //    var y = ts * s * s;
        //    var x = s * (a0 - y * (a1 - y));
        //    y = ts * x * x;
        //    //x *= a0 - y * (a1 - y);
        //    //y = ts * x * x;
        //    //x *= a0 - y * (a1 - y);
        //    //y = ts * x * x;
        //    return x * (a0 - y * (a1 - y));
        //}

        //public static double ApproxRSqrt2(double s) => s * (s * (s * (s * (s * (t66 * s + t66) + t64) + t63) + t62) + t61) + t60;

        //public static double ApproxRSqrt3(double s) => t60 + s * (t61 + s * (t62 + s * (t63 + s * (t64 + s * (t65 + s * t66)))));

        //public static double ApproxRSqrt4(double s) =>
        //    FusedMultiplyAdd(s, FusedMultiplyAdd(s, FusedMultiplyAdd(s, 
        //    FusedMultiplyAdd(s, FusedMultiplyAdd(s, FusedMultiplyAdd(s, 
        //        t66, t65), t64),t63),t62),t61),t62);



        public static (double x, double y) xy(in (double x, double y, double z) value) => (value.x, value.y);

        public static (double y, double z) yz(in (double x, double y, double z) value) => (value.y, value.z);

        public static (double z, double x) zx(in (double x, double y, double z) value) => (value.z, value.x);

        public static void Permute(int[] idxs, double[] vals)
        {
            Array.Sort(idxs, vals);
        }

        public static void Sort(int ia, int ib, int[] idxs, double[] vals)
        {
            (idxs[ia], idxs[ib], vals[ia], vals[ib]) =
                vals[ia] > vals[ib]
                ? (idxs[ib], idxs[ia], vals[ib], vals[ia])
                : (idxs[ia], idxs[ib], vals[ia], vals[ib]);
        }

        public static void Sort3(int[] idxs, double[] vals)
        {
            Sort(0, 1, idxs, vals);
            Sort(1, 2, idxs, vals);
            Sort(0, 1, idxs, vals);
        }

        public static void Sort4(int[] idxs, double[] vals)
        {
            Sort(0, 2, idxs, vals);
            Sort(1, 3, idxs, vals);
            Sort(0, 1, idxs, vals);
            Sort(2, 3, idxs, vals);
            Sort(1, 2, idxs, vals);
        }


        /// <summary>
        /// Sorts 2 doubles ascending
        /// </summary>
        public static void Sort(ref double a, ref double b) => (a, b) = a > b ? (b, a) : (a, b);

        public static void SortByAbsoluteValue(ref double absa, ref double a, ref double absb, ref double b) => (absa, a, absb, b) = absa > absb ? (absb, b, absa, a) : (absa, a, absb, b);

        /// <summary>
        /// Sorts 3 doubles ascending
        /// </summary>
        public static void Sort(ref double a, ref double b, ref double c, bool byAbsoluteValue = false)
        {
            if (byAbsoluteValue)
            {
                var absa = Math.Abs(a);
                var absb = Math.Abs(b);
                var absc = Math.Abs(c);
                SortByAbsoluteValue(ref absa, ref a, ref absb, ref b);
                SortByAbsoluteValue(ref absb, ref b, ref absc, ref c);
                SortByAbsoluteValue(ref absa, ref a, ref absb, ref b);
            }
            else
            {
                Sort(ref a, ref b);
                Sort(ref b, ref c);
                Sort(ref a, ref b);
            }
        }

        public static void Sort(ref double a, ref double b, ref double c, ref double d, bool byAbsoluteValue = false)
        {
            if (byAbsoluteValue)
            {
                var absa = Math.Abs(a);
                var absb = Math.Abs(b);
                var absc = Math.Abs(c);
                var absd = Math.Abs(d);
                SortByAbsoluteValue(ref absa, ref a, ref absc, ref c);
                SortByAbsoluteValue(ref absb, ref b, ref absd, ref d);
                SortByAbsoluteValue(ref absa, ref a, ref absb, ref b);
                SortByAbsoluteValue(ref absc, ref c, ref absd, ref d);
                SortByAbsoluteValue(ref absb, ref b, ref absc, ref c);
            }
            else
            {
                Sort(ref a, ref c);
                Sort(ref b, ref d);
                Sort(ref a, ref b);
                Sort(ref c, ref d);
                Sort(ref b, ref c);
            }
        }

        public static double Normalize(ref double a, ref double b)
        {
            var iab = new[] { 0, 1 };
            var aab = new[] { Math.Abs(a), Math.Abs(b) };
            Sort(0, 1, iab, aab);
            var length = aab[1];
                 
            if (aab[0] == 0.0)
            {
                if (aab[1] == 0.0 || double.IsNaN(aab[1]))
                {
                    aab[0] = double.NaN;
                    aab[1] = double.NaN;
                }
                else
                {
                    aab[1] = 1.0;
                }
            }
            else if (double.IsPositiveInfinity(aab[1]))
            {
                aab[0] = 0.0;
                aab[1] = 1.0;
            }
            else
            {
                aab[0] /= aab[1];
                var sq = Sqrt(aab[0] * aab[0] + 1.0);
                length *= sq;
                aab[1] = 1.0 / sq;
                //aab[1] *= (3.0 - sq * aab[1] * aab[1]) * 0.5;
                aab[0] *= aab[1];
            }
            Permute(iab, aab);
            a = a < 0 ? -aab[0] : aab[0];
            b = b < 0 ? -aab[1] : aab[1];

            return length;
        }
        public static double Normalize(ref double a, ref double b, ref double c)
        {
            // Spezialbehandlung wenn alle gleich, um perp zu ermöglichen, sollte extrem selten sein!
            var s = a + b + c;
            if (s != 0 
                && Abs(a * 3 / s - 1) < Constants.TRIGTOL 
                && Abs(b * 3 / s - 1) < Constants.TRIGTOL
                && Abs(c * 3 / s - 1) < Constants.TRIGTOL )
            {
                var l = Hypot(a, b, c);
                a = Constants.RSQRT3 - Constants.TRIGTOL;
                b = Constants.RSQRT3;
                c = Constants.RSQRT3 + Constants.TRIGTOL;
                return l;
            }

            var iabc = new[] { 0, 1, 2 };
            var aabc = new[] { Math.Abs(a), Math.Abs(b), Math.Abs(c) };
            Sort3(iabc, aabc);
            var length = aabc[2];

            if (aabc[1] == 0.0)
            {
                if (aabc[2] == 0.0 || double.IsNaN(aabc[2]))
                {
                    aabc[0] = aabc[1] = aabc[2] = double.NaN;
                }
                else
                {
                    aabc[2] = 1.0;
                }
            }
            else if (double.IsPositiveInfinity(aabc[2]))
            {
                aabc[0] = aabc[1] = 0.0;
                aabc[2] = 1.0;
            }
            else
            {
                aabc[0] /= aabc[2];
                aabc[1] /= aabc[2];
                var sq = Sqrt(aabc[0] * aabc[0] + aabc[1] * aabc[1] + 1.0);
                length *= sq;
                aabc[2] = 1.0 / sq;
                aabc[0] *= aabc[2];
                aabc[1] *= aabc[2];
            }

            Permute(iabc, aabc);
            a = a < 0 ? -aabc[0] : aabc[0];
            b = b < 0 ? -aabc[1] : aabc[1];
            c = c < 0 ? -aabc[2] : aabc[2];

            return length;
        }

        public static void Normalize(ref double a, ref double b, ref double c, ref double d)
        {
            var iabcd = new[] { 0, 1, 2, 3 };
            var aabcd = new[] { Math.Abs(a), Math.Abs(b), Math.Abs(c), Math.Abs(d) };
            Sort4(iabcd, aabcd);

            if (aabcd[2] == 0.0)
            {
                if (aabcd[3] == 0.0 || double.IsNaN(aabcd[3]))
                {
                    aabcd[0] = aabcd[1] = aabcd[2] = aabcd[3] = double.NaN;
                }
                else
                {
                    aabcd[3] = 1.0;
                }
            }
            else if (double.IsPositiveInfinity(aabcd[3]))
            {
                aabcd[0] = aabcd[1] = aabcd[2] = 0.0;
                aabcd[3] = 1.0;
            }
            else
            {
                aabcd[0] /= aabcd[3];
                aabcd[1] /= aabcd[3];
                aabcd[2] /= aabcd[3];
                aabcd[3] = 1.0 / Sqrt(aabcd[0] * aabcd[0] + aabcd[1] * aabcd[1] + aabcd[2] * aabcd[2] + 1.0);
                aabcd[0] *= aabcd[3];
                aabcd[1] *= aabcd[3];
                aabcd[2] *= aabcd[3];
            }
            Permute(iabcd, aabcd);
            a = a < 0 ? -aabcd[0] : aabcd[0];
            b = b < 0 ? -aabcd[1] : aabcd[1];
            c = c < 0 ? -aabcd[2] : aabcd[2];
            d = d < 0 ? -aabcd[3] : aabcd[3];
        }


        public static double Hypot(double a, double b, bool absSort = true)
        {
            if (absSort)
            {
                a = Math.Abs(a);
                b = Math.Abs(b);
                Sort(ref a, ref b);
            }
            if (a == 0.0)
            {
                return b;
            }
            else if (double.IsPositiveInfinity(b) && !double.IsNaN(a))
            {
                return double.PositiveInfinity;
            }
            else
            {
                a /= b;
                return b * Sqrt(a * a + 1.0);
            }
        }

        public static double Hypot((double a, double b, double c) vector) => Hypot(vector.a, vector.b, vector.c);

        public static double Hypot(double a, double b, double c, bool absSort = true)
        {
            if (absSort)
            {
                a = Math.Abs(a);
                b = Math.Abs(b);
                c = Math.Abs(c);
                Sort(ref a, ref b, ref c);
            }
            if (a == 0.0)
            {
                return b == 0.0 ? c : Hypot(b, c, false);
            }
            else if (double.IsPositiveInfinity(c) && !double.IsNaN(a) && !double.IsNaN(b))
            {
                return double.PositiveInfinity;
            }
            else
            {
                a /= c;
                b /= c;
                return c * Sqrt((a * a + b * b) + 1.0);
            }
        }

        public static double Hypot(double a, double b, double c, double d)
        {
            a = Abs(a);
            b = Abs(b);
            c = Abs(c);
            d = Abs(d);
            Sort(ref a, ref b, ref c, ref d);
            if (a == 0.0)
            {
                return b == 0.0 ? Hypot(c, d, false) : Hypot(b, c, d, false);
            }
            else if (double.IsPositiveInfinity(d) && !double.IsNaN(a) && !double.IsNaN(b) && !double.IsNaN(c))
            {
                return double.PositiveInfinity;
            }
            else
            {
                a /= d;
                b /= d;
                c /= d;
                return d * Sqrt(((a * a + b * b) + c * c) + 1.0);
            }
        }

        public static double Sum(double a, double b, double c, bool absSort = true)
        {
            Sort(ref a, ref b, ref c, absSort);
            return a + b + c;
        }

        public static double Sum(double a, double b, double c, double d, bool absSort = true)
        {
            Sort(ref a, ref b, ref c, ref d, absSort);
            return a + b + c + d;
        }

        public static double Det(in (double x, double y) a, in (double x, double y) b) => (a.x * b.y) - (b.x * a.y);

        public static double Det(in (double x, double y, double z) a, in (double x, double y, double z) b, in (double x, double y, double z) c)
        {
            return Dot(a, Cross(b, c));
        }

        public static (double x, double y, double z) Cross(in (double x, double y, double z) a, in (double x, double y, double z) b)
        {
            return (Det(yz(a), yz(b)), Det(zx(a), zx(b)), Det(xy(a), xy(b)));
        }

        public static double Dot(in (double x, double y) a, in (double x, double y) b) => (a.x * b.x) + (a.y * b.y);

        public static double Dot(in (double x, double y) a) => Dot(a, a);

        public static double Dot(in (double x, double y, double z) a, in (double x, double y, double z) b) => Sum(a.x * b.x, a.y * b.y, a.z * b.z);

        public static double Dot(in (double x, double y, double z) a) => Dot(a, a);


        //public static (double x, double y, double z) Add(in (double x, double y, double z) a, in (double x, double y, double z) b) => (
        //    a.x + b.x,
        //    a.y + b.y,
        //    a.z + b.z);

        //public static (double x, double y, double z) Add(in (double x, double y, double z) a, in double b) => (
        //    a.x + b,
        //    a.y + b,
        //    a.z + b);

        //public static (double x, double y, double z) Sub(in (double x, double y, double z) a, in (double x, double y, double z) b) => (
        //    a.x - b.x,
        //    a.y - b.y,
        //    a.z - b.z);

        //public static (double x, double y, double z) Sub(in (double x, double y, double z) a, in double b) => (
        //    a.x - b,
        //    a.y - b,
        //    a.z - b);

        //public static (double x, double y, double z) Mul(in (double x, double y, double z) a, in double b) => (
        //    a.x * b,
        //    a.y * b,
        //    a.z * b);

        //public static (double x, double y, double z) Mul(in double a, in (double x, double y, double z) b) => (
        //    a * b.x,
        //    a * b.y,
        //    a * b.z);

    }
}
