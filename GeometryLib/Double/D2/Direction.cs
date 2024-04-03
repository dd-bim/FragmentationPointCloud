using System;
using System.Globalization;

using static GeometryLib.Double.Constants;
using static GeometryLib.Double.Helper;
using static System.Math;

namespace GeometryLib.Double.D2
{
    public readonly struct Direction
    {
        private static readonly Direction unitX = new Direction(1.0, 0.0, true);
        private static readonly Direction unitY = new Direction(0.0, 1.0, true);
        private static readonly Direction negUnitX = new Direction(-1.0, 0.0, true);
        private static readonly Direction negUnitY = new Direction(0.0, -1.0, true);

        /// <summary>
        /// Unit vector of x-axis
        /// </summary>
        public static ref readonly Direction UnitX => ref unitX;

        /// <summary>
        /// Unit vector of y-axis
        /// </summary>
        public static ref readonly Direction UnitY => ref unitY;

        /// <summary>
        /// Negative unit vector of x-axis
        /// </summary>
        public static ref readonly Direction NegUnitX => ref negUnitX;

        /// <summary>
        /// Negative unit vector of y-axis
        /// </summary>
        public static ref readonly Direction NegUnitY => ref negUnitY;

        /// <summary>X Axis Value</summary>
        public double x { get; }

        /// <summary>Y Axis Value</summary>
        public double y { get; }

        /// <summary>Sine of direction angle</summary>
        public double sin => y;

        /// <summary>Cosine of direction angle</summary>
        public double cos => x;

        internal Direction(in double x, in double y, bool intern)
        {
            this.x = x;
            this.y = y;
        }
        public Direction(in double x, in double y)
        {
            double tx = x, ty = y;
            Helper.Normalize(ref tx, ref ty);
            this.x = tx;
            this.y = ty;
        }

        public Direction(in Vector v) : this(v.x, v.y) { }

        public Direction(in double angle):this(Cos(angle), Sin(angle))
        { }

        public void Deconstruct(out double outX, out double outY) => (outX, outY) = (x, y);

        public static Direction Create(in double x, in double y, out double length)
        {
            double tx = x, ty = y;
            length = Helper.Normalize(ref tx, ref ty);
            return new Direction(x, y, true);
        }

        public static Direction Create(in Vector vector, out double length) => Create(vector.x, vector.y, out length);

        public static implicit operator Vector(in Direction d) => new Vector(d.x, d.y);

        public (double x, double y) Tuple => (x, y);

        public double Angle() => Math.Atan2(y, x);

        /// <summary>2 * PI - x</summary>
        public Direction Neg() => new Direction(x, -y, true);

        public Direction TwoPiSubThis() => Neg();

        /// <summary>PI/2 - x</summary>
        public Direction Complementary() => new Direction(y, x, true);

        public Direction HalfPiSubThis() => Complementary();

        /// <summary>PI - x</summary>
        public Direction Supplementary() => new Direction(-x, y, true);

        public Direction PiSubThis() => Supplementary();

        public Direction SubHalfPi() => new Direction(y, -x, true);

        public Direction AddHalfPi() => new Direction(-y, x, true);

        public Direction AddPi() => new Direction(-x, -y, true);

        public Direction SubPi() => AddPi();

        public Direction ReNormalize()
        {
            var rlen = 1.0 / Hypot(x,y);
            return new Direction(x * rlen, y * rlen);
        }

        internal static Direction Diff(in D3.Direction left, in D3.Direction right) => new Direction(left.DiffCos(right), left.DiffSin(right));

        public Direction Diff(in Direction other) => new Direction(Dot(other), Abs(other.Det(this)));

        public double DiffCos(in Direction other) => Dot(other);

        public double DiffSin(in Direction other) => Abs(other.Det(this));

        public Direction Sub(in Direction other) => new Direction(Dot(other), other.Det(this));

        public Direction Add(in Direction other) => new Direction((x * other.x) - (y * other.y), (y * other.x) + (x * other.y));

        public Vector Mul(in double other) => new Vector(other * x, other * y);

        public double Dot(in Direction other) => Helper.Dot(Tuple, other.Tuple);

        public double Det(in Direction other) => Helper.Det(Tuple, other.Tuple);

        public double Dot(in Vector other) => Helper.Dot(Tuple, other.xy);

        public double Det(in Vector other) => Helper.Det(Tuple, other.xy);

        public Direction BiSector(in Direction other)
        {
            var xx = this.x + other.x;
            var yy = this.y + other.y;
            var len = Hypot(xx, yy);
            return len > TRIGTOL
                ? new Direction(xx / len, yy / len, true)
                : AddHalfPi(); // Richtungen entgegengesetzt kollinear
        }

        public Vector RotateSub(in Vector other) => new Vector(Dot(other), Det(other));

        public Vector RotateAdd(in Vector other) => new Vector((x * other.x) - (y * other.y), (y * other.x) + (x * other.y));

        public override string ToString() => ToString(" ");

        public string ToString(string separator) => string.Format(CultureInfo.InvariantCulture, "{0:G17}{2}{1:G17}", x, y, separator);

        public static Direction operator +(in Direction left, in Direction right) => left.Add(right);

        public static Direction operator -(in Direction left, in Direction right) => left.Sub(right);

        public static double operator *(in Direction left, in Direction right) => left.Dot(right);

        public static Vector operator *(in double left, in Direction right) => right.Mul(left);

        public static Vector operator *(in Direction left, in double right) => left.Mul(right);

        public static Vector operator /(in Direction left, in double right) => left.Mul(1.0 / right);

    }
}
