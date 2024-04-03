using GeometryLib.Interfaces;
using GeometryLib.Interfaces.D2;

using System;
using System.Globalization;
using System.Numerics;

using Fra2 = GeometryLib.Int32.Fraction.D2;
using Numb = GeometryLib.Numbers;

namespace GeometryLib.Int32.D2
{
    public readonly struct Vector : IEquatable<Vector>, IComparable<Vector>, IIntegerPoint, IVector<int>//, IGeometryOgc2<int>
    {
        public int GeometricDimension => 0;

        private static readonly Vector zero = new Vector(0, 0);

        /// <summary>
        ///     Zero length Vector
        /// </summary>
        public static ref readonly Vector Zero => ref zero;

        /// <summary>X Axis Value</summary>
        public int x { get; }

        /// <summary>Y Axis Value</summary>
        public int y { get; }

        public (int x, int y) xy => (x, y);

        public double DoubleX => x;

        public double DoubleY => y;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Vector" /> struct.
        /// </summary>
        public Vector(in int x, in int y)
        {
            this.x = x;
            this.y = y;
        }

        public Double.D2.Vector ToDouble()
        {
            return new Double.D2.Vector(x, y);
        }

        public override string ToString()
        {
            return ToString(" ");
        }

        public string ToString(string separator)
        {
            return string.Format("{0}{2}{1}", x, y, separator);
        }

        public Vector((int x, int y) p) : this(p.x, p.y)
        {
        }

        public void Deconstruct(out int xOut, out int yOut)
        {
            (xOut, yOut) = (x, y);
        }

        public long Sum()
        {
            return (long)x + y;
        }

        public long AbsSum()
        {
            return (long)Math.Abs(x) + Math.Abs(y);
        }

        public Vector Neg()
        {
            return new Vector(-x, -y);
        }

        public Vector Square()
        {
            return new Vector(x * x, y * y);
        }

        public Vector Abs()
        {
            return new Vector(Math.Abs(x), Math.Abs(y));
        }

        public long SumSq()
        {
            return Dot(x, y);
        }

        public Vector Add(in Vector other)
        {
            return new Vector(x + other.x, y + other.y);
        }

        public Vector Add(in int other)
        {
            return new Vector(x + other, y + other);
        }

        public Vector Sub(in Vector other)
        {
            return new Vector(x - other.x, y - other.y);
        }

        public Vector Sub(in int other)
        {
            return new Vector(x - other, y - other);
        }

        public Vector Mul(in int other)
        {
            return new Vector(other * x, other * y);
        }

        public static long Det(in Vector a, in Vector b, in Vector c)
        {
            var ax = (long)b.x - a.x;
            var ay = (long)b.y - a.y;
            var bx = (long)c.x - a.x;
            var by = (long)c.y - a.y;
            return (ax * by) - (ay * bx);
        }

        public long Det(in Vector a, in Vector b)
        {
            return Det(a, b, this);
        }

        public long Det(in (Vector a, Vector b) segment)
        {
            return Det(segment.a, segment.b, this);
        }

        public long Det(in Vector other)
        {
            return Det(x, y, other.x, other.y);
        }

        private static long Det(int ax, int ay, int bx, int by)
        {
            return Math.BigMul(ax, by) - Math.BigMul(ay, bx);
        }

        public long Dot(Vector other)
        {
            return Dot(x, y, other.x, other.y);
        }

        internal static long Dot(int ax, int ay, int bx, int by)
        {
            return Math.BigMul(ax, bx) + Math.BigMul(ay, by);
        }

        private static long Dot(int x, int y)
        {
            return Math.BigMul(x, x) + Math.BigMul(y, y);
        }

        public bool IsBetween(in Vector a, in Vector b)
        {
            var ab = b.Sub(a);
            var ap = Sub(a);
            if (ab.Det(ap) != 0) return false;

            var lnum = ab.Dot(ap);
            var lden = ab.SumSq();

            return lnum > 0 && lnum < lden;
        }


        public bool IsOnRay(in Vector orig, in Vector dest, out long det)
        {
            var ab = dest.Sub(orig);
            var ap = Sub(orig);
            det = ab.Det(ap);
            return det == 0 && ab.Dot(ap) > 0;
        }


        public bool IsInside(Vector a, Vector b, Vector c)
        {
            return Det(a, b, this) > 0 && Det(b, c, this) > 0 && Det(c, a, this) > 0;
        }

        public bool IsDisjoint(Vector a, Vector b, Vector c)
        {
            return Det(a, b, this) < 0 || Det(b, c, this) < 0 || Det(c, a, this) < 0;
        }


        internal static bool Intersect(in Vector a, in Vector b, in Vector c, in Vector d,
            out ulong num1, out ulong num2, out ulong den)
        {
            var ac = c - a;
            var ab = b - a;
            var cd = d - c;

            var lnum1 = cd.Det(ac);
            var lnum2 = ab.Det(ac);
            var lden = cd.Det(ab);
            // force positive denominator
            if (lden < 0)
            {
                lnum1 = -lnum1;
                lnum2 = -lnum2;
                lden = -lden;
            }

            if (lnum1 < 0 || lnum2 < 0 || lden == 0 || lnum1 > lden || lnum2 > lden)
            {
                num1 = default;
                num2 = default;
                den = default;
                return false;
            }

            num1 = (ulong)lnum1;
            num2 = (ulong)lnum2;
            den = (ulong)lden;
            return true;
        }

        public static bool Collinear(in Vector a, in Vector b, in Vector c)
        {
            return Det(a, b, c) == 0;
        }

        public bool Collinear(in Vector a, in Vector b)
        {
            return Det(a, b) == 0;
        }

        internal static bool Intersect(in Vector a1, in Vector b1, in Vector a2, in Vector b2, out ulong num1,
            out ulong den)
        {
            var aa = a2 - a1;
            var ab1 = b1 - a1;
            var ab2 = b2 - a2;

            var lnum1 = ab2.Det(aa);
            var lden = ab2.Det(ab1);
            // force positive denominator
            if (lden < 0)
            {
                lnum1 = -lnum1;
                lden = -lden;
            }

            if (lnum1 < 0 || lden == 0 || lnum1 > lden)
            {
                num1 = default;
                den = default;
                return false;
            }

            num1 = (ulong)lnum1;
            den = (ulong)lden;
            return true;
        }

        /// <summary>
        ///     Relative position of intersection point of two segments on first segment
        /// </summary>
        /// <param name="a1"> Start of first segment </param>
        /// <param name="b1"> Dest of first segment </param>
        /// <param name="a2"> Start of second segment </param>
        /// <param name="b2"> Dest of second segment </param>
        /// <param name="pos1"> </param>
        /// <param name="pos2"> </param>
        /// <returns> </returns>
        public static bool Intersect(in Vector a1, in Vector b1, in Vector a2, in Vector b2,
            out Numb.Fraction pos1, out Numb.Fraction pos2)
        {
            if (Intersect(a1, b1, a2, b2, out var num1, out var num2, out var den))
            {
                pos1 = new Numb.Fraction(num1, den);
                pos2 = new Numb.Fraction(num2, den);
                return true;
            }

            pos1 = default;
            pos2 = default;
            return false;
        }

        /// <summary>
        ///     Relative position of intersection point of two segments on first segment
        /// </summary>
        /// <param name="a1"> Start of first segment </param>
        /// <param name="b1"> Dest of first segment </param>
        /// <param name="a2"> Start of second segment </param>
        /// <param name="b2"> Dest of second segment </param>
        /// <param name="pos1"> </param>
        /// <returns> </returns>
        public static bool Intersect(in Vector a1, in Vector b1, in Vector a2, in Vector b2, out Numb.Fraction pos1)
        {
            if (Intersect(a1, b1, a2, b2, out ulong num1, out var den))
            {
                pos1 = new Numb.Fraction(num1, den);
                return true;
            }

            pos1 = default;
            return false;
        }

        /// <summary>
        ///     Position on line a b (if possible)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="point"></param>
        /// <param name="num"></param>
        /// <param name="den"></param>
        public static bool PositionOf(in Vector a, in Vector b, in Vector point, out ulong num, out ulong den)
        {
            var ap = point - a;
            var ab = b - a;

            var lnum = ab.Dot(ap);
            var lden = ab.SumSq();

            if (lnum < 0 || lden == 0 || lnum > lden)
            {
                num = default;
                den = default;
                return false;
            }

            num = (ulong)lnum;
            den = (ulong)lden;
            return true;
        }

        /// <summary> Calculates point position on segment</summary>
        public static bool PositionOf(in Vector a, in Vector b, in Vector point, out Numb.Fraction position)
        {
            if (PositionOf(a, b, point, out var num, out var den))
            {
                position = new Numb.Fraction(num, den);
                return true;
            }

            position = default;
            return false;
        }

        public bool Equals(Vector point)
        {
            return x == point.x && y == point.y;
        }

        public bool Equals(Fra2.Vector point)
        {
            return point.Equals(this);
        }

        public override bool Equals(object? obj)
        {
            return obj is Vector point && Equals(point);
        }

        ///// <inheritdoc />
        //public bool Equals(IPoint? other)
        //{
        //    return other switch
        //    {
        //        PointF pointF => Equals(pointF),
        //        Point point => Equals(point),
        //        _ => false
        //    };
        //}

        public override int GetHashCode()
        {
#if NETSTANDARD2_0 || NET472 || NET48 || NET462
            return x ^ y;
#else
            return HashCode.Combine(x, y);
#endif
        }

        public int CompareTo(Vector other)
        {
            var comp = x.CompareTo(other.x);
            return comp != 0 ? comp : y.CompareTo(other.y);
        }

        public static bool operator ==(in Vector left, in Vector right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(in Vector left, in Vector right)
        {
            return !left.Equals(right);
        }

        public static bool operator <(in Vector left, in Vector right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(in Vector left, in Vector right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(in Vector left, in Vector right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(in Vector left, in Vector right)
        {
            return left.CompareTo(right) >= 0;
        }


        //public static bool operator ==(in Point left, in IPoint right) => left.Equals(right);

        //public static bool operator !=(in Point left, in IPoint right) => !left.Equals(right);

        //public static bool operator ==(in IPoint left, in Point right) => (left != null) && left.Equals(right);

        //public static bool operator !=(in IPoint left, in Point right) => (left != null) && !left.Equals(right);

        public static (Vector, Vector) Sort(in Vector a, in Vector b)
        {
            return a.CompareTo(b) > 0 ? (b, a) : (a, b);
        }

        public Vector Min(in Vector other)
        {
            return new Vector(
                Math.Min(x, other.x),
                Math.Min(y, other.y));
        }

        public Vector Max(in Vector other)
        {
            return new Vector(
                Math.Max(x, other.x),
                Math.Max(y, other.y));
        }

        public static Vector operator +(in Vector left, in Vector right)
        {
            return left.Add(right);
        }

        public static Vector operator +(in Vector left, in int right)
        {
            return left.Add(right);
        }

        public static Vector operator -(in Vector left, in Vector right)
        {
            return left.Sub(right);
        }

        public static Vector operator -(in Vector left, in int right)
        {
            return left.Sub(right);
        }

        public static Vector operator -(in Vector value)
        {
            return value.Neg();
        }

        public static long operator *(in Vector left, in Vector right)
        {
            return left.Dot(right);
        }

        public static Vector operator *(in int left, in Vector right)
        {
            return right.Mul(left);
        }

        public static Vector operator *(in Vector left, in int right)
        {
            return left.Mul(right);
        }

        /// <summary>
        /// Returns only true if this is in Direction of edge
        /// </summary>
        /// <param name="edgeA"></param>
        /// <param name="edgeB"></param>
        /// <returns></returns>
        public bool InDirection(in Vector edgeA, in Vector edgeB)
        {
            var bx = (long)edgeB.x - edgeA.x;
            var tx = (long)x - edgeA.x;
            var by = (long)edgeB.y - edgeA.y;
            var ty = (long)y - edgeA.y;
            var det = (bx * ty) - (by * tx);
            var dotx = bx * tx;
            var doty = by * ty;
            var sx = Math.Sign(dotx);
            var sy = Math.Sign(doty);
            // && (dotx + doty) > 0;
            return det == 0 && (((sx + sy) > 0) 
                || (sx < 0 && sy > 0 && doty > dotx) 
                || (sy < 0 && sx > 0 && dotx > doty));
        }

        //public long DirectedEdgeSign(in Vector edgeA, in Vector edgeB)
        //{
        //    int bx = edgeB.x - edgeA.x;
        //    int tx = x - edgeA.x;
        //    int by = edgeB.y - edgeA.y;
        //    int ty = y - edgeA.y;
        //    long det = Math.BigMul(bx, ty) - Math.BigMul(by, tx);
        //    return det == 0 ? (Math.BigMul(bx, tx) + Math.BigMul(by, ty)) > 0 ? 0 : -1 : det;

        //    //return Math.Sign(Math.BigMul(lineA.x - x, lineB.y - y) - Math.BigMul(lineA.y - y, lineB.x - x));
        //}

        //private static int inCircle(in Vector a, in Vector b, in Vector c)
        //{
        //    //Det(in Vector a, in Vector b) => a.Sub(this).Det(b.Sub(this));
        //    // Nicht under/over-flow sicher!
        //    var ab = a.Det(b);
        //    var bc = b.Det(c);
        //    var ca = c.Det(a);

        //    int s1 = Math.Sign(ab);
        //    int s2 = Math.Sign(bc);
        //    int s3 = Math.Sign(ca);
        //    GeometryLib.Int32.Helper.Sort(ref s1, ref s2, ref s3);

        //    /*
        //     * Dreieck muss ccw sein!
        //     * s1  s2  s3
        //     * <0  <0  >0 = Außen (hinter Vertex)           = s2 
        //     * <0  =0  >0 = Außen (hinter Vertex auf Kante) = <0
        //     * <0  >0  >0 = Außen (neben Kante)             =  ?
        //     * =0  =0  >0 = auf Vertex                      = s2
        //     * =0  >0  >0 = auf Kante                       = s2
        //     * >0  >0  >0 = innen                           = s2
        //     */

        //    int sig = s2;
        //    if (s1 < 0)
        //    {
        //        if (s2 > 0)
        //        {
        //            var ad = a.SumSq();
        //            var bd = b.SumSq();
        //            var cd = c.SumSq();
        //            sig = (BigInteger.Multiply(ad, bc) 
        //                 + BigInteger.Multiply(bd, ca) 
        //                 + BigInteger.Multiply(cd, ab)).Sign;
        //        }
        //        else
        //        {
        //            sig = -1;
        //        }
        //    }

        //    return sig;
        //}

//        REAL incirclefast(pa, pb, pc, pd)
//{
//  adx = pa[0] - pd[0];
//  ady = pa[1] - pd[1];
//  bdx = pb[0] - pd[0];
//  bdy = pb[1] - pd[1];
//  cdx = pc[0] - pd[0];
//  cdy = pc[1] - pd[1];

//  abdet = adx* bdy - bdx* ady;
//  bcdet = bdx* cdy - cdx* bdy;
//  cadet = cdx* ady - adx* cdy;
//  alift = adx* adx + ady* ady;
//  blift = bdx* bdx + bdy* bdy;
//  clift = cdx* cdx + cdy* cdy;

//  return alift* bcdet + blift* cadet + clift* abdet;
//    }

    public bool InCircle(in Vector a, in Vector b, in Vector c) // => inCircle(a - this, b - this, c - this);
        {
            // Quelle:https://www.cs.cmu.edu/afs/cs/project/quake/public/code/predicates.c : incirclefast

            long adx = a.x - x;
            long ady = a.y - y;
            long bdx = b.x - x;
            long bdy = b.y - y;
            long cdx = c.x - x;
            long cdy = c.y - y;

            var abdet = adx * bdy - bdx * ady;
            var bcdet = bdx * cdy - cdx * bdy;
            var cadet = cdx * ady - adx * cdy;
            var alift = adx * adx + ady * ady;
            var blift = bdx * bdx + bdy * bdy;
            var clift = cdx * cdx + cdy * cdy;

            return (BigInteger.Multiply(alift, bcdet) 
                + BigInteger.Multiply(blift, cadet)
                + BigInteger.Multiply(clift, abdet)).Sign > 0;



            //long cax = (long)a.x - c.x;
            //long pax = (long)a.x - x;
            //long cbx = (long)b.x - c.x;
            //long pbx = (long)b.x - x;
            //long cay = (long)a.y - c.y;
            //long pay = (long)a.y - y;
            //long cby = (long)b.y - c.y;
            //long pby = (long)b.y - y;

            //long e = (cax * cbx) + (cay * cby);
            //long f = (pbx * pay) - (pax * pby);
            //long g = (cbx * cay) - (cax * cby);
            //long h = (pbx * pax) + (pay * pby);

            ////var comp = N.Misc.ProductCompare(e, f, g, h);

            //// return BigInteger.Multiply(e, f).CompareTo(BigInteger.Multiply(g, h)) < 0;
            //return (BigInteger.Multiply(e, f) + BigInteger.Multiply(g, h)).Sign < 0;
        }

        public Vector OtherY(in Vector other)
        {
            return new Vector(x, other.y);
        }


        public Vector AddOne()
        {
            return new Vector(x + 1, y + 1);
        }

        public Vector SubOne()
        {
            return new Vector(x - 1, y - 1);
        }

        //public bool IsBetween(in IIntegerPoint a, in IIntegerPoint b)
        //{
        //    switch ((a, b))
        //    {
        //        case (Vector av, Vector bv):
        //            return IsBetween(av, bv);
        //        case (F.Vector av, Vector bv):
        //            return new F.Vector(this).IsBetween(av, new F.Vector(bv));
        //        case (Vector av, F.Vector bv):
        //            return new F.Vector(this).IsBetween(new F.Vector(av), bv);
        //        case (F.Vector av, F.Vector bv):
        //            return new F.Vector(this).IsBetween(av, bv);
        //        default:
        //            throw new NotImplementedException();
        //    }
        //}

        //public int EdgeSign(in IIntegerPoint a, in IIntegerPoint b)
        //{
        //    if (a is Vector av && b is Vector bv) return EdgeSign(av, bv);
        //    else return new F.Vector(this).EdgeSign(a, b);
        //}

        //public bool InCircle(in IIntegerPoint a, in IIntegerPoint b, in IIntegerPoint c)
        //{
        //    if (a is Vector av && b is Vector bv && c is Vector cv) return InCircle(av, bv, cv);

        //    throw new NotImplementedException();
        //}

        public bool Equals(IIntegerPoint? other)
        {
            return other is Vector v ? Equals(v) : other is Fra2.Vector fv && fv.Equals(this);
        }

        public string ToWktString() => $"{WKTNames.Point}({ToString()})";

        public static bool TryParse(in string input, out Vector vector, char separator = ' ')
        {
            var str = input.Trim();
            if (!string.IsNullOrEmpty(str))
            {
                var split = str.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length >= 2
                             && int.TryParse(split[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x)
                             && int.TryParse(split[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y))
                {
                    vector = new Vector(x, y);
                    return true;
                }
            }
            vector = default;
            return false;
        }

        public static bool TryParseWkt(in string input, out Vector vector)
        {
            var si = input.IndexOf(WKTNames.Point, StringComparison.InvariantCultureIgnoreCase) + WKTNames.Point.Length + 1;
            var ei = input.LastIndexOf(')');
            if ((ei - si) > 2)
                return TryParse(input[si..ei], out vector);
            vector = default;
            return false;
        }

        IVector<int> IVector<int>.Min(IVector<int> other)
        {
            return Min(other is Vector v ? v : new Vector(other.x, other.y));
        }

        IVector<int> IVector<int>.Max(IVector<int> other)
        {
            return Max(other is Vector v ? v : new Vector(other.x, other.y));
        }


        int IVector<int>.SideSign(IVector<int> v2, IVector<int> v3)
        {
            return Math.Sign(Det(
                v2 is Vector v ? v : new Vector(v2.x, v2.y),
                v3 is Vector vv ? vv : new Vector(v3.x, v3.y)
                ));
        }
    }
}