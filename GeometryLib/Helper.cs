using System;
using static System.Math;

namespace GeometryLib.Double;

public static class Helper
{
    public static (double x, double y) xy(in (double x, double y, double z) value)
    {
        return (value.x, value.y);
    }

    public static (double y, double z) yz(in (double x, double y, double z) value)
    {
        return (value.y, value.z);
    }

    public static (double z, double x) zx(in (double x, double y, double z) value)
    {
        return (value.z, value.x);
    }

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
    ///     Sorts 2 doubles ascending
    /// </summary>
    public static void Sort(ref double a, ref double b)
    {
        (a, b) = a > b ? (b, a) : (a, b);
    }

    public static void SortByAbsoluteValue(ref double absa, ref double a, ref double absb, ref double b)
    {
        (absa, a, absb, b) = absa > absb ? (absb, b, absa, a) : (absa, a, absb, b);
    }

    /// <summary>
    ///     Sorts 3 doubles ascending
    /// </summary>
    public static void Sort(ref double a, ref double b, ref double c, bool byAbsoluteValue = false)
    {
        if (byAbsoluteValue)
        {
            var absa = double.Abs(a);
            var absb = double.Abs(b);
            var absc = double.Abs(c);
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
            double absa = Abs(a);
            double absb = Abs(b);
            double absc = Abs(c);
            double absd = Abs(d);
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


    public static double Hypot(in double a, in double b, in double c)
    {
        var aa = double.Abs(a);
        var ab = double.Abs(b);
        var ac = double.Abs(c);

        Sort(ref aa, ref ab, ref ac);

        return double.Hypot(aa, double.Hypot(ab, ac));
    }

    public static double Hypot(in double a, in double b, in double c, in double d)
    {
        var aa = double.Abs(a);
        var ab = double.Abs(b);
        var ac = double.Abs(c);
        var ad = double.Abs(d);

        Sort(ref aa, ref ab, ref ac, ref ad);

        return double.Hypot(aa, double.Hypot(ab, double.Hypot(ac, ad)));
    }

    public static double Hypot(in double a, double b, double c)
    {
        var aa = double.Abs(a);
        var ab = double.Abs(b);
        var ac = double.Abs(c);

        Sort(ref aa, ref ab, ref b);

        return double.Hypot(aa, double.Hypot(ab, ac));
    }

    public static double Normalize(ref double a, ref double b)
    {
        int[] iab = new[] { 0, 1 };
        double[] aab = new[] { Abs(a), Abs(b) };
        Sort(0, 1, iab, aab);
        double length = aab[1];

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
            double sq = Sqrt(aab[0] * aab[0] + 1.0);
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
        double s = a + b + c;
        if (s != 0
            && Abs(a * 3 / s - 1) < Constants.TRIGTOL
            && Abs(b * 3 / s - 1) < Constants.TRIGTOL
            && Abs(c * 3 / s - 1) < Constants.TRIGTOL)
        {
            double l = Hypot(a, b, c);
            a = Constants.RSQRT3 - Constants.TRIGTOL;
            b = Constants.RSQRT3;
            c = Constants.RSQRT3 + Constants.TRIGTOL;
            return l;
        }

        int[] iabc = new[] { 0, 1, 2 };
        double[] aabc = new[] { Abs(a), Abs(b), Abs(c) };
        Sort3(iabc, aabc);
        double length = aabc[2];

        if (aabc[1] == 0.0)
        {
            if (aabc[2] == 0.0 || double.IsNaN(aabc[2]))
                aabc[0] = aabc[1] = aabc[2] = double.NaN;
            else
                aabc[2] = 1.0;
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
            double sq = Sqrt(aabc[0] * aabc[0] + aabc[1] * aabc[1] + 1.0);
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
        int[] iabcd = new[] { 0, 1, 2, 3 };
        double[] aabcd = new[] { Abs(a), Abs(b), Abs(c), Abs(d) };
        Sort4(iabcd, aabcd);

        if (aabcd[2] == 0.0)
        {
            if (aabcd[3] == 0.0 || double.IsNaN(aabcd[3]))
                aabcd[0] = aabcd[1] = aabcd[2] = aabcd[3] = double.NaN;
            else
                aabcd[3] = 1.0;
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

    public static double Det(in (double x, double y) a, in (double x, double y) b)
    {
        return a.x * b.y - b.x * a.y;
    }

    public static double Det(in (double x, double y, double z) a, in (double x, double y, double z) b,
        in (double x, double y, double z) c)
    {
        return Dot(a, Cross(b, c));
    }

    public static (double x, double y, double z) Cross(in (double x, double y, double z) a,
        in (double x, double y, double z) b)
    {
        return (Det(yz(a), yz(b)), Det(zx(a), zx(b)), Det(xy(a), xy(b)));
    }

    public static double Dot(in (double x, double y) a, in (double x, double y) b)
    {
        return a.x * b.x + a.y * b.y;
    }

    public static double Dot(in (double x, double y) a)
    {
        return Dot(a, a);
    }

    public static double Dot(in (double x, double y, double z) a, in (double x, double y, double z) b)
    {
        return Sum(a.x * b.x, a.y * b.y, a.z * b.z);
    }

    public static double Dot(in (double x, double y, double z) a)
    {
        return Dot(a, a);
    }
}