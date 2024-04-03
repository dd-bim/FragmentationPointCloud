using static System.Math;

namespace GeometryLib.Double.D3
{
    public static class Decomposition
    {
        private static readonly double GAMMA = Sqrt(8.0) + 3.0;
        private static readonly double t = (2.0 - Sqrt(2.0)) / 4.0;
        private static readonly double CPI8 = Sqrt(1.0 - t);//cos (Math.PI / 8.)
        private static readonly double SPI8 = Sqrt(t); //sin (Math.PI / 8.)
        private static readonly double EPSILON = 1.0e-15;


        private static (double, double) approxGivQuat(double a11, double a12, double a22)
        {
            var ch = 2.0 * (a11 - a22);
            var chch = ch * ch;
            var shsh = a12 * a12;
            if (GAMMA * shsh < chch)
            {
                var w = 1.0 / Sqrt(chch + shsh); // hier würde eine Approximation reichen (rsqrt)
                return (w * ch, w * a12);
            }
            else
            {
                return (CPI8, SPI8);
            }
        }
        
        private static (Quaternion, SpdMatrix) jacConj(int xi, int yi, int zi, SpdMatrix s, Quaternion q)
        {
            const int wi = 3;
            (var ch, var sh) = approxGivQuat(s.xx, s.xy, s.yy);
            // var scale = ch * ch + sh * sh;// ist hier immer 1 da exakte Wurzelberechnung
            var a = (ch * ch - sh * sh);// / scale
            var b = (2.0 * sh * ch);// / scale

            // (x,y,z) corresponds to ((0,1,2),(1,2,0),(2,0,1))
            // for (p,q) = ((0,1),(1,2),(0,2))
            var nq = new double[4];
            nq[xi] = ch * q[xi] + sh * q[yi];
            nq[yi] = ch * q[yi] - sh * q[xi];
            nq[zi] = ch * q[zi] + sh * q[wi];
            nq[wi] = ch * q[wi] - sh * q[zi];
            // perform conjugation S = Q'*S*Q
            // re-arrange matrix for next iteration
            return (new Quaternion(nq[0], nq[1], nq[2], nq[3]),
                new SpdMatrix(
                -b * (-b * s.xx + a * s.xy) + a * (-b * s.xy + a * s.yy),
                -b * s.xz + a * s.yz,
                a * (-b * s.xx + a * s.xy) + b * (-b * s.xy + a * s.yy),
                s.zz,
                a * s.xz + b * s.yz,
                a * (a * s.xx + b * s.xy) + b * (a * s.xy + b * s.yy)));
            //    double xx,
            //    double yx, 
            //    double yy,
            //    double zx, 
            //    double zy, 
            //    double zz)
            //            new SymMat(
            //-b * (-b * s.xx + a * s.xy) + a * (-b * s.xy + a * s.yy),
            //-b * s.xz + a * s.yz,
            //s.zz,
            //a * (-b * s.xx + a * s.xy) + b * (-b * s.xy + a * s.yy),
            //a * s.xz + b * s.yz,
            //a * (a * s.xx + b * s.xy) + b * (a * s.xy + b * s.yy)));

        }

        // finds transformation that diagonalizes a symmetric matrix
        private static Quaternion jacEigen(SpdMatrix s)
        {
            var q = new Quaternion(0.0, 0.0, 0.0, 1.0);
            for (var i = 0; i < 6; i++)
            {
                // 6 Iterationen reichen hier (default 4)
                // we wish to eliminate the maximum off-diagonal element
                // on every iteration, but cycling over all 3 possible rotations
                // in fixed order (p,q) = (1,2) , (2,3), (1,3) still retains
                //  asymptotic convergence
                (q, s) = jacConj(0, 1, 2, s, q); // p,q = 0,1
                (q, s) = jacConj(1, 2, 0, s, q); // p,q = 1,2
                (q, s) = jacConj(2, 0, 1, s, q); // p,q = 0,2   
            }
            return q;
        }

        private static double dist2(double x, double y, double z) => x * x + y * y + z * z;

        private static Quaternion sortBySingVals(SpdMatrix s, Quaternion q)
        {
            (var x, var y, var z, var w) = q;
            var rho1 = dist2(s.xx, s.xy, s.xz);
            var rho2 = dist2(s.xy, s.yy, s.yz);
            var rho3 = dist2(s.xz, s.yz, s.zz);

            var c = rho1 < rho2;
            (x, y, z, w, rho1, rho2) = c ? (y + x, y - x, z + w, w - z, rho2, rho1) : (x, y, z, w, rho1, rho2);

            c = rho1 < rho3;
            (x, y, z, w, rho3) = c ? (x - z, y + w, z + x, w - y, rho1) : (x, y, z, w, rho3);

            c = rho2 < rho3;
            (x, y, z, w) = c ? (x + w, z + y, z - y, w - x) : (x, y, z, w);

            return new Quaternion(x, y, z, w);
        }

        private static ((double, double, double), (double, double, double)) condNegSwap(bool c, (double a, double b, double c) x, (double, double, double) y) => c ? (y, (-x.a, -x.b, -x.c)) : (x, y);


        private static (Quaternion, Matrix) sortSingVals(Matrix m, Quaternion q)
        {
            (var x, var y, var z, var w) = q;
            var rho1 = dist2(m.xx, m.yx, m.zx);
            var rho2 = dist2(m.xy, m.yy, m.zy);
            var rho3 = dist2(m.xz, m.yz, m.zz);

            var c = rho1 < rho2;
            ((var xx, var yx, var zx), (var xy, var yy, var zy)) = condNegSwap(c, (m.xx, m.yx, m.zx), (m.xy, m.yy, m.zy));
            (x, y, z, w, rho1, rho2) = c ? (y + x, y - x, z + w, w - z, rho2, rho1) : (x, y, z, w, rho1, rho2);

            c = rho1 < rho3;
            double xz, yz, zz;
            ((xx, yx, zx), (xz, yz, zz)) = condNegSwap(c, (xx, yx, zx), (m.xz, m.yz, m.zz));
            (x, y, z, w, rho3) = c ? (x - z, y + w, z + x, w - y, rho1) : (x, y, z, w, rho3);

            c = rho2 < rho3;
            ((xy, yy, zy), (xz, yz, zz)) = condNegSwap(c, (xy, yy, zy), (xz, yz, zz));
            (x, y, z, w) = c ? (x + w, z + y, z - y, w - x) : (x, y, z, w);
            return (new Quaternion(x, y, z, w),
             new Matrix(xx, xy, xz, yx, yy, yz, zx, zy, zz));
        }

        private static (double, double) givQuat(double a1, double a2)
        {
            // a1 = pivot point on diagonal
            // a2 = lower triangular entry we want to annihilate
            var rho = Sqrt(a1 * a1 + a2 * a2);
            var sh = rho > EPSILON ? a2 : 0.0;
            var ch = Abs(a1 + Max(rho, EPSILON));
            var w = 1.0 / Sqrt(ch * ch + sh * sh); // hier reicht eine Approximation (rsqrt)
            return a1 < 0.0 ? (sh * w, ch * w) : (ch * w, sh * w);
        }

        private static (Quaternion, Matrix) qr(Matrix m)
        {
            // first givens rotation (ch,0,0,sh)
            (var ch1, var sh1) = givQuat(m.xx, m.yx);
            var cos = 1.0 - 2.0 * sh1 * sh1;
            var sin = 2.0 * ch1 * sh1;
            (var rxx1, var rxy1, var rxz1) = (cos * m.xx + sin * m.yx, cos * m.xy + sin * m.yy, cos * m.xz + sin * m.yz);
            (var ryy1, var ryz1) = (cos * m.yy - sin * m.xy, cos * m.yz - sin * m.xz);

            // second givens rotation (ch,0,-sh,0)
            (var ch2, var sh2) = givQuat(rxx1, m.zx);
            cos = 1.0 - 2.0 * sh2 * sh2;
            sin = 2.0 * ch2 * sh2;
            (var rxx2, var rxy2, var rxz2) = (cos * rxx1 + sin * m.zx, cos * rxy1 + sin * m.zy, cos * rxz1 + sin * m.zz);
            (var rzy1, var rzz1) = (cos * m.zy - sin * rxy1, cos * m.zz - sin * rxz1);

            // third givens rotation (ch,sh,0,0)
            (var ch3, var sh3) = givQuat(ryy1, rzy1);
            cos = 1.0 - 2.0 * sh3 * sh3;
            sin = 2.0 * ch3 * sh3;
            (var ryy2, var ryz2) = (cos * ryy1 + sin * rzy1, cos * ryz1 + sin * rzz1);
            var rzz2 = cos * rzz1 - sin * ryz1;

            // Kombination der 3 Rotationen, Normierung nicht unbedingt nötig (da genaue Wurzelberechnung)
            return (new Quaternion(
                ch1 * ch2 * sh3 + sh1 * sh2 * ch3,
                sh1 * ch2 * sh3 - ch1 * sh2 * ch3,
                ch1 * sh2 * sh3 + sh1 * ch2 * ch3,
                ch1 * ch2 * ch3 - sh1 * sh2 * sh3),
                new Matrix(rxx2, rxy2, rxz2, 0, ryy2, ryz2, 0, 0, rzz2));
        }

        public static Quaternion EigenVectors(in SpdMatrix s)
        {
            var q = jacEigen(s);
            var ss = s * q;
            // sort singular values and find V
            q = sortBySingVals(ss, q);
            return q;
        }

        public static Vector SVD(in Matrix m, out Quaternion u, out Quaternion v)
        {
            Matrix s;
            var ata = SpdMatrix.MulMtM(m);
            // symmetric eigenalysis
            var q = jacEigen(ata);
            var b = m * q;
            // sort singular values and find V
            (v, s) = sortSingVals(b, q);
            // QR decomposition
            (u, s) = qr(s);
            return new Vector(s.xx, s.yy, s.zz);
        }

        public static Matrix QR(in Matrix m, out Quaternion q)
        {
            Matrix r;
            (q, r) = qr(m);
            return r;
        }


    }
}
