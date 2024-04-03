using System;
using System.Globalization;
using System.Text;

using static GeometryLib.Double.Helper;


namespace GeometryLib.Double.D3
{
    public enum Axes { X, Y, Z }


    public readonly struct RotMatrix
    {
        private static readonly RotMatrix unit = new RotMatrix(
            1.0, 0.0, 0.0, 
            0.0, 1.0, 0.0, 
            0.0, 0.0, 1.0);
 
        public static ref readonly RotMatrix Unit => ref unit;

        public Direction AxisX { get; }

        public Direction AxisY { get; }

        public Direction AxisZ { get; }

        public double xx => AxisX.x;
        public double yx => AxisX.y;
        public double zx => AxisX.z;
        public double xy => AxisY.x;
        public double yy => AxisY.y;
        public double zy => AxisY.z;
        public double xz => AxisZ.x;
        public double yz => AxisZ.y;
        public double zz => AxisZ.z;

        public (Direction x, Direction y, Direction z) Cols => (
                AxisX,
                AxisY,
                AxisZ);

        public Direction ColX => AxisX;

        public Direction ColY => AxisY;

        public Direction ColZ => AxisZ;

        private RotMatrix(
            in double xx, in double xy, in double xz,
            in double yx, in double yy, in double yz,
            in double zx, in double zy, in double zz)
        {
            this.AxisX = new Direction(xx, yx, zx, true);
            this.AxisY = new Direction(xy, yy, zy, true);
            this.AxisZ = new Direction(xz, yz, zz, true);
        }

        public RotMatrix(Quaternion q)
        {
            (var x, var y, var z, var w) = q;
            (var wx, var wy, var wz) = (w * x, w * y, w * z);
            (var xx, var xy, var xz) = (x * x, x * y, x * z);
            (var yy, var yz, var zz) = (y * y, y * z, z * z);

            this.AxisX = new Direction(1.0 - 2.0 * (yy + zz),       2.0 * (xy + wz),       2.0 * (xz - wy));
            this.AxisY = new Direction(      2.0 * (xy - wz), 1.0 - 2.0 * (xx + zz),       2.0 * (yz + wx));
            this.AxisZ = new Direction(      2.0 * (xz + wy),       2.0 * (yz - wx), 1.0 - 2.0 * (xx + yy));
        }

        public RotMatrix(in Direction reference, in Axes referenceAxis, Vector? next = null)
        {
            Direction second, third;
            if (next.HasValue)
            {
                second = reference.MakePerp(next.Value, out third);
            }
            else
            {
                second = reference.Perp(out third);
            }
            switch (referenceAxis)
            {
                case Axes.X:
                    AxisX = reference;
                    AxisY = second;
                    AxisZ = third;
                    break;
                case Axes.Y:
                    AxisY = reference;
                    AxisZ = second;
                    AxisX = third;
                    break;
                default:
                    AxisZ = reference;
                    AxisX = second;
                    AxisY = third;
                    break;
            }
        }


        public (Direction x, Direction y, Direction z) Rows() => (
                new Direction(xx, xy, xz, true),
                new Direction(yx, yy, yz, true),
                new Direction(zx, zy, zz, true));

        public Direction RowX() => new Direction(xx, xy, xz, true);

        public Direction RowY() => new Direction(yx, yy, yz, true);

        public Direction RowZ() => new Direction(zx, zy, zz, true);

        public Vector Mul(in Vector col) => //AxisX.Mul(col.x) + AxisY.Mul(col.y) + AxisZ.Mul(col.z);
            new Vector(
            xx * col.x + xy * col.y + xz * col.z,
            yx * col.x + yy * col.y + yz * col.z,
            zx * col.x + zy * col.y + zz * col.z);

        public Vector RightMul(in Vector row) => new Vector(
            AxisX.Dot(row),
            AxisY.Dot(row),
            AxisZ.Dot(row));

        public D2.Vector RightMul2d(in Vector row) => new D2.Vector(
            AxisX.Dot(row),
            AxisY.Dot(row));

        public Vector Mul(in D2.Vector col) => //AxisX.Mul(col.x) + AxisY.Mul(col.y) + AxisZ.Mul(col.z);
            new Vector(
            xx * col.x + xy * col.y,
            yx * col.x + yy * col.y,
            zx * col.x + zy * col.y);


        public Direction Mul(in Direction col) =>
            new Direction(
            xx * col.x + xy * col.y + xz * col.z,
            yx * col.x + yy * col.y + yz * col.z,
            zx * col.x + zy * col.y + zz * col.z);

        public Direction RightMul(in Direction row) => 
            new Direction(
            AxisX.Dot(row),
            AxisY.Dot(row),
            AxisZ.Dot(row));

        public Direction Mul(in D2.Direction col) =>
            new Direction(
            xx * col.x + xy * col.y,
            yx * col.x + yy * col.y,
            zx * col.x + zy * col.y);


        public static Vector operator *(in RotMatrix r, in Vector v) => r.Mul(v);
        
        public static Vector operator *(in RotMatrix r, in D2.Vector v) => r.Mul(v);
        
        public static Direction operator *(in RotMatrix r, in Direction v) => r.Mul(v);

        public static Direction operator *(in RotMatrix r, in D2.Direction v) => r.Mul(v);


        public static Vector operator *(in Vector v, in RotMatrix r) => r.RightMul(v); 
        
        public static Direction operator *(in Direction v, in RotMatrix r) => r.RightMul(v);


        public override string ToString()
        {
            var lineX = new[] { xx.ToString("G17", CultureInfo.InvariantCulture), xy.ToString("G17", CultureInfo.InvariantCulture), xz.ToString("G17", CultureInfo.InvariantCulture) };
            var lineY = new[] { yx.ToString("G17", CultureInfo.InvariantCulture), yy.ToString("G17", CultureInfo.InvariantCulture), yz.ToString("G17", CultureInfo.InvariantCulture) };
            var lineZ = new[] { zx.ToString("G17", CultureInfo.InvariantCulture), zy.ToString("G17", CultureInfo.InvariantCulture), zz.ToString("G17", CultureInfo.InvariantCulture) };
            var maxX = Math.Max(lineX[0].Length, Math.Max(lineY[0].Length, lineZ[0].Length));
            var maxY = Math.Max(lineX[1].Length, Math.Max(lineY[1].Length, lineZ[1].Length));
            var maxZ = Math.Max(lineX[2].Length, Math.Max(lineY[2].Length, lineZ[2].Length));
            var fX = "[{0,-" + maxX;
            var fY = "} {1,-" + maxY;
            var fZ = "} {2,-" + maxZ + "}]";
            var f = fX + fY + fZ;

            var sb = new StringBuilder();
            sb.AppendLine(string.Format(f, lineX[0], lineX[1], lineX[2]));
            sb.AppendLine(string.Format(f, lineY[0], lineY[1], lineY[2]));
            sb.AppendLine(string.Format(f, lineZ[0], lineZ[1], lineZ[2]));

            return sb.ToString();
        }

    }
}
