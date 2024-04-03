using System;
using System.Globalization;
using System.Text;

using static System.Math;

namespace GeometryLib.Double.D3
{
    public readonly struct SpdMatrix
    {
        private static readonly SpdMatrix unit = new SpdMatrix(
            1.0, 0.0, 0.0,
                 1.0, 0.0,
                      1.0);

        private static readonly SpdMatrix zero = new SpdMatrix(
             0.0, 0.0, 0.0,
                  0.0, 0.0,
                       0.0);

        public static ref readonly SpdMatrix Unit => ref unit;

        public static ref readonly SpdMatrix Zero => ref zero;

        public readonly double xx, xy, xz, yy, yz, zz;

        public SpdMatrix(
            double xx, double xy, double xz,
                       double yy, double yz,
                                  double zz)
        {
            this.xx = xx;
            this.xy = xy;
            this.xz = xz;
            this.yy = yy;
            this.yz = yz;
            this.zz = zz;
        }

        public void Deconstruct(
            out double outXx, out double outXy, out double outXz,
                              out double outYy, out double outYz,
                                                out double outZz)
        {
            outXx = xx;
            outXy = xy;
            outXz = xz;
            outYy = yy;
            outYz = yz;
            outZz = zz;
        }

        public (double xx, double xy, double xz,
                           double yy, double yz,
                                      double zz) Tuple => (xx, xy, xz, yy, yz, zz);

        public double[] ToArray() => new[]
        {
            xx,
            xy,
            xz,
            yy,
            yz,
            zz
        };

        public static SpdMatrix FromArray(in double[] sym) => new SpdMatrix(
            sym[0],
            sym[1],
            sym[2],
            sym[3],
            sym[4],
            sym[5]
            );

        public Vector Deviation() => new Vector(Math.Sqrt(xx), Math.Sqrt(yy), Math.Sqrt(zz));

        public static explicit operator Matrix(SpdMatrix sym)
        {
            return new Matrix
            (
                sym.xx, sym.xy, sym.xz,
                sym.xy, sym.yy, sym.yz,
                sym.xz, sym.yz, sym.zz
            );
        }

        public static SpdMatrix Diag(in double d) => new SpdMatrix(
            d, 0.0, 0.0,
                 d, 0.0,
                      d);

        public static SpdMatrix Diag(in Vector v) => new SpdMatrix(
            v.x, 0.0, 0.0,
                 v.y, 0.0,
                      v.z);

        public Vector Diag() => new Vector(xx, yy, zz);

        public Vector DiagSqrt() => new Vector(Math.Sqrt(xx), Math.Sqrt(yy), Math.Sqrt(zz));

        public SpdMatrix AddCov(in Vector v) => new SpdMatrix(
                xx + v.x * v.x, xy + v.y * v.x, xz + v.z * v.x,
                                yy + v.y * v.y, yz + v.z * v.y,
                                                zz + v.z * v.z);

        public static SpdMatrix MulMtM(in Matrix m)
        {
            (var xx, var xy, var xz, var yx, var yy, var yz, var zx, var zy, var zz) = m;
            return new SpdMatrix(
                xx * xx + yx * yx + zx * zx, xy * xx + yy * yx + zy * zx, xz * xx + yz * yx + zz * zx,
                                             xy * xy + yy * yy + zy * zy, xz * xy + yz * yy + zz * zy,
                                                                          xz * xz + yz * yz + zz * zz);
        }

        public static SpdMatrix operator *(in SpdMatrix m, in Quaternion q)
        {

            (var sxx, var sxy, var sxz, var syy, var syz, var szz) = m;
            (var x, var y, var z, var w) = q;
            var s = 2.0 / (w * w + x * x + y * y + z * z);
            (var wx, var wy, var wz) = (s * w * x, s * w * y, s * w * z);
            (var xx, var xy, var xz) = (s * x * x, s * x * y, s * x * z);
            (var yy, var yz, var zz) = (s * y * y, s * y * z, s * z * z);
            return new SpdMatrix(
                sxx * (1.0 - zz - yy) + sxy * (xy + wz) + sxz * (xz - wy),
                sxy * (1.0 - zz - yy) + syy * (xy + wz) + syz * (xz - wy),
                sxz * (1.0 - zz - yy) + syz * (xy + wz) + szz * (xz - wy),
                syy * (1.0 - zz - xx) + sxy * (xy - wz) + syz * (yz + wx),
                syz * (1.0 - zz - xx) + sxz * (xy - wz) + szz * (yz + wx),
                szz * (1.0 - yy - xx) + sxz * (xz + wy) + syz * (yz - wx));
        }


        public Matrix Mul(in SpdMatrix b)
        {
            return new Matrix(
                b.xz * xz + b.xy * xy + b.xx * xx, b.yz * xz + b.yy * xy + b.xy * xx, b.zz * xz + b.yz * xy + b.xz * xx,
                b.xz * yz + b.xy * yy + b.xx * xy, b.yz * yz + b.yy * yy + b.xy * xy, b.zz * yz + b.yz * yy + b.xz * xy,
                b.xz * zz + b.xy * yz + b.xx * xz, b.yz * zz + b.yy * yz + b.xy * xz, b.zz * zz + b.yz * yz + b.xz * xz);
        }

        public Vector Mul(Vector vec) =>
            new Vector(
            xx * vec.x + xy * vec.y + xz * vec.z,
            xy * vec.x + yy * vec.y + yz * vec.z,
            xz * vec.x + yz * vec.y + zz * vec.z);

        public Matrix Mul(in Matrix m)
        {
            return new Matrix(
                m.zx * xz + m.yx * xy + m.xx * xx, m.zy * xz + m.yy * xy + m.xy * xx, m.zz * xz + m.yz * xy + m.xz * xx,
                m.zx * yz + m.yx * yy + m.xx * xy, m.zy * yz + m.yy * yy + m.xy * xy, m.zz * yz + m.yz * yy + m.xz * xy,
                m.zx * zz + m.yx * yz + m.xx * xz, m.zy * zz + m.yy * yz + m.xy * xz, m.zz * zz + m.yz * yz + m.xz * xz);
        }

        public double RowMulSymMulCol(in Vector vec) => vec.Dot(Mul(vec));

        public SpdMatrix MatMulSymMulMatTrans(in Matrix m) => new SpdMatrix(
                m.xz * m.xz * zz + m.xy * m.xy * yy + m.xx * m.xx * xx + 2.0 * (m.xy * m.xz * yz + m.xx * m.xz * xz + m.xx * m.xy * xy), 
                m.xz * m.yz * zz + (m.xy * m.yz + m.xz * m.yy) * yz + m.xy * m.yy * yy + (m.xx * m.yz + m.xz * m.yx) * xz + (m.xx * m.yy + m.xy * m.yx) * xy + m.xx * m.yx * xx, 
                m.xz * m.zz * zz + (m.xy * m.zz + m.xz * m.zy) * yz + m.xy * m.zy * yy + (m.xx * m.zz + m.xz * m.zx) * xz + (m.xx * m.zy + m.xy * m.zx) * xy + m.xx * m.zx * xx,
                m.yz * m.yz * zz + m.yy * m.yy * yy + m.yx * m.yx * xx + 2.0 * (m.yy * m.yz * yz + m.yx * m.yz * xz + m.yx * m.yy * xy), 
                m.yz * m.zz * zz + (m.yy * m.zz + m.yz * m.zy) * yz + m.yy * m.zy * yy + (m.yx * m.zz + m.yz * m.zx) * xz + (m.yx * m.zy + m.yy * m.zx) * xy + m.yx * m.zx * xx,
                m.zz * m.zz * zz + m.zy * m.zy * yy + m.zx * m.zx * xx + 2.0 * (m.zy * m.zz * yz + m.zx * m.zz * xz + m.zx * m.zy * xy)
            );

        public SpdMatrix Mul(in double b)
        {
            return new SpdMatrix(
                b * xx, b * xy, b * xz,
                        b * yy, b * yz,
                                b * zz);
        }

        public SpdMatrix Add(in SpdMatrix b)
        {
            return new SpdMatrix(
                    xx + b.xx, xy + b.xy, xz + b.xz,
                               yy + b.yy, yz + b.yz,
                                          zz + b.zz);
        }

        public SpdMatrix Add(in double b)
        {
            return new SpdMatrix(
                b + xx, b + xy, b + xz,
                        b + yy, b + yz,
                                b + zz);
        }

        public SpdMatrix Sub(in SpdMatrix b)
        {
            return new SpdMatrix(
                    xx - b.xx, xy - b.xy, xz - b.xz,
                               yy - b.yy, yz - b.yz,
                                          zz - b.zz);
        }

        public SpdMatrix RightSub(in double b)
        {
            return new SpdMatrix(
                b - xx, b - xy, b - xz,
                        b - yy, b - yz,
                                b - zz);
        }

        internal TriMatrix Cholesky()
        {
            // Chol L
            var xx = Math.Sqrt(this.xx);
            var xy = this.xy / xx;
            var xz = this.xz / xx;
            var tyy = this.yy - xy * xy;
            var yy = Math.Sqrt(this.yy - xy * xy);
            var yz = (this.yz - xy * xz) / yy;
            var zz = Math.Sqrt(this.zz - xz * xz - yz * yz);

            return new TriMatrix(xx, xy, xz, yy, yz, zz);
        }

        public SpdMatrix Inv()
        {
            var (a, b, c, d, e, f) = this;
            var adbb = a * d - b * b;
            var bcae = b * c - a * e;
            var becd = b * e - c * d;
            var r = 1.0 / (bcae * e + adbb * f + becd * c);
            var xx = (d * f - e * e) * r;
            var xy = (c * e - b * f) * r;
            var xz = becd * r;
            var yy = (a * f - c * c) * r;
            var yz = bcae * r;
            var zz = adbb * r;

            // Test
            //var m1 = CholeskyInv();
            //var m2 = new SymMatrix(xx, xy, xz, yy, yz, zz);
            //var mm1 = this * m1;
            //var mm2 = this * m2;


            return new SpdMatrix(xx, xy, xz, yy, yz, zz);
        }


        public SpdMatrix CholeskyInv()
        {
            // alternative mehr Berechnungen, aber besser bei großen Werten
            return Cholesky().Inv().SelfMul();
        }


        public static Matrix operator *(in SpdMatrix a, in SpdMatrix b) => a.Mul(b);

        public static SpdMatrix operator +(in SpdMatrix a, in SpdMatrix b) => a.Add(b);

        public static SpdMatrix operator -(in SpdMatrix a, in SpdMatrix b) => a.Sub(b);

        public static SpdMatrix operator *(in SpdMatrix a, in double b) => a.Mul(b);

        public static SpdMatrix operator +(in SpdMatrix a, in double b) => a.Add(b);

        public static SpdMatrix operator -(in SpdMatrix a, in double b) => a.Add(-b);

        public static SpdMatrix operator *(in double a, in SpdMatrix b) => b.Mul(a);

        public static SpdMatrix operator +(in double a, in SpdMatrix b) => b.Add(a);

        public static SpdMatrix operator -(in double a, in SpdMatrix b) => b.RightSub(a);

        public static Vector operator *(in Vector a, in SpdMatrix b) => b.Mul(a);

        public static Vector operator *(in SpdMatrix a, in Vector b) => a.Mul(b);

        public static Matrix operator *(in SpdMatrix a, in Matrix b) => a.Mul(b);

        public override string ToString()
        {
            var lineX = new[] { xx.ToString("G17", CultureInfo.InvariantCulture), xy.ToString("G17", CultureInfo.InvariantCulture), xz.ToString("G17", CultureInfo.InvariantCulture) };
            var lineY = new[] { yy.ToString("G17", CultureInfo.InvariantCulture), yz.ToString("G17", CultureInfo.InvariantCulture) };
            var lineZ = zz.ToString("G17", CultureInfo.InvariantCulture);
            var maxX = lineX[0].Length;
            var maxY = Max(lineX[1].Length, lineY[0].Length);
            var maxZ = Max(lineX[2].Length, Max(lineY[1].Length, lineZ.Length));
            var fX = "[{0,-" + maxX;
            var fY = "} {1,-" + maxY;
            var fZ = "} {2,-" + maxZ + "}]";
            var f = fX + fY + fZ;

            var sb = new StringBuilder();
            sb.AppendLine(string.Format(f, lineX[0], lineX[1], lineX[2]));
            sb.AppendLine(string.Format(f, "", lineY[0], lineY[1]));
            sb.AppendLine(string.Format(f, "", "", lineZ));

            return sb.ToString();
        }

        public string ToArrayString(string separator = " ")
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:G17}{6}{1:G17}{6}{2:G17}{6}{3:G17}{6}{4:G17}{6}{5:G17}", xx, xy, xz, yy, yz, zz, separator);
        }



    }
}
