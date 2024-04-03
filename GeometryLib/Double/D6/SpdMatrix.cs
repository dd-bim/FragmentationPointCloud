using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeometryLib.Double.D6
{
    public readonly struct SpdMatrix
    {
        public const int Length = 21;

        private static readonly SpdMatrix unit = new SpdMatrix(
            1.0, 0.0, 0.0, 0.0 ,0.0, 0.0,
                 1.0, 0.0, 0.0, 0.0, 0.0,
                      1.0, 0.0, 0.0, 0.0,
                           1.0, 0.0, 0.0,
                                1.0, 0.0, 
                                     1.0);

        private static readonly SpdMatrix zero = new SpdMatrix(
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
                 0.0, 0.0, 0.0, 0.0, 0.0,
                      0.0, 0.0, 0.0, 0.0,
                           0.0, 0.0, 0.0,
                                0.0, 0.0,
                                     0.0);

        public static ref readonly SpdMatrix Unit => ref unit;

        public static ref readonly SpdMatrix Zero => ref zero;

        public readonly double s00, s01, s02, s03, s04, s05, s11, s12, s13, s14, s15, s22, s23, s24, s25, s33, s34, s35, s44, s45, s55;

        public SpdMatrix(
            in double s00, in double s01, in double s02, in double s03, in double s04, in double s05, 
                           in double s11, in double s12, in double s13, in double s14, in double s15, 
                                          in double s22, in double s23, in double s24, in double s25, 
                                                         in double s33, in double s34, in double s35, 
                                                                        in double s44, in double s45, 
                                                                                       in double s55)
        {
            this.s00 = s00;
            this.s01 = s01;
            this.s02 = s02;
            this.s03 = s03;
            this.s04 = s04;
            this.s05 = s05;
            this.s11 = s11;
            this.s12 = s12;
            this.s13 = s13;
            this.s14 = s14;
            this.s15 = s15;
            this.s22 = s22;
            this.s23 = s23;
            this.s24 = s24;
            this.s25 = s25;
            this.s33 = s33;
            this.s34 = s34;
            this.s35 = s35;
            this.s44 = s44;
            this.s45 = s45;
            this.s55 = s55;
        }

        public void Deconstruct(
            out double s00, out double s01, out double s02, out double s03, out double s04, out double s05,
                            out double s11, out double s12, out double s13, out double s14, out double s15,
                                            out double s22, out double s23, out double s24, out double s25,
                                                            out double s33, out double s34, out double s35,
                                                                            out double s44, out double s45,
                                                                                            out double s55)
        {
            s00 = this.s00;
            s01 = this.s01;
            s02 = this.s02;
            s03 = this.s03;
            s04 = this.s04;
            s05 = this.s05;
            s11 = this.s11;
            s12 = this.s12;
            s13 = this.s13;
            s14 = this.s14;
            s15 = this.s15;
            s22 = this.s22;
            s23 = this.s23;
            s24 = this.s24;
            s25 = this.s25;
            s33 = this.s33;
            s34 = this.s34;
            s35 = this.s35;
            s44 = this.s44;
            s45 = this.s45;
            s55 = this.s55;
        }

        public double[] ToArray() => new[] 
        {
            s00,
            s01,
            s02,
            s03,
            s04,
            s05,
            s11,
            s12,
            s13,
            s14,
            s15,
            s22,
            s23,
            s24,
            s25,
            s33,
            s34,
            s35,
            s44,
            s45,
            s55
        };

        public static SpdMatrix FromArray(in double[] sym) => new SpdMatrix(
            sym[0],
            sym[1],
            sym[2],
            sym[3],
            sym[4],
            sym[5],
            sym[6],
            sym[7],
            sym[8],
            sym[9],
            sym[10],
            sym[11],
            sym[12],
            sym[13],
            sym[14],
            sym[15],
            sym[16],
            sym[17],
            sym[18],
            sym[19],
            sym[20]
            );

        public D3.SpdMatrix Get3x3Sym(int firstIndex)
        {
            switch (firstIndex)
            {
                case 0:
                    return new D3.SpdMatrix(s00, s01, s02, s11, s12, s22);
                case 1:
                    return new D3.SpdMatrix(s11, s12, s13, s22, s23, s33);
                case 2:
                    return new D3.SpdMatrix(s22, s23, s24, s33, s34, s44);
                case 3:
                    return new D3.SpdMatrix(s33, s34, s35, s44, s45, s55);
                default:
                    return D3.SpdMatrix.Zero;
            }
        }

        public D2.SpdMatrix Get2x2Sym(int firstIndex)
        {
            switch (firstIndex)
            {
                case 0:
                    return new D2.SpdMatrix(s00, s01, s11);
                case 1:
                    return new D2.SpdMatrix(s11, s12, s22);
                case 2:
                    return new D2.SpdMatrix(s22, s23, s33);
                case 3:
                    return new D2.SpdMatrix(s33, s34, s44);
                case 4:
                    return new D2.SpdMatrix(s44, s45, s55);
                default:
                    return D2.SpdMatrix.Zero;
            }
        }

        public string ToArrayString(char separator = ' ')
        {
            var strings = new[] {
                s00.ToString("G17", CultureInfo.InvariantCulture),
                s01.ToString("G17", CultureInfo.InvariantCulture),
                s02.ToString("G17", CultureInfo.InvariantCulture),
                s03.ToString("G17", CultureInfo.InvariantCulture),
                s04.ToString("G17", CultureInfo.InvariantCulture),
                s05.ToString("G17", CultureInfo.InvariantCulture),
                s11.ToString("G17", CultureInfo.InvariantCulture),
                s12.ToString("G17", CultureInfo.InvariantCulture),
                s13.ToString("G17", CultureInfo.InvariantCulture),
                s14.ToString("G17", CultureInfo.InvariantCulture),
                s15.ToString("G17", CultureInfo.InvariantCulture),
                s22.ToString("G17", CultureInfo.InvariantCulture),
                s23.ToString("G17", CultureInfo.InvariantCulture),
                s24.ToString("G17", CultureInfo.InvariantCulture),
                s25.ToString("G17", CultureInfo.InvariantCulture),
                s33.ToString("G17", CultureInfo.InvariantCulture),
                s34.ToString("G17", CultureInfo.InvariantCulture),
                s35.ToString("G17", CultureInfo.InvariantCulture),
                s44.ToString("G17", CultureInfo.InvariantCulture),
                s45.ToString("G17", CultureInfo.InvariantCulture),
                s55.ToString("G17", CultureInfo.InvariantCulture)
            };
            return string.Join(separator.ToString(), strings);
        }

        public static bool TryParseArray(string s, out SpdMatrix mat, char separator = ' ')
        {
            var split = s.Split(new char[] { separator });
            if (split.Length != Length)
            {
                mat = default;
                return false;
            }
            var data = new double[Length];
            for (var i = 0; i < Length; i++)
            {
                if (!double.TryParse(split[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var x))
                {
                    mat = default;
                    return false;
                }
                data[i] = x;
            }

            mat = new SpdMatrix
            (
                data[0], data[1], data[2], data[3], data[4], data[5],
                data[6], data[7], data[8], data[9], data[10],
                data[11], data[12], data[13], data[14],
                data[15], data[16], data[17], 
                data[18], data[19], 
                data[20]
            );

            return true;
        }

        public D3.SpdMatrix MatMulSymMulTrans6x3(in double[][] m) => new D3.SpdMatrix(
            m[0][5] * (m[0][5] * s55 + 2 * (m[0][4] * s45 + m[0][3] * s35 + m[0][2] * s25 + m[0][1] * s15 + m[0][0] * s05)) + m[0][4] * (m[0][4] * s44 + 2 * (m[0][3] * s34 + m[0][2] * s24 + m[0][1] * s14 + m[0][0] * s04)) + m[0][3] * (m[0][3] * s33 + 2 * (m[0][2] * s23 + m[0][1] * s13 + m[0][0] * s03)) + m[0][2] * (m[0][2] * s22 + 2 * (m[0][1] * s12 + m[0][0] * s02)) + m[0][1] * (m[0][1] * s11 + 2 * m[0][0] * s01) + m[0][0] * m[0][0] * s00, m[0][5] * (m[1][5] * s55 + m[1][4] * s45 + m[1][3] * s35 + m[1][2] * s25 + m[1][1] * s15 + m[1][0] * s05) + m[0][4] * (m[1][5] * s45 + m[1][4] * s44 + m[1][3] * s34 + m[1][2] * s24 + m[1][1] * s14 + m[1][0] * s04) + m[0][3] * (m[1][5] * s35 + m[1][4] * s34 + m[1][3] * s33 + m[1][2] * s23 + m[1][1] * s13 + m[1][0] * s03) + m[0][2] * (m[1][5] * s25 + m[1][4] * s24 + m[1][3] * s23 + m[1][2] * s22 + m[1][1] * s12 + m[1][0] * s02) + m[0][1] * (m[1][5] * s15 + m[1][4] * s14 + m[1][3] * s13 + m[1][2] * s12 + m[1][1] * s11 + m[1][0] * s01) + m[0][0] * (m[1][5] * s05 + m[1][4] * s04 + m[1][3] * s03 + m[1][2] * s02 + m[1][1] * s01 + m[1][0] * s00), m[0][5] * (m[2][5] * s55 + m[2][4] * s45 + m[2][3] * s35 + m[2][2] * s25 + m[2][1] * s15 + m[2][0] * s05) + m[0][4] * (m[2][5] * s45 + m[2][4] * s44 + m[2][3] * s34 + m[2][2] * s24 + m[2][1] * s14 + m[2][0] * s04) + m[0][3] * (m[2][5] * s35 + m[2][4] * s34 + m[2][3] * s33 + m[2][2] * s23 + m[2][1] * s13 + m[2][0] * s03) + m[0][2] * (m[2][5] * s25 + m[2][4] * s24 + m[2][3] * s23 + m[2][2] * s22 + m[2][1] * s12 + m[2][0] * s02) + m[0][1] * (m[2][5] * s15 + m[2][4] * s14 + m[2][3] * s13 + m[2][2] * s12 + m[2][1] * s11 + m[2][0] * s01) + m[0][0] * (m[2][5] * s05 + m[2][4] * s04 + m[2][3] * s03 + m[2][2] * s02 + m[2][1] * s01 + m[2][0] * s00),
            m[1][5] * (m[1][5] * s55 + 2 * (m[1][4] * s45 + m[1][3] * s35 + m[1][2] * s25 + m[1][1] * s15 + m[1][0] * s05)) + m[1][4] * (m[1][4] * s44 + 2 * (m[1][3] * s34 + m[1][2] * s24 + m[1][1] * s14 + m[1][0] * s04)) + m[1][3] * (m[1][3] * s33 + 2 * (m[1][2] * s23 + m[1][1] * s13 + m[1][0] * s03)) + m[1][2] * (m[1][2] * s22 + 2 * (m[1][1] * s12 + m[1][0] * s02)) + m[1][1] * (m[1][1] * s11 + 2 * m[1][0] * s01) + m[1][0] * m[1][0] * s00, m[1][5] * (m[2][5] * s55 + m[2][4] * s45 + m[2][3] * s35 + m[2][2] * s25 + m[2][1] * s15 + m[2][0] * s05) + m[1][4] * (m[2][5] * s45 + m[2][4] * s44 + m[2][3] * s34 + m[2][2] * s24 + m[2][1] * s14 + m[2][0] * s04) + m[1][3] * (m[2][5] * s35 + m[2][4] * s34 + m[2][3] * s33 + m[2][2] * s23 + m[2][1] * s13 + m[2][0] * s03) + m[1][2] * (m[2][5] * s25 + m[2][4] * s24 + m[2][3] * s23 + m[2][2] * s22 + m[2][1] * s12 + m[2][0] * s02) + m[1][1] * (m[2][5] * s15 + m[2][4] * s14 + m[2][3] * s13 + m[2][2] * s12 + m[2][1] * s11 + m[2][0] * s01) + m[1][0] * (m[2][5] * s05 + m[2][4] * s04 + m[2][3] * s03 + m[2][2] * s02 + m[2][1] * s01 + m[2][0] * s00),
            m[2][5] * (m[2][5] * s55 + 2 * (m[2][4] * s45 + m[2][3] * s35 + m[2][2] * s25 + m[2][1] * s15 + m[2][0] * s05)) + m[2][4] * (m[2][4] * s44 + 2 * (m[2][3] * s34 + m[2][2] * s24 + m[2][1] * s14 + m[2][0] * s04)) + m[2][3] * (m[2][3] * s33 + 2 * (m[2][2] * s23 + m[2][1] * s13 + m[2][0] * s03)) + m[2][2] * (m[2][2] * s22 + 2 * (m[2][1] * s12 + m[2][0] * s02)) + m[2][1] * (m[2][1] * s11 + 2 * m[2][0] * s01) + m[2][0] * m[2][0] * s00
            );

    }
}
