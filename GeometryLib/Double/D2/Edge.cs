using System;

using static GeometryLib.Double.Constants;

namespace GeometryLib.Double.D2
{
    public readonly struct Edge
    {
        public Vector Orig { get; }

        public Vector Dest { get; }

        public Edge(Vector orig, Vector dest)
        {
            Orig = orig;
            Dest = dest;
        }

        public Vector Vector => Dest - Orig;

        public bool IsPoint => Vector.SumSq() < TRIGTOL_SQUARED;

        public int SideSign(in Vector v) => Math.Sign(v.Sub(Orig).Det(this.Vector));

        //public bool Intersection(in Edge other, out Vector p)
        //{
        //    var aa = other.Orig - Orig;
        //    var ab1 = this.Vector;
        //    var ab2 = other.Vector;

        //    double num1 = ab2.Det(aa);
        //    double num2 = ab1.Det(aa);
        //    double den = ab2.Det(ab1);
        //    // force positive denominator
        //    if (den < 0)
        //    {
        //        num1 = -num1;
        //        num2 = -num2;
        //        den = -den;
        //    }

        //    if (num1 < 0 || num2 < 0 || den < TRIGTOL || num1 > den || num2 > den)
        //    {
        //        p = default;
        //        return false;
        //    }

        //    if (num1 > num2)
        //    {
        //        num1 /= den;
        //        p = new Vector(Orig.x + ab1.x * num1, Orig.y + ab1.y * num1);
        //    }
        //    else
        //    {
        //        num2 /= den;
        //        p = new Vector(other.Orig.x + ab2.x * num2, other.Orig.y + ab2.y * num2);
        //    }
        //    return true;
        //}

        //public static bool Intersection(in Edge edge, in Line line, out Vector p)
        //{
        //    Vector u = edge.Dest - edge.Orig;
        //    Vector v = line.Direction;
        //    Vector w = edge.Orig - line.Position;
        //    double D = u.Det(v);

        //    // test if  the(y are parallel (includes either being a point)
        //    if (Math.Abs(D) < Constants.TRIGTOL)
        //    {           // S1 and S2 are parallel
        //        p = default;
        //        return false;
        //    }

        //    // the segments are skew and may intersect in a point
        //    // get the intersect parameter for S1
        //    double sI = v.Det(w) / D;

        //    p = edge.Orig + sI * u;                // compute S1 intersect point
        //    return true;
        //}

        //public static bool Intersection(in Edge edge, in Edge S2, out Vector p)
        //{
        //    Vector u = edge.Dest - edge.Orig;
        //    Vector v = S2.Dest - S2.Orig;
        //    Vector w = edge.Orig - S2.Orig;
        //    double D = u.Det(v);

        //    // test if  the(y are parallel (includes either being a point)
        //    if (Math.Abs(D) < Constants.TRIGTOL)
        //    {           // S1 and S2 are parallel
        //        p = default;
        //        return false;
        //    }

        //    // the segments are skew and may intersect in a point
        //    // get the intersect parameter for S1
        //    double sI = v.Det(w) / D;
        //    if (sI < 0 || sI > 1)                // no intersect with S1
        //    {
        //        p = default;
        //        return false;
        //    }

        //    // get the intersect parameter for S2
        //    double tI = u.Det(w) / D;
        //    if (tI < 0 || tI > 1)                // no intersect with S2
        //    {
        //        p = default;
        //        return false;
        //    }

        //    p = edge.Orig + sI * u;                // compute S1 intersect point
        //    return true;
        //}
    }
}