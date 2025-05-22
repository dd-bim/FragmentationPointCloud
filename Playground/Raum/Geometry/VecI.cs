using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Playground.Raum.Geometry;

public readonly record struct VecI(long X, long Y)
{
    public static VecI operator -(in VecI left, in VecI right) => new(left.X - right.X, left.Y - right.Y);
    public static VecI operator +(in VecI left, in VecI right) => new(left.X + right.X, left.Y + right.Y);

    /// <summary>
    /// Determinant
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static Int128 operator ^(in VecI left, in VecI right) => ((Int128)left.X * right.Y) - ((Int128)left.Y * right.X);

    /// <summary>
    /// Returns a new <see cref="VecI"/> instance with the minimum X and Y values between this instance and the
    /// specified <paramref name="other"/> instance.
    /// </summary>
    /// <param name="other">The <see cref="VecI"/> instance to compare with this instance.</param>
    /// <returns>A new <see cref="VecI"/> instance where each component is the smaller of the corresponding components in this
    /// instance and <paramref name="other"/>.</returns>
    public VecI Min(in VecI other) => new(long.Min(X, other.X), long.Min(Y, other.Y));

    /// <summary>
    /// Returns a new vector containing the maximum values of the corresponding components from this vector and the
    /// specified vector.
    /// </summary>
    /// <param name="other">The vector to compare with this vector.</param>
    /// <returns>A new <see cref="VecI"/> where each component is the greater of the corresponding components of this vector and
    /// <paramref name="other"/>.</returns>
    public VecI Max(in VecI other) => new(long.Max(X, other.X), long.Max(Y, other.Y));

    /// <summary>
    /// Determines the relative position of this point with respect to the directed edge defined by the specified source
    /// and target points.
    /// </summary>
    /// <param name="edgeSource">The starting point of the directed edge.</param>
    /// <param name="edgeTarget">The ending point of the directed edge.</param>
    /// <returns>An integer indicating the relative position of this point: <list type="bullet"> <item><description><c>1</c> if
    /// this point is to the left of the directed edge.</description></item> <item><description><c>-1</c> if this point
    /// is to the right of the directed edge.</description></item> <item><description><c>0</c> if this point lies
    /// exactly on the directed edge.</description></item> </list></returns>
    public int SideSign(in VecI edgeSource, in VecI edgeTarget) => Int128.Sign((edgeSource - this) ^ (edgeTarget - this));

    /// <summary>
    /// Determines whether a flip operation is required based on the positions of three vertices and the current point.
    /// </summary>
    /// <remarks>This method evaluates the relative positions of the vertices and the current point to
    /// determine  whether the point lies within the circumcircle of the triangle formed by the vertices. If the point 
    /// is inside the circumcircle, a flip operation is required to maintain the Delaunay condition.</remarks>
    /// <param name="a">The first vertex of the triangle.</param>
    /// <param name="b">The second vertex of the triangle.</param>
    /// <param name="c">The third vertex of the triangle.</param>
    /// <returns><see langword="true"/> if a flip operation is required; otherwise, <see langword="false"/>.</returns>
    public bool DoFlip(in VecI a, in VecI b, in VecI c) // => inCircle(sa - this, ta - this, c - this);
    {
        // Cline_Renka_test
        //            c
        //           /1\
        //          /---\
        //         /     \
        //        sa-------ta
        //         \     /
        //          \___/
        //           \2/
        //            P
        // P muss rechts von sa-ta liegen!

        Int128 xca = a.X - c.X;
        Int128 xcb = b.X - c.X;
        Int128 xpa = a.X - X;
        Int128 xpb = b.X - X;
        Int128 yca = a.Y - c.Y;
        Int128 ycb = b.Y - c.Y;
        Int128 ypa = a.Y - Y;
        Int128 ypb = b.Y - Y;

        var cos1 = (xca * xcb) + (yca * ycb);
        var cos2 = (xpb * xpa) + (ypb * ypa);

        switch ((Int128.IsNegative(cos1), Int128.IsNegative(cos2)))
        {
            case (true, true):
                // 1 > 90° und 2 > 90° == P im Umkreis = Flip sa-ta
                return true;
            case (false, false):
                // 1 <= 90° und 2 <= 90° == P sicher außerhalb bzw. auf Kreis (beide 90°) = kein Flip nötig
                return false;
            default:
                var msin1 = (xcb * yca) - (xca * ycb); // -sin1
                var sin2 = (xpb * ypa) - (xpa * ypb);
                return BigInteger.Multiply(cos1, sin2) < BigInteger.Multiply(msin1, cos2);
        }

    }

    /// <summary>
    /// Determines whether the current segment intersects or touches the specified segment from the right.
    /// </summary>
    /// <remarks>This method checks for an intersection or touch point between the current segment and the
    /// specified segment within the given fractional range of the other segment. The intersection or touch is
    /// considered valid only if it occurs from the right side of the current segment.</remarks>
    /// <param name="target">The endpoint of the current segment.</param>
    /// <param name="otherSource">The starting point of the other segment.</param>
    /// <param name="otherTarget">The endpoint of the other segment.</param>
    /// <param name="otherStart">The fractional position representing the start of the other segment.</param>
    /// <param name="otherEnd">The fractional position representing the end of the other segment.</param>
    /// <param name="otherPos">When this method returns <see langword="true"/>, contains the fractional position of the intersection or touch
    /// point on the other segment. If no intersection or touch occurs, this is set to the default value.</param>
    /// <returns><see langword="true"/> if the current segment intersects or touches the other segment from the right within the
    /// specified fractional range; otherwise, <see langword="false"/>.</returns>
    public bool IntersectsOrTouchesFromRight(in VecI target, in VecI otherSource, in VecI otherTarget,
     in Fraction otherStart, in Fraction otherEnd, out Fraction otherPos)
    {
        var sata = target - this;
        var sbtb = otherTarget - otherSource;
        var tbsa = this - otherTarget;
        Int128 den = sata ^ sbtb, numb, revnumb, numa;
        if (den > 0L
            && Int128.Sign(revnumb = tbsa ^ sata) >= 0
            && (numa = sbtb ^ tbsa) <= den
            && Int128.Sign(numa) >= 0
            && Int128.Sign(numb = den - revnumb) >= 0
            && Fraction.Create(numb, den, out var iposB)
            && iposB is Fraction posB
            && otherStart < posB
            && posB <= otherEnd)
        {
            otherPos = posB;
            return true;
        }
        otherPos = default;
        return false;
    }

    /// <summary>
    /// Determines the orientation of the edge defined by the current point and the specified <paramref name="target"/> 
    /// relative to the edge defined by <paramref name="otherSource"/> and <paramref name="otherTarget"/>.
    /// </summary>
    /// <param name="target">The endpoint of the edge originating from the current point.</param>
    /// <param name="otherSource">The starting point of the other edge.</param>
    /// <param name="otherTarget">The endpoint of the other edge.</param>
    /// <param name="sameDirection">When this method returns, contains <see langword="true"/> if the two edges point in the same general direction; 
    /// otherwise, <see langword="false"/>.</param>
    /// <returns>An integer indicating the orientation of the current edge relative to the other edge: <list type="bullet">
    /// <item><description><c>-1</c> if the current edge is counterclockwise relative to the other
    /// edge.</description></item> <item><description><c>0</c> if the two edges are collinear.</description></item>
    /// <item><description><c>1</c> if the current edge is clockwise relative to the other edge.</description></item>
    /// </list></returns>
    public int GetEdgeOrientation(in VecI target, in VecI otherSource, in VecI otherTarget, out bool sameDirection)
    {
        (long xt1, long yt1) = target;
        (long xs2, long ys2) = otherSource;
        (long xt2, long yt2) = otherTarget;

        long dx1 = xt1 - X;
        long dy1 = yt1 - Y;
        long dx2 = xt2 - xs2;
        long dy2 = yt2 - ys2;

        sameDirection = ((Int128)dx1 * dx2) + ((Int128)dy1 * dy2) > 0;
        return Int128.Sign(((Int128)dx1 * dy2) - ((Int128)dy1 * dx2));
    }

    /// <summary>
    /// Calculates the intersection position of this edge with another edge in terms of a fractional parameter.
    /// </summary>
    /// <param name="target">The endpoint of this edge, represented as a <see cref="VecI"/>.</param>
    /// <param name="otherSource">The starting point of the other edge, represented as a <see cref="VecI"/>.</param>
    /// <param name="otherTarget">The endpoint of the other edge, represented as a <see cref="VecI"/>.</param>
    /// <returns>A <see cref="Fraction"/> representing the relative position of the intersection along this edge. The value is
    /// normalized such that 0 represents the starting point of this edge, and 1 represents the endpoint.</returns>
    /// <exception cref="ArithmeticException">Thrown if the intersection calculation fails, such as when the edges are parallel or overlapping.</exception>
    public Fraction IntersectEdge(in VecI target, in VecI otherSource, in VecI otherTarget)
    {
        var sasb = otherSource - this;
        var sata = target - this;
        var sbtb = otherTarget - otherSource;

        var numa = sbtb ^ sasb;
        var den = sbtb ^ sata;

        // Denominator muss positiv sein
        if (den < 0)
        {
            numa = -numa;
            den = -den;
        }

        return !Fraction.Create(numa, den, out var position)
            ? throw new ArithmeticException($"Intersection failed.") : position;
    }

    private static bool TryMultiply(Int128 a, long b, out Int128 result)
    {
        try
        {
            checked
            {
                result = a * b;
            }
            return true;

        }
        catch (OverflowException)
        {
            result = default;
            return false;
        }
    }

    public static bool PointOnEdge(VecI source, VecI target, Fraction position, out VecI point)
    {
        if (position.IsZero)
        {
            point = source;
            return true;
        }
        if (position.IsOne)
        {
            point = target; 
            return true;
        }
        var (sx, sy) = source;
        var (tx, ty) = target;
        long dx, dy;
        Int128 remx, remy;
        if (TryMultiply(position.Num, tx - sx, out var prodX) &&
                TryMultiply(position.Num, ty - sy, out var prodY))
        {
            (var idx, remx) = Int128.DivRem(prodX, position.Den);
            (var idy, remy) = Int128.DivRem(prodY, position.Den);
            dx = (long)idx;
            dy = (long)idy;
        }
        else
        {
            var (bdx, bremx) = BigInteger.DivRem(BigInteger.Multiply(position.Num, tx - sx), position.Den);
            var (bdy, bremy) = BigInteger.DivRem(BigInteger.Multiply(position.Num, ty - sy), position.Den);
            dx = (long)bdx;
            dy = (long)bdy;
            remx = (Int128)bremx;
            remy = (Int128)bremy;
        }
        long x = sx + dx;
        long y = sy + dy;
        var denh = position.Den >> 1;
        if (remx > denh)
        {
            x++;
        }
        if (remy > denh)
        {
            y++;
        }
        point = new VecI(x, y);
        return remx == 0 && remy == 0;

    }
}

