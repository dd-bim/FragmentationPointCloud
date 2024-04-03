using System;
using System.Globalization;

namespace GeometryLib.Double.D3
{
    public readonly struct Quaternion
    {
        private static readonly Quaternion zero = new Quaternion(0.0, 0.0, 0.0, 1.0, true);
        private static readonly Quaternion unitX = new Quaternion(1.0, 0.0, 0.0, 0.0, true);
        private static readonly Quaternion unitY = new Quaternion(0.0, 1.0, 0.0, 0.0, true);
        private static readonly Quaternion unitZ = new Quaternion(0.0, 0.0, 1.0, 0.0, true);


        public static ref readonly Quaternion Zero => ref zero;

        public static ref readonly Quaternion UnitX => ref unitX;

        public static ref readonly Quaternion UnitY => ref unitY;

        public static ref readonly Quaternion UnitZ => ref unitZ;

        public double x { get; }

        public double y { get; }

        public double z { get; }

        public double w { get; }

        public double this[int index] => index switch { 0 => x, 1 => y, 2 => z, 3 => w, _ => throw new IndexOutOfRangeException() };


        public Quaternion(in double x, in double y, in double z, in double w, bool noNormalize)
        {
            if (noNormalize)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.w = w;
            }
            else
            {
                var rlen = 1.0 / Helper.Hypot(x, y, z, w);
                this.x = x * rlen;
                this.y = y * rlen;
                this.z = z * rlen;
                this.w = w * rlen;
            }
        }

        public Quaternion(in double x, in double y, in double z, in double w)
        {
            double tx = x, ty = y, tz = z, tw = w;
            Helper.Normalize(ref tx, ref ty, ref tz, ref tw);
            this.x = tx;
            this.y = ty;
            this.z = tz;
            this.w = tw;
        }

        public Quaternion(in RotMatrix m)
        {
            var x = Math.Sqrt(Math.Max(0.0, 1.0 + m.AxisX.x - m.AxisY.y - m.AxisZ.z)) / 2.0;
            var y = Math.Sqrt(Math.Max(0.0, 1.0 - m.AxisX.x + m.AxisY.y - m.AxisZ.z)) / 2.0;
            var z = Math.Sqrt(Math.Max(0.0, 1.0 - m.AxisX.x - m.AxisY.y + m.AxisZ.z)) / 2.0;
            this.w = Math.Sqrt(Math.Max(0.0, 1.0 + m.AxisX.x + m.AxisY.y + m.AxisZ.z)) / 2.0;
            this.x = m.AxisY.z < m.AxisZ.y ? -x : x;
            this.y = m.AxisZ.x < m.AxisX.z ? -y : y;
            this.z = m.AxisX.y < m.AxisY.x ? -z : z;
        }

        public static explicit operator Vector(in Quaternion q) => new Vector(q.x, q.y, q.z);

        public static explicit operator Quaternion(in Direction v) => new Quaternion(v.x, v.y, v.z, 0.0, true);

        public Quaternion(in Vector v, in double w, bool noNormalize) : this(v.x, v.y, v.z, w, noNormalize) { }

        public Quaternion(in (double x, double y, double z) v, in double w, bool noNormalize) : this(v.x, v.y, v.z, w, noNormalize) { }

        public Quaternion Conj() => new Quaternion(-x, -y, -z, w, true);

        public void Deconstruct(out double x, out double y, out double z, out double w)
        {
            x = this.x;
            y = this.y;
            z = this.z;
            w = this.w;
        }

        public (double x, double y, double z) XYZ => (x, y, z);

        public Quaternion Mul(in Quaternion other)
        {
            var qx = x * other.w + y * other.z - z * other.y + w * other.x;
            var qy = -x * other.z + y * other.w + z * other.x + w * other.y;
            var qz = x * other.y - y * other.x + z * other.w + w * other.z;
            var qw = -x * other.x - y * other.y - z * other.z + w * other.w;
            return new Quaternion(qx, qy, qz, qw);
        }

        /// <summary>Multiplication by unit x. Rotates the quaternion by pi around x axis</summary>
        public Quaternion MulUnitX() => new Quaternion(w, z, -y, -x, true);

        /// <summary>Multiplication by unit y. Rotates the quaternion by pi around y axis</summary>
        public Quaternion MulUnitY() => new Quaternion(-z, w, x, -y, true);

        /// <summary>Multiplication by unit z. Rotates the quaternion by pi around z axis</summary>
        public Quaternion MulUnitZ() => new Quaternion(y, -x, w, -z, true);

        public Direction AxisX() => new Direction(
                1.0 - 2.0 * ((y * y) + (z * z)),
                2.0 * ((w * z) + (x * y)),
                2.0 * ((x * z) - (w * y))
            );

        public Direction AxisY() => new Direction(
                2.0 * ((y * x) - (w * z)),
                1.0 - 2.0 * ((z * z) + (x * x)),
                2.0 * ((w * x) + (y * z))
            );

        public Direction AxisZ() => new Direction(
                2.0 * ((z * x) + (w * y)),
                2.0 * ((z * y) - (w * x)),
                1.0 - 2.0 * ((x * x) + (y * y))
            );

        public Vector Transform(in Vector v)
        {
            var abX = (y * v.z) - (z * v.y);
            var abY = (z * v.x) - (x * v.z);
            var abZ = (x * v.y) - (y * v.x);
            return new Vector(
                v.x + 2.0 * ((w * abX) + (y * abZ) - (z * abY)),
                v.y + 2.0 * ((w * abY) + (z * abX) - (x * abZ)),
                v.z + 2.0 * ((w * abZ) + (x * abY) - (y * abX))
            );
        }

        public Vector Transform(in D2.Vector v)
        {
            var abX = -z * v.y;
            var abY = (z * v.x);
            var abZ = (x * v.y) - (y * v.x);
            return new Vector(
                v.x + 2.0 * ((w * abX) + (y * abZ) - (z * abY)),
                v.y + 2.0 * ((w * abY) + (z * abX) - (x * abZ)),
                2.0 * ((w * abZ) + (x * abY) - (y * abX))
            );
        }

        public Direction Transform(in Direction v)
        {
            var abX = (y * v.z) - (z * v.y);
            var abY = (z * v.x) - (x * v.z);
            var abZ = (x * v.y) - (y * v.x);
            return new Direction(
                v.x + 2.0 * ((w * abX) + (y * abZ) - (z * abY)),
                v.y + 2.0 * ((w * abY) + (z * abX) - (x * abZ)),
                v.z + 2.0 * ((w * abZ) + (x * abY) - (y * abX))
            );
        }

        public double TransformZ(in Direction v)
        {
            return 2 * (z * (v.y * y + v.x * x) - y * (v.z * y + v.x * w) + x * (v.y * w - v.z * x)) + v.z;
        }

        public Direction Transform(in D2.Direction v)
        {
            var abX = -z * v.y;
            var abY = (z * v.x);
            var abZ = (x * v.y) - (y * v.x);
            return new Direction(
                v.x + 2.0 * ((w * abX) + (y * abZ) - (z * abY)),
                v.y + 2.0 * ((w * abY) + (z * abX) - (x * abZ)),
                2.0 * ((w * abZ) + (x * abY) - (y * abX))
            );
        }

        /// <summary>
        /// Multiplikation
        /// </summary>
        public static Quaternion operator *(in Quaternion left, in Quaternion right) => left.Mul(right);
        //{
        //    var qq = Cross(left.XYZ, right.XYZ);
        //    var v = Add(Add(Mul(left.w, right.XYZ), Mul(right.w, left.XYZ)), qq);
        //    var w = left.w * right.w - Dot(left.XYZ, right.XYZ);
        //    return new Quaternion(v, w);
        //}

        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "({0:G17} {1:G17} {2:G17}):{3:G17}", x, y, z, w);

    }
}
