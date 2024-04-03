using System;
using System.Globalization;
using System.Text;

using static System.Math;


namespace GeometryLib.Double.D2
{
    public readonly struct SpdMatrix: Interfaces.ISpdMatrix
    {
        private static readonly SpdMatrix unit = new SpdMatrix(
            1.0, 0.0,
                 1.0);

        private static readonly SpdMatrix zero = new SpdMatrix(
             0.0, 0.0,
                  0.0);

        public static ref readonly SpdMatrix Unit => ref unit;

        public static ref readonly SpdMatrix Zero => ref zero;

        public readonly double xx, xy, yy;

        public SpdMatrix(double xx, double xy, double yy)
        {
            this.xx = xx;
            this.xy = xy;
            this.yy = yy;
        }


        public SpdMatrix(in D3.SpdMatrix mat)
        {
            this.xx = mat.xx;
            this.xy = mat.xy;
            this.yy = mat.yy;
        }

        public void Deconstruct(out double outXx, out double outXy, out double outYy)
        {
            outXx = xx;
            outXy = xy;
            outYy = yy;
        }

        public (double xx, double xy, double yy) Tuple => (xx, xy, yy);


        //public static explicit operator Matrix(SymMatrix sym)
        //{
        //    return new Matrix
        //    (
        //        sym.xx, sym.xy, sym.xz,
        //        sym.xy, sym.yy, sym.yz,
        //        sym.xz, sym.yz, sym.zz
        //    );
        //}

        public static SpdMatrix Diag(in double d) => new SpdMatrix(d, 0.0, d);

        public static SpdMatrix Diag(in Vector v) => new SpdMatrix(v.x, 0.0, v.y);

        public Vector Diag() => new Vector(xx, yy);

        public Vector DiagSqrt() => new Vector(Math.Sqrt(xx), Math.Sqrt(yy));

        public SpdMatrix AddCov(in Vector v) => new SpdMatrix(
                xx + v.x * v.x, xy + v.y * v.x,
                                yy + v.y * v.y);

        //public static SymMatrix MulMtM(in Matrix m)
        //{
        //    var (xx, xy, xz, yx, yy, yz, zx, zy, zz) = m;
        //    return new SymMatrix(
        //        xx * xx + yx * yx + zx * zx, xy * xx + yy * yx + zy * zx, xz * xx + yz * yx + zz * zx,
        //                                     xy * xy + yy * yy + zy * zy, xz * xy + yz * yy + zz * zy,
        //                                                                  xz * xz + yz * yz + zz * zz);
        //}

        //public static SymMatrix operator *(in SymMatrix m, in Quaternion q)
        //{

        //    var (sxx, sxy, sxz, syy, syz, szz) = m;
        //    var (x, y, z, w) = q;
        //    var s = 2.0 / (w * w + x * x + y * y + z * z);
        //    var (wx, wy, wz) = (s * w * x, s * w * y, s * w * z);
        //    var (xx, xy, xz) = (s * x * x, s * x * y, s * x * z);
        //    var (yy, yz, zz) = (s * y * y, s * y * z, s * z * z);
        //    return new SymMatrix(
        //        sxx * (1.0 - zz - yy) + sxy * (xy + wz) + sxz * (xz - wy),
        //        sxy * (1.0 - zz - yy) + syy * (xy + wz) + syz * (xz - wy),
        //        sxz * (1.0 - zz - yy) + syz * (xy + wz) + szz * (xz - wy),
        //        syy * (1.0 - zz - xx) + sxy * (xy - wz) + syz * (yz + wx),
        //        syz * (1.0 - zz - xx) + sxz * (xy - wz) + szz * (yz + wx),
        //        szz * (1.0 - yy - xx) + sxz * (xz + wy) + syz * (yz - wx));
        //}


        //public Matrix Mul(in SymMatrix b)
        //{
        //    return new Matrix(
        //        b.xz * xz + b.xy * xy + b.xx * xx, b.yz * xz + b.yy * xy + b.xy * xx, b.zz * xz + b.yz * xy + b.xz * xx,
        //        b.xz * yz + b.xy * yy + b.xx * xy, b.yz * yz + b.yy * yy + b.xy * xy, b.zz * yz + b.yz * yy + b.xz * xy,
        //        b.xz * zz + b.xy * yz + b.xx * xz, b.yz * zz + b.yy * yz + b.xy * xz, b.zz * zz + b.yz * yz + b.xz * xz);
        //}

        public SpdMatrix Mul(in double b) => new SpdMatrix(b * xx, b * xy, b * yy);

        public Vector Mul(Vector vec) =>
            new Vector(
            xx * vec.x + xy * vec.y,
            xy * vec.x + yy * vec.y);

        public double RowMulSymMulCol(in Vector vec) => vec.Dot(Mul(vec));


        public TriMatrix Cholesky()
        {
            // Chol L
            var xx = Math.Sqrt(this.xx);
            var xy = this.xy / xx;
            var yy = Math.Sqrt(this.yy - xy * xy);
            return new TriMatrix(xx, xy, yy);
        }

        //public SymMatrix Inv()
        //{
        //    var (a, b, c) = this;
        //    var r = 1.0 / (a * c - b * b);

        //    return new SymMatrix(c * r, -b * r, a * r);
        //}

        public SpdMatrix CholeskyInv()
        {
            // alternative mehr Berechnungen, aber besser bei großen Werten
            return Cholesky().Inv().SelfMul();
        }


        public SpdMatrix Add(in SpdMatrix b) => new SpdMatrix(xx + b.xx, xy + b.xy, yy + b.yy);

        public SpdMatrix Add(in double b) => new SpdMatrix(b + xx, b + xy, b + yy);

        public SpdMatrix Sub(in SpdMatrix b) => new SpdMatrix(xx - b.xx, xy - b.xy, yy - b.yy);

        public SpdMatrix RightSub(in double b) => new SpdMatrix(b - xx, b - xy, b - yy);

        //public static Matrix operator *(in SymMatrix a, in SymMatrix b) => a.Mul(b);

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

        public override string ToString()
        {
            var lineX = new[] { xx.ToString("G17", CultureInfo.InvariantCulture), xy.ToString("G17", CultureInfo.InvariantCulture) };
            var lineY = yy.ToString("G17", CultureInfo.InvariantCulture);
            var maxX = lineX[0].Length;
            var maxY = Max(lineX[1].Length, lineY.Length);
            var f = "[{0,-" + maxX + "} {1,-" + maxY + "}]";

            var sb = new StringBuilder();
            sb.AppendLine(string.Format(f, lineX[0], lineX[1]));
            sb.AppendLine(string.Format(f, "", lineY));

            return sb.ToString();
        }

        public string ToArrayString(string separator = " ")
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:G17}{3}{1:G17}{3}{2:G17}", xx, xy, yy, separator);
        }



    }
}
