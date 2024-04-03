using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;


using static System.Math;
using static GeometryLib.Double.Constants;
using static GeometryLib.Double.Helper;

namespace GeometryLib.Double.D3
{
    public readonly struct Vector
    {
        private static readonly Vector zero = new Vector(0.0, 0.0, 0.0);
        private static readonly Vector nan = new Vector(double.NaN, double.NaN, double.NaN);
        private static readonly Vector negativeInfinity = new Vector(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);
        private static readonly Vector positiveInfinity = new Vector(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);

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

        /// <summary>Z Axis Value</summary>
        public double z { get; }

        public bool IsNaN => double.IsNaN(x) || double.IsNaN(y) || double.IsNaN(z);

        public bool IsInfinity => double.IsInfinity(x) || double.IsInfinity(y) || double.IsInfinity(z);

        /// <summary>
        /// Initializes a new instance of the <see cref="Vector"/> struct.
        /// </summary>
        public Vector(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector((double x, double y, double z) tuple) : this(tuple.x, tuple.y, tuple.z) { }

        public void Deconstruct(out double outX, out double outY, out double outZ)
        {
            outX = x;
            outY = y;
            outZ = z;
        }

        public (double x, double y, double z) xyz => (x, y, z);

        public double Length() => Hypot(x, y, z);

        public Vector Neg() => new Vector(-x, -y, -z);

        public Vector Square() => new Vector(x * x, y * y, z * z);

        public Vector Abs() => new Vector(Math.Abs(x), Math.Abs(y), Math.Abs(z));

        public double SumSq() => Helper.Dot(xyz);

        public double Sum() => Helper.Sum(x, y, z);

        public double AbsSum() => Helper.Sum(Math.Abs(x), Math.Abs(y), Math.Abs(z), false);

        public double Distance(in Vector other) => Hypot(x - other.x, y - other.y, z - other.z);

        public Vector Add(in Vector other) => new Vector(
            x + other.x,
            y + other.y,
            z + other.z);

        public Vector Add(in double other) => new Vector(
            x + other,
            y + other,
            z + other);

        public Vector Sub(in Vector other) => new Vector(
            x - other.x,
            y - other.y,
            z - other.z);

        public Vector Sub(in double other) => new Vector(
            x - other,
            y - other,
            z - other);

        public Vector Mul(in double other) => new Vector(
            other * x,
            other * y,
            other * z);

        public Vector Mid(in Vector other) => new Vector(
            0.5 * (x + other.x),
            0.5 * (y + other.y),
            0.5 * (z + other.z));

        public Vector Min(in Vector other) => new Vector(
            Math.Min(x, other.x),
            Math.Min(y, other.y),
            Math.Min(z, other.z));

        public Vector Max(in Vector other) => new Vector(
            Math.Max(x, other.x),
            Math.Max(y, other.y),
            Math.Max(z, other.z));

        public double Dot(in Vector other) => Helper.Dot(xyz, other.xyz);

        public Vector Cross(in Vector other) => new Vector(Helper.Cross(xyz, other.xyz));

        public static double Det(in Vector a, in Vector b, in Vector c) => Helper.Det(a.xyz, b.xyz, c.xyz);

        public SpdMatrix Outer() => new SpdMatrix(x * x, x * y, x * z, y * y, y * z, z * z);

        public bool ApproxEquals(in Vector other, double tol = EPS)
        {
            var diff = Sub(other).Abs();
            return diff.x <= tol && diff.y <= tol && diff.z <= tol;
        }

        public Vector Transform(Quaternion rotation, Vector translation) =>
            translation + rotation.Transform(this);

        public Vector BackTransform(Quaternion rotation, Vector translation) =>
            rotation.Conj().Transform(Sub(translation));

        /// <summary>
        /// Punkt als Schnittpunkt, wenn möglich
        /// </summary>
        /// <param name="a">Ebene 1</param>
        /// <param name="b">Ebene 2</param>
        /// <param name="c">Ebene 3</param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static bool Create(IPlane a, IPlane b, IPlane c, out Vector point)
        {
            var ab = a.Normal.Cross(b.Normal);
            if (ab.SumSq() > TRIGTOL_SQUARED)
            {
                var bc = b.Normal.Cross(c.Normal);
                if (bc.SumSq() > TRIGTOL_SQUARED)
                {
                    var ca = c.Normal.Cross(a.Normal);
                    if (ca.SumSq() > TRIGTOL_SQUARED)
                    {
                        var v = (a.D * bc) + (b.D * ca) + (c.D * ab);
                        var t = -3.0 / (ab.Dot(c.Normal) + bc.Dot(a.Normal) + ca.Dot(b.Normal));
                        point = new Vector(t * v.x, t * v.y, t * v.z);
                        return true;
                    }
                }
            }
            point = default;
            return false;
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
            Helper.Sum(a.y, b.y, c.y) * THIRD,
            Helper.Sum(a.z, b.z, c.z) * THIRD);

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
            var zs = new double[len];
            var azs = new double[len];
            for (var i = 0; i < len; i++)
            {
                (var x, var y, var z) = it.Current;
                xs[i] = x;
                ys[i] = y;
                zs[i] = z;
                axs[i] = Math.Abs(x);
                ays[i] = Math.Abs(y);
                azs[i] = Math.Abs(z);
                it.MoveNext();
            }
            Array.Sort(axs, xs);
            Array.Sort(ays, ys);
            Array.Sort(azs, zs);
            var sx = xs[0];
            var sy = ys[0];
            var sz = zs[0];
            for (var i = 1; i < len; i++)
            {
                sx += xs[i];
                sy += ys[i];
                sz += zs[i];
            }
            return new Vector(sx, sy, sz);
        }

        public override string ToString() => ToString(" ");

        public string ToString(string separator) => string.Format(CultureInfo.InvariantCulture, "{0:G17}{3}{1:G17}{3}{2:G17}", x, y, z, separator);

        public string ToWktString() => $"POINT Z({this})";

        public static bool TryParse(in string input, out Vector vector, char separator = ' ')
        {
            var str = input.Trim();
            if (!string.IsNullOrEmpty(str))
            {
                var split = str.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length >= 3
                    && double.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
                    && double.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y)
                    && double.TryParse(split[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
                {
                    vector = new Vector(x, y, z);
                    return true;
                }
            }
            vector = default;
            return false;
        }

        public static bool TryParseWkt(in string input, out Vector vector)
        {
            if (!string.IsNullOrEmpty(input) && input.Length > 7)
            {
                var start = input.IndexOf("POINT", StringComparison.InvariantCultureIgnoreCase);
                var end = start + 5;
                if (start >= 0 && end < input.Length)
                {
                    start = input.IndexOf("Z", end, StringComparison.InvariantCultureIgnoreCase);
                    end = start + 1;
                    if (start > 4 && end < input.Length)
                    {
                        start = input.IndexOf('(', end) + 1;
                        if (start > 6 && start < input.Length)
                        {
                            end = input.IndexOf(')', start);
                            if (end > (start + 4))
                            {
                                return TryParse(input[start..end], out vector);
                            }
                        }
                    }
                }
            }
            vector = default;
            return false;
        }

        public static void WriteObj(string path, IReadOnlyList<IReadOnlyList<Vector>> rings)
        {
            using var file = File.CreateText(path + ".obj");
            var faces = new List<string>();
            var vertexCnt = 1;
            foreach (var ring in rings)
            {
                var first = vertexCnt;
                var face = new StringBuilder("f");
                for (var i = 0; i < ring.Count; i++)
                {
                    file.WriteLine($"v {ring[i]}");
                    face.AppendFormat(" {0}", first + i);
                }
                faces.Add(face.ToString());
                vertexCnt += ring.Count;
            }
            foreach (var face in faces)
            {
                file.WriteLine(face);
            }
        }

    }
}
