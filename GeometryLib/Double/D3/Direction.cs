using System;
using System.Globalization;

using static GeometryLib.Double.Constants;
using static GeometryLib.Double.Helper;
using static System.Math;

namespace GeometryLib.Double.D3
{
    public readonly struct Direction
    {
        private static readonly Direction unitX = new Direction(1.0, 0.0, 0.0, true);
        private static readonly Direction unitY = new Direction(0.0, 1.0, 0.0, true);
        private static readonly Direction unitZ = new Direction(0.0, 0.0, 1.0, true);
        private static readonly Direction negUnitX = new Direction(-1.0, 0.0, 0.0, true);
        private static readonly Direction negUnitY = new Direction(0.0, -1.0, 0.0, true);
        private static readonly Direction negUnitZ = new Direction(0.0, 0.0, -1.0, true);

        /// <summary>
        /// Unit vector of x-axis
        /// </summary>
        public static ref readonly Direction UnitX => ref unitX;

        /// <summary>
        /// Unit vector of y-axis
        /// </summary>
        public static ref readonly Direction UnitY => ref unitY;

        /// <summary>
        /// Unit vector of z-axis
        /// </summary>
        public static ref readonly Direction UnitZ => ref unitZ;

        /// <summary>
        /// Negative unit vector of x-axis
        /// </summary>
        public static ref readonly Direction NegUnitX => ref negUnitX;

        /// <summary>
        /// Negative unit vector of y-axis
        /// </summary>
        public static ref readonly Direction NegUnitY => ref negUnitY;

        /// <summary>
        /// Negative unit vector of z-axis
        /// </summary>
        public static ref readonly Direction NegUnitZ => ref negUnitZ;

        /// <summary>X Axis Value</summary>
        public double x { get; }

        /// <summary>Y Axis Value</summary>
        public double y { get; }

        /// <summary>Z Axis Value</summary>
        public double z { get; }

        public Direction(in double x, in double y, in double z, bool intern)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Direction(in double x, in double y, in double z)
        {
            double tx = x, ty = y, tz = z;
            Helper.Normalize(ref tx,ref ty,ref tz);
            this.x = tx;
            this.y = ty;
            this.z = tz;
        }

        public static explicit operator Direction(in Vector v) => new Direction(v.x,v.y,v.z);

        public Direction(in (double x, double y, double z) tuple) : this(tuple.x, tuple.y, tuple.z) { }

        public Direction(in double azimuth, in double inclination)
        {
            var sin = Sin(inclination);
            var x = sin * Cos(azimuth);
            var y = sin * Sin(azimuth);
            var z = Cos(inclination);
            Helper.Normalize(ref x, ref y, ref z);
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Direction(in D2.Direction azimuth, in D2.Direction inclination)
        {
            var x = inclination.sin * azimuth.cos;
            var y = inclination.sin * azimuth.sin;
            var z = inclination.cos;
            Helper.Normalize(ref x, ref y, ref z);
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Direction(in (D2.Direction azimuth, D2.Direction inclination) angles) : this(angles.azimuth, angles.inclination) { }

        public void Deconstruct(out double outX, out double outY, out double outZ)
        {
            outX = x;
            outY = y;
            outZ = z;
        }

        public (double x, double y, double z) xyz => (x, y, z);

        public static Direction Create(in double x, in double y, in double z, out double length)
        {
            double tx = x, ty = y, tz = z;
            length = Helper.Normalize(ref tx, ref ty, ref tz);
            return new Direction(tx,ty,tz,true);
        }

        public static Direction Create(in Vector vector, out double length) => Create(vector.x, vector.y, vector.z, out length);

        public static implicit operator Vector(in Direction d) => new Vector(d.x, d.y, d.z);

        public double Azimuth() => Atan2(y, x);

        public double Inclination() => Acos(z);

        public D2.Direction InclinationDirection() => new D2.Direction(z, Sqrt(1.0 - z * z), true);

        public Direction Turn() => new Direction(-x, -y, -z, true);

        public Vector Mul(in double other) => new Vector(
            other * x,
            other * y,
            other * z);

        public double Dot(in Direction other) => Helper.Dot(xyz, other.xyz);

        public double Dot(in Vector other) => Helper.Dot(xyz, other.xyz);

        public Vector Cross(in Direction other) => new Vector(Helper.Cross(xyz, other.xyz));

        public Vector Cross(in Vector other) => new Vector(Helper.Cross(xyz, other.xyz));

        public D2.Direction Diff(in Direction other) => D2.Direction.Diff(this, other);

        public double DiffCos(in Direction other) => Dot(other);

        public double DiffSin(in Direction other) => Hypot(Helper.Cross(other.xyz, xyz));

        private Vector perp()
        {
            return new Vector(
                y * y - z * x,
                z * z - x * y,
                x * x - y * z);
        }

        public Direction Perp()
        {
            var vx = perp();
            var vy = Cross(vx);
            return (Direction)vy.Cross(this); 
            // nochmaliges Cross um nmerische Grenzfälle auszugleichen
            // im Normalfall ist das Ergebniss gleich zu vx
        }

        public Direction Perp(out Direction third)
        {
            var second = Perp();
            third = (Direction)Cross(second);
            return second;
        }

        public Direction MakePerp(in Vector second, out Direction third)
        {
            third = new Direction(Helper.Cross(xyz, second.xyz));
            return new Direction(Helper.Cross(third.xyz, xyz));
        }

        public Direction BiSector(in Direction other)
        {
            var xx = this.x + other.x;
            var yy = this.y + other.y;
            var zz = this.z + other.z;
            var len = Hypot(xx, yy, zz);
            if (len > TRIGTOL)
            {
                len = 1.0 / len;
                return new Direction(xx * len, yy * len, zz * len, true);
            }
            return Perp(); // Richtungen entgegengesetzt kollinear
        }

        public static Vector operator *(in double left, in Direction right) => right.Mul(left);

        public static Vector operator *(in Direction left, in double right) => left.Mul(right);

        public static Vector operator /(in Direction left, in double right) => left.Mul(1.0 / right);

        public static Direction operator -(in Direction value) => value.Turn();


        public override string ToString() => ToString(" ");

        public string ToString(string separator) => string.Format(CultureInfo.InvariantCulture, "{0:G17}{3}{1:G17}{3}{2:G17}", x, y, z, separator);

        public string ToWktString() => $"POINT Z({ToString()})";


        public override int GetHashCode()
        {
#if NETSTANDARD2_0 || NET472 || NET48 || NET462
            return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();
#else
            return HashCode.Combine(x, y, z);
#endif
        }

    }
}
