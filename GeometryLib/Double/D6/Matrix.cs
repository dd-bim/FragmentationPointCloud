using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeometryLib.Double.D6
{
    public readonly struct Matrix
    {
        private static readonly Matrix unit = new Matrix(
            1.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 1.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 1.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 1.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 1.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 1.0);

        private static readonly Matrix zero = new Matrix(
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0);

        public static ref readonly Matrix Unit => ref unit;

        public static ref readonly Matrix Zero => ref zero;

        public readonly double 
            m00, m01, m02, m03, m04, m05, 
            m10, m11, m12, m13, m14, m15, 
            m20, m21, m22, m23, m24, m25, 
            m30, m31, m32, m33, m34, m35, 
            m40, m41, m42, m43, m44, m45,
            m50, m51, m52, m53, m54, m55;

        public Matrix(
            in double m00, in double m01, in double m02, in double m03, in double m04, in double m05, 
            in double m10, in double m11, in double m12, in double m13, in double m14, in double m15, 
            in double m20, in double m21, in double m22, in double m23, in double m24, in double m25, 
            in double m30, in double m31, in double m32, in double m33, in double m34, in double m35, 
            in double m40, in double m41, in double m42, in double m43, in double m44, in double m45, 
            in double m50, in double m51, in double m52, in double m53, in double m54, in double m55)
        {
            this.m00 = m00;
            this.m01 = m01;
            this.m02 = m02;
            this.m03 = m03;
            this.m04 = m04;
            this.m05 = m05;
            this.m10 = m10;
            this.m11 = m11;
            this.m12 = m12;
            this.m13 = m13;
            this.m14 = m14;
            this.m15 = m15;
            this.m20 = m20;
            this.m21 = m21;
            this.m22 = m22;
            this.m23 = m23;
            this.m24 = m24;
            this.m25 = m25;
            this.m30 = m30;
            this.m31 = m31;
            this.m32 = m32;
            this.m33 = m33;
            this.m34 = m34;
            this.m35 = m35;
            this.m40 = m40;
            this.m41 = m41;
            this.m42 = m42;
            this.m43 = m43;
            this.m44 = m44;
            this.m45 = m45;
            this.m50 = m50;
            this.m51 = m51;
            this.m52 = m52;
            this.m53 = m53;
            this.m54 = m54;
            this.m55 = m55;
        }

        public void Deconstruct(
            out double m00, out double m01, out double m02, out double m03, out double m04, out double m05,
            out double m10, out double m11, out double m12, out double m13, out double m14, out double m15,
            out double m20, out double m21, out double m22, out double m23, out double m24, out double m25,
            out double m30, out double m31, out double m32, out double m33, out double m34, out double m35,
            out double m40, out double m41, out double m42, out double m43, out double m44, out double m45,
            out double m50, out double m51, out double m52, out double m53, out double m54, out double m55)
        {
            m00 = this.m00;
            m01 = this.m01;
            m02 = this.m02;
            m03 = this.m03;
            m04 = this.m04;
            m05 = this.m05;
            m10 = this.m10;
            m11 = this.m11;
            m12 = this.m12;
            m13 = this.m13;
            m14 = this.m14;
            m15 = this.m15;
            m20 = this.m20;
            m21 = this.m21;
            m22 = this.m22;
            m23 = this.m23;
            m24 = this.m24;
            m25 = this.m25;
            m30 = this.m30;
            m31 = this.m31;
            m32 = this.m32;
            m33 = this.m33;
            m34 = this.m34;
            m35 = this.m35;
            m40 = this.m40;
            m41 = this.m41;
            m42 = this.m42;
            m43 = this.m43;
            m44 = this.m44;
            m45 = this.m45;
            m50 = this.m50;
            m51 = this.m51;
            m52 = this.m52;
            m53 = this.m53;
            m54 = this.m54;
            m55 = this.m55;
        }


    }

}
