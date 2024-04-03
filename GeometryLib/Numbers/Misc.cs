using System;

namespace GeometryLib.Numbers
{
    public static class Misc
    {
        // TODO: Hat irgendeinen Bug
        //public static int ProductCompare(in long a1, in long a2, in long b1, in long b2)
        //{
        //    int siga = Math.Sign(a1) * Math.Sign(a2);
        //    int sigb = Math.Sign(b1) * Math.Sign(b2);
        //    int comp = siga.CompareTo(sigb);
        //    if (comp != 0 || siga == 0)
        //    {
        //        return comp;
        //    }

        //    ulong ua1 = (ulong)Math.Abs(a1); // 63Bit
        //    ulong ua2 = (ulong)Math.Abs(a2);
        //    ulong ub1 = (ulong)Math.Abs(b1);
        //    ulong ub2 = (ulong)Math.Abs(b2);

        //    ulong a1Hi = ua1 >> 32; // 31 Bit Shift reicht, da long nur 63 Bits + 1 Sign Bit hat
        //    ulong a2Hi = ua2 >> 32; // somit hat xHi noch maximal 32 Bits gesetzt, das erhöht
        //    ulong b1Hi = ub1 >> 32; // die Chance hier schon einen Treffer zu erreichen
        //    ulong b2Hi = ub2 >> 32;
        //    ulong aa = a1Hi * a2Hi;
        //    ulong bb = b1Hi * b2Hi;
        //    comp = siga * (aa.CompareTo(bb));
        //    return comp != 0 ? comp : siga * (ua1 * ua2).CompareTo(ub1 * ub2);
        //}


        //public static int ProductCompare(in ulong a1, in ulong a2, in ulong b1, in ulong b2)
        //{
        //    ulong a1Hi = a1 >> 32;
        //    ulong a2Hi = a2 >> 32;
        //    ulong b1Hi = b1 >> 32;
        //    ulong b2Hi = b2 >> 32;
        //    ulong aHi = a1Hi * a2Hi;
        //    ulong bHi = b1Hi * b2Hi;
        //    int comp = aHi.CompareTo(bHi);
        //    return comp != 0 ? comp : (a1 * a2).CompareTo(b1 * b2);
        //}
    }
}
