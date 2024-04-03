using System.Globalization;
using System.Text;

using static System.Math;

namespace GeometryLib.Double.D3
{
    public readonly struct Matrix
    {
        private static readonly Matrix unit = new Matrix(
            1.0, 0.0, 0.0,
            0.0, 1.0, 0.0,
            0.0, 0.0, 1.0);

        private static readonly Matrix zero = new Matrix(
             0.0, 0.0, 0.0,
             0.0, 0.0, 0.0,
             0.0, 0.0, 0.0);

        public static ref readonly Matrix Unit => ref unit;

        public static ref readonly Matrix Zero => ref zero;


        public readonly double xx, xy, xz, yx, yy, yz, zx, zy, zz;

        public Matrix(
            double xx, double xy, double xz,
            double yx, double yy, double yz,
            double zx, double zy, double zz)
        {
            this.xx = xx;
            this.xy = xy;
            this.xz = xz;
            this.yx = yx;
            this.yy = yy;
            this.yz = yz;
            this.zx = zx;
            this.zy = zy;
            this.zz = zz;
        }

        public static Matrix Diag(in double d) => new Matrix(
            d, 0.0, 0.0,
            0.0, d, 0.0,
            0.0, 0.0, d);


        public static Matrix Diag(in Vector v) => new Matrix(
            v.x, 0.0, 0.0,
            0.0, v.y, 0.0,
            0.0, 0.0, v.z);

        public Vector Diag() => new Vector(xx, yy, zz);



        public static Matrix FromRows(Vector rowx, Vector rowy, Vector rowz)
        {
            return new Matrix(
                rowx.x, rowx.y, rowx.z,
                rowy.x, rowy.y, rowy.z,
                rowz.x, rowz.y, rowz.z
                );
        }

        public static Matrix FromCols(Vector colx, Vector coly, Vector colz)
        {
            return new Matrix(
                colx.x, coly.x, colz.x,
                colx.y, coly.y, colz.y,
                colx.z, coly.z, colz.z
                );
        }

        public void Deconstruct(
            out double xx, out double xy, out double xz,
            out double yx, out double yy, out double yz,
            out double zx, out double zy, out double zz)
        {
            xx = this.xx;
            xy = this.xy;
            xz = this.xz;
            yx = this.yx;
            yy = this.yy;
            yz = this.yz;
            zx = this.zx;
            zy = this.zy;
            zz = this.zz;
        }

        public (double xx, double xy, double xz,
                double yx, double yy, double yz,
                double zx, double zy, double zz) Tuple => (xx, xy, xz, yx, yy, yz, zx, zy, zz);


        public (Vector x, Vector y, Vector z) Rows() => (
                new Vector(xx, xy, xz),
                new Vector(yx, yy, yz),
                new Vector(zx, zy, zz));

        public Vector RowX() => new Vector(xx, xy, xz);

        public Vector RowY() => new Vector(yx, yy, yz);

        public Vector RowZ() => new Vector(zx, zy, zz);

        public (Vector x, Vector y, Vector z) Cols() => (
                new Vector(xx, yx, zx),
                new Vector(xy, yy, zy),
                new Vector(xz, yz, zz));

        public Vector ColX() => new Vector(xx, yx, zx);

        public Vector ColY() => new Vector(xy, yy, zy);

        public Vector ColZ() => new Vector(xz, yz, zz);

        public Matrix Trans() => new Matrix(
            xx, yx, zx, 
            xy, yy, zy, 
            xz, yz, zz);

        public Matrix Mul(Matrix b)
        {
            return new Matrix(
                b.zx * xz + b.yx * xy + b.xx * xx, b.zy * xz + b.yy * xy + b.xy * xx, b.zz * xz + b.yz * xy + b.xz * xx,
                b.zx * yz + b.yx * yy + b.xx * yx, b.zy * yz + b.yy * yy + b.xy * yx, b.zz * yz + b.yz * yy + b.xz * yx,
                b.zx * zz + b.yx * zy + b.xx * zx, b.zy * zz + b.yy * zy + b.xy * zx, b.zz * zz + b.yz * zy + b.xz * zx);
        }

        public Matrix Mul(SpdMatrix b)
        {
            return new Matrix(
                b.xz * xz + b.xy * xy + b.xx * xx, b.yz * xz + b.yy * xy + b.xy * xx, b.zz * xz + b.yz * xy + b.xz * xx,
                b.xz * yz + b.xy * yy + b.xx * yx, b.yz * yz + b.yy * yy + b.xy * yx, b.zz * yz + b.yz * yy + b.xz * yx,
                b.xz * zz + b.xy * zy + b.xx * zx, b.yz * zz + b.yy * zy + b.xy * zx, b.zz * zz + b.yz * zy + b.xz * zx);
        }

        public Vector Mul(Vector b)
        {
            return new Vector(
                b.z * xz + b.y * xy + b.x * xx,
                b.z * yz + b.y * yy + b.x * yx,
                b.z * zz + b.y * zy + b.x * zx);
        }

        public Vector RightMul(Vector a)
        {
            return new Vector(
                zx * a.z + yx * a.y + xx * a.x, 
                zy * a.z + yy * a.y + xy * a.x, 
                zz * a.z + yz * a.y + xz * a.x);
        }

        public Matrix Mul(double b)
        {
            return new Matrix(
                b * xx, b * xy, b * xz,
                b * yx, b * yy, b * yz,
                b * zx, b * zy, b * zz);
        }

        public Matrix Add(Matrix b)
        {
            return new Matrix(
                    xx + b.xx, xy + b.xy, xz + b.xz,
                    yx + b.yx, yy + b.yy, yz + b.yz,
                    zx + b.zx, zy + b.zy, zz + b.zz);
        }

        public Matrix Add(double b)
        {
            return new Matrix(
                    xx + b, xy + b, xz + b,
                    yx + b, yy + b, yz + b,
                    zx + b, zy + b, zz + b);
        }

        public Matrix Sub(Matrix b)
        {
            return new Matrix(
                    xx - b.xx, xy - b.xy, xz - b.xz,
                    yx - b.yx, yy - b.yy, yz - b.yz,
                    zx - b.zx, zy - b.zy, zz - b.zz);
        }

        public Matrix RightSub(double b)
        {
            return new Matrix(
                    b - xx, b - xy, b - xz,
                    b - yx, b - yy, b - yz,
                    b - zx, b - zy, b - zz);
        }

        public static Matrix operator *(Matrix m, Quaternion q)
        {
            (var mxx, var mxy, var mxz, var myx, var myy, var myz, var mzx, var mzy, var mzz) = m;
            (var x, var y, var z, var w) = q;
            var s = 2.0 / (w * w + x * x + y * y + z * z);
            (var wx, var wy, var wz) = (s * w * x, s * w * y, s * w * z);
            (var xx, var xy, var xz) = (s * x * x, s * x * y, s * x * z);
            (var yy, var yz, var zz) = (s * y * y, s * y * z, s * z * z);
            return new Matrix(
                  mxx * (1.0 - zz - yy) + mxy * (xy + wz) + mxz * (xz - wy),
                  mxy * (1.0 - zz - xx) + mxx * (xy - wz) + mxz * (yz + wx),
                  mxz * (1.0 - yy - xx) + mxx * (xz + wy) + mxy * (yz - wx),

                  myx * (1.0 - zz - yy) + myy * (xy + wz) + myz * (xz - wy),
                  myy * (1.0 - zz - xx) + myx * (xy - wz) + myz * (yz + wx),
                  myz * (1.0 - yy - xx) + myx * (xz + wy) + myy * (yz - wx),

                  mzx * (1.0 - zz - yy) + mzy * (xy + wz) + mzz * (xz - wy),
                  mzy * (1.0 - zz - xx) + mzx * (xy - wz) + mzz * (yz + wx),
                  mzz * (1.0 - yy - xx) + mzx * (xz + wy) + mzy * (yz - wx));
        }

        public static Matrix operator *(Matrix a, Matrix b) => a.Mul(b);

        public static Matrix operator +(Matrix a, Matrix b) => a.Add(b);

        public static Matrix operator -(Matrix a, Matrix b) => a.Sub(b);

        public static Vector operator *(Matrix a, Vector b) => a.Mul(b);

        public static Matrix operator *(Matrix a, SpdMatrix b) => a.Mul(b);

        public static Vector operator *(Vector a, Matrix b) => b.RightMul(a);

        public static Matrix operator *(Matrix a, double b) => a.Mul(b);

        public static Matrix operator +(Matrix a, double b) => a.Add(b);

        public static Matrix operator -(Matrix a, double b) => a.Add(-b);

        public static Matrix operator *(double a, Matrix b) => b.Mul(a);

        public static Matrix operator +(double a, Matrix b) => b.Add(a);

        public static Matrix operator -(double a, Matrix b) => b.RightSub(a);

        public override string ToString()
        {
            var lineX = new[] { xx.ToString("G17", CultureInfo.InvariantCulture), xy.ToString("G17", CultureInfo.InvariantCulture), xz.ToString("G17", CultureInfo.InvariantCulture) };
            var lineY = new[] { yx.ToString("G17", CultureInfo.InvariantCulture), yy.ToString("G17", CultureInfo.InvariantCulture), yz.ToString("G17", CultureInfo.InvariantCulture) };
            var lineZ = new[] { zx.ToString("G17", CultureInfo.InvariantCulture), zy.ToString("G17", CultureInfo.InvariantCulture), zz.ToString("G17", CultureInfo.InvariantCulture) };
            var maxX = Max(lineX[0].Length, Max(lineY[0].Length, lineZ[0].Length));
            var maxY = Max(lineX[1].Length, Max(lineY[1].Length, lineZ[1].Length));
            var maxZ = Max(lineX[2].Length, Max(lineY[2].Length, lineZ[2].Length));
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
