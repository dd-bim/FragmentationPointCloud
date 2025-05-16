using System;
using static System.Math;

namespace GeometryLib;

public static class Helper
{
    private static void Sort(int ia, int ib, int[] idxs, double[] vals)
    {
        (idxs[ia], idxs[ib], vals[ia], vals[ib]) =
            vals[ia] > vals[ib]
                ? (idxs[ib], idxs[ia], vals[ib], vals[ia])
                : (idxs[ia], idxs[ib], vals[ia], vals[ib]);
    }


    /// <summary>
    ///     Sorts 2 doubles ascending
    /// </summary>
    private static void Sort(ref double a, ref double b)
    {
        (a, b) = a > b ? (b, a) : (a, b);
    }

    private static void SortByAbsoluteValue(ref double absa, ref double a, ref double absb, ref double b)
    {
        (absa, a, absb, b) = absa > absb ? (absb, b, absa, a) : (absa, a, absb, b);
    }

    /// <summary>
    ///     Sorts 3 doubles ascending
    /// </summary>
    private static void Sort(ref double a, ref double b, ref double c, bool byAbsoluteValue = false)
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


    public static double Normalize(ref double a, ref double b)
    {
        int[] iab = [0, 1];
        double[] aab = [Abs(a), Abs(b)];
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

        Array.Sort(iab, aab);
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
            double b1 = b;
            var aa = double.Abs(a);
            var ab = double.Abs(b1);
            var ac = double.Abs(c);

            Sort(ref aa, ref ab, ref b1);
            double l = double.Hypot(aa, double.Hypot(ab, ac));
            a = Constants.RSQRT3 - Constants.TRIGTOL;
            b = Constants.RSQRT3;
            c = Constants.RSQRT3 + Constants.TRIGTOL;
            return l;
        }

        int[] iabc = [0, 1, 2];
        double[] aabc = [Abs(a), Abs(b), Abs(c)];
        Sort(0, 1, iabc, aabc);
        Sort(1, 2, iabc, aabc);
        Sort(0, 1, iabc, aabc);
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

        Array.Sort(iabc, aabc);
        a = a < 0 ? -aabc[0] : aabc[0];
        b = b < 0 ? -aabc[1] : aabc[1];
        c = c < 0 ? -aabc[2] : aabc[2];

        return length;
    }

    public static double Sum(double a, double b, double c, bool absSort = true)
    {
        Sort(ref a, ref b, ref c, absSort);
        return a + b + c;
    }

}