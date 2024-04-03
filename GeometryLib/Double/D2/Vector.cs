using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

using static System.Math;
using static GeometryLib.Double.Constants;
using static GeometryLib.Double.Helper;

using I = GeometryLib.Int32.D2;

namespace GeometryLib.Double.D2
{
    public readonly struct Vector : IEquatable<Vector>, IComparable<Vector>
    {
        private static readonly Vector zero = new Vector(0.0, 0.0);
        private static readonly Vector nan = new Vector(double.NaN, double.NaN);
        private static readonly Vector negativeInfinity = new Vector(double.NegativeInfinity, double.NegativeInfinity);
        private static readonly Vector positiveInfinity = new Vector(double.PositiveInfinity, double.PositiveInfinity);

        /// <summary>
        /// Zero length Vector
        /// </summary>
        public static ref readonly Vector Zero => ref zero;

        /// <summary>
        /// NaN vector
        /// </summary>
        public static ref readonly Vector NaN => ref nan;

        /// <summary>Negative infinity vector</summary>
        public static ref readonly Vector NegativeInfinity => ref negativeInfinity;

        /// <summary>Positive infinity vector</summary>
        public static ref readonly Vector PositiveInfinity => ref positiveInfinity;

        /// <summary>X Axis Value</summary>
        public double x { get; }

        /// <summary>Y Axis Value</summary>
        public double y { get; }

        public bool IsNaN => double.IsNaN(x) || double.IsNaN(y);

        public bool IsInfinity => double.IsInfinity(x) || double.IsInfinity(y);

        /// <summary>
        /// Initializes a new instance of the <see cref="Vector"/> struct.
        /// </summary>
        public Vector(double x, double y)
        {
            this.x = x;
            this.y = y;
        }
        public Vector(D3.Vector vector)
        {
            this.x = vector.x;
            this.y = vector.y;
        }

        public Vector((double x, double y) tuple) : this(tuple.x, tuple.y) { }

        /// <summary>Deconstructs the vector.</summary>
        public void Deconstruct(out double outX, out double outY) => (outX, outY) = (x, y);

        /// <summary>Gets the tuple.</summary>
        public (double x, double y) xy => (x, y);

        //public static implicit operator Vector(IVector2<double> v) => v is Vector vec ? vec : new Vector(v.x,v.y);

        /// <summary>The length (magnitude) of the vector.</summary>
        public double Length() => Hypot(x, y);

        /// <summary>The angle of the vector direction.</summary>
        public double Angle() => Math.Atan2(y, x);

        public double Sum() => x + y;

        public double AbsSum() => Math.Abs(x) + Math.Abs(y);

        public Vector Neg() => new Vector(-x, -y);

        public Vector Square() => new Vector(x * x, y * y);

        public Vector Abs() => new Vector(Math.Abs(x), Math.Abs(y));

        public double SumSq() => Helper.Dot(xy, xy);

        public bool ApproxEquals(in Vector other, double tol = EPS)
        {
            var diff = Sub(other).Abs();
            return diff.x <= tol && diff.y <= tol;
        }

        public Vector Add(in Vector other) => new Vector(x + other.x, y + other.y);

        public Vector Add(in double other) => new Vector(x + other, y + other);

        public Vector Sub(in Vector other) => new Vector(x - other.x, y - other.y);

        public Vector Sub(in double other) => new Vector(x - other, y - other);

        public Vector Mul(in double other) => new Vector(other * x, other * y);

        public Vector Mid(in Vector other) => new Vector(
            0.5 * (x + other.x),
            0.5 * (y + other.y));

        public Vector Min(in Vector other) => new Vector(
            Math.Min(x, other.x),
            Math.Min(y, other.y));

        public Vector Max(in Vector other) => new Vector(
            Math.Max(x, other.x),
            Math.Max(y, other.y));

        /// <summary>
        /// Scalar product (dot)
        /// </summary>
        public double Dot(in Vector other) => Helper.Dot(xy, other.xy);

        public double Det(in Vector a, in Vector b) => a.Sub(this).Det(b.Sub(this));

        /// <summary>
        ///  Determinant of a 2x2 matrix of the vectors
        /// </summary>
        public double Det(in Vector other) => Helper.Det(xy, other.xy);

        public SpdMatrix Outer() => new SpdMatrix(x * x, x * y, y * y);

        public double SignedDistanceToLine(Vector lineA, Vector lineB)
        {
            var l = lineB - lineA;
            return l.Det(Sub(lineA)) / l.Length();
        }


        public static Vector operator +(in Vector left, in Vector right) => left.Add(right);

        public static Vector operator +(in Vector left, in double right) => left.Add(right);

        public static Vector operator -(in Vector left, in Vector right) => left.Sub(right);

        public static Vector operator -(in Vector left, in double right) => left.Sub(right);

        public static Vector operator -(in Vector value) => value.Neg();

        public static double operator *(in Vector left, in Vector right) => left.Dot(right);

        public static Vector operator *(in double left, in Vector right) => right.Mul(left);

        public static Vector operator *(in Vector left, in double right) => left.Mul(right);

        public static Vector operator /(in Vector left, in double right) => left.Mul(1.0 / right);

        public static Vector Mean(in Vector a, in Vector b) => a.Mid(b);

        public static Vector Mean(in Vector a, in Vector b, in Vector c) => new Vector(
            Helper.Sum(a.x, b.x, c.x) * THIRD,
            Helper.Sum(a.y, b.y, c.y) * THIRD);

        public static Vector Mean(in IReadOnlyCollection<Vector> vectors, bool isRing = false)
        {
            using var it = vectors.GetEnumerator();
            if (!it.MoveNext())
            {
                return Zero;
            }
            var a = it.Current;
            if (!it.MoveNext())
            {
                return a;
            }
            var b = it.Current;
            if (!it.MoveNext())
            {
                return isRing ? a : Mean(a, b);
            }
            var c = it.Current;
            if (!it.MoveNext())
            {
                return isRing ? Mean(a, b) : Mean(a, b, c);
            }
            var sum = Sum(vectors, isRing);
            return sum / (isRing ? vectors.Count - 1 : vectors.Count);
        }

        public static Vector Sum(in IReadOnlyCollection<Vector> vectors, bool isRing = false)
        {
            var len = isRing ? vectors.Count - 1 : vectors.Count;
            if (len < 1)
            {
                return Zero;
            }
            using var it = vectors.GetEnumerator();
            _ = it.MoveNext();
            if (len == 1)
            {
                return it.Current;
            }
            var xs = new double[len];
            var axs = new double[len];
            var ys = new double[len];
            var ays = new double[len];
            for (var i = 0; i < len; i++)
            {
                (var x, var y) = it.Current;
                xs[i] = x;
                ys[i] = y;
                axs[i] = Math.Abs(x);
                ays[i] = Math.Abs(y);
                it.MoveNext();
            }
            Array.Sort(axs, xs);
            Array.Sort(ays, ys);
            var sx = xs[0];
            var sy = ys[0];
            for (var i = 1; i < len; i++)
            {
                sx += xs[i];
                sy += ys[i];
            }
            return new Vector(sx, sy);
        }

        public D3.Vector Transform(D3.Quaternion rotation, D3.Vector translation) =>
            translation + rotation.Transform(this);

        //public Vector BackTransform(Quaternion rotation, Vector translation) =>
        //    rotation.Conj() * Sub(translation);

        public override string ToString() => ToString(" ");

        public string ToString(string separator) => string.Format(CultureInfo.InvariantCulture, "{0:G17}{2}{1:G17}", x, y, separator);

        private static int circleSign(in Vector a, in Vector b, in Vector c)
        {
            //Det(in Vector a, in Vector b) => a.Sub(this).Det(b.Sub(this));
            // Nicht under/over-flow sicher!
            var ab = a.Det(b);
            var bc = b.Det(c);
            var ca = c.Det(a);

            var s1 = Math.Sign(ab);
            var s2 = Math.Sign(bc);
            var s3 = Math.Sign(ca);
            Int32.Helper.Sort(ref s1, ref s2, ref s3);

            /*
             * Dreieck muss ccw sein!
             * s1  s2  s3
             * <0  <0  >0 = Außen (hinter Vertex)           = s2 
             * <0  =0  >0 = Außen (hinter Vertex auf Kante) = <0
             * <0  >0  >0 = Außen (neben Kante)             =  ?
             * =0  =0  >0 = auf Vertex                      = s2
             * =0  >0  >0 = auf Kante                       = s2
             * >0  >0  >0 = innen                           = s2
             */

            var sig = s2;
            if (s1 < 0)
            {
                if (s2 > 0)
                {
                    var ad = a.SumSq();
                    var bd = b.SumSq();
                    var cd = c.SumSq();
                    //return false;
                    sig = Math.Sign(Helper.Sum(ad * bc, bd * ca, cd * ab));
                }
                else
                {
                    sig = -1;
                }
            }

            return sig;
        }

        public int CircleSign(in Vector a, in Vector b, in Vector c) => circleSign(a - this, b - this, c - this);

        public bool Equals(Vector point) => (x == point.x) && (y == point.y);

        public override bool Equals(object? obj) => obj is Vector point && Equals(point);

        public override int GetHashCode()
        {
#if NETSTANDARD2_0 || NET472 || NET48 || NET462
            return x.GetHashCode() ^ y.GetHashCode();
#else
            return HashCode.Combine(x, y);
#endif
        }

        public int CompareTo(Vector other)
        {
            var comp = x.CompareTo(other.x);
            return comp != 0 ? comp : y.CompareTo(other.y);
        }

        public Vector OtherY(in Vector other) => new Vector(x, other.y);

        public Vector AddOne() => new Vector(x + 1, y + 1);

        public Vector SubOne() => new Vector(x - 1, y - 1);

        public Vector ToDouble() => this;


        public string ToWktString() => $"{WKTNames.Point}({ToString()})";

        public static bool TryParse(in string input, out Vector vector, char separator = ' ')
        {
            var str = input.Trim();
            if (!string.IsNullOrEmpty(str))
            {
                var split = str.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length >= 2
                    && double.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
                    && double.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
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

        public static bool operator ==(Vector left, Vector right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector left, Vector right)
        {
            return !(left == right);
        }

        public static bool operator <(Vector left, Vector right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(Vector left, Vector right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(Vector left, Vector right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(Vector left, Vector right)
        {
            return left.CompareTo(right) >= 0;
        }

        /// <summary>
        /// Cleans point list from duplicates
        /// </summary>
        /// <param name="minimalDistance">Minimal point distance</param>
        /// <param name="points">Point list</param>
        /// <param name="old2newIndex">Mapping of indices in old point list to indices in new list </param>
        /// <param name="bbox">Bounding box</param>
        /// <returns>Cleaned list of points, rounded to the mean of all points in the minimal distance</returns>
        public static ImmutableArray<Vector> MakeUnique(in double minimalDistance, in IReadOnlyList<Vector> points, out ImmutableDictionary<int, int> old2newIndex, in BBox? bbox = null)
        {
            // Punkte sortieren nach Koordinatensumme (absolute Werte)
            var xys = points.Select(p => p.AbsSum()).ToArray();
            var indices = Enumerable.Range(0, points.Count).ToArray();
            Array.Sort(xys, indices);

            // Vorwärtssuche nach Punkten in Mindestabstand
            var maxSumDiff = SQRT2 * minimalDistance; // Maximal mögliche Differenz der XY Summe
            var minimalDistanceSquared = minimalDistance * minimalDistance;
            var sindices = ImmutableArray.CreateBuilder<ImmutableHashSet<int>.Builder>(indices.Length);
            for (var i = 0; i < indices.Length; i++)
            {
                var ix = xys[i];
                var ii = indices[i];
                var iv = points[ii];
                if (ii < 0)
                    continue;

                var equalIndizes = ImmutableHashSet.CreateBuilder<int>();
                equalIndizes.Add(ii);

                for (var j = i + 1; j < indices.Length; j++)
                {
                    if (Math.Abs(ix - xys[j]) > maxSumDiff)
                        break;
                    var ji = indices[j];
                    if ((iv - points[ji]).SumSq() > minimalDistanceSquared)
                        continue;
                    equalIndizes.Add(ji);
                }
                sindices.Add(equalIndizes);
            }

            // Indizes vereinigen (Rückwärtssuche)
            for (var i = sindices.Count - 1; i >= 0; i--)
            {
                var ii = sindices[i];
                for (var j = i - 1; j >= 0; j--)
                {
                    if (sindices[j].Overlaps(ii))
                    {
                        sindices[j].UnionWith(ii);
                        sindices.RemoveAt(i);
                        break;
                    }
                }
            }

            // Mittel bilden und Indizes zuweisen
            var map = ImmutableDictionary.CreateBuilder<int, int>();
            var spoints = ImmutableArray.CreateBuilder<Vector>();
            for (var i = 0; i < sindices.Count; i++)
            {
                var sum = zero;
                foreach (var pi in sindices[i])
                {
                    sum += points[pi];
                    map[pi] = i;
                }
                spoints.Add(sum / sindices[i].Count);
            }

            old2newIndex = map.ToImmutable();

            return spoints.ToImmutable();
        }

        public class XComparer : IComparer<Vector>
        {
            public int Compare(Vector x, Vector y) => x.x.CompareTo(y.x);
        }

        public class YComparer : IComparer<Vector>
        {
            public int Compare(Vector x, Vector y) => x.y.CompareTo(y.y);
        }

    }
}
