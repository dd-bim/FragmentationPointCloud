using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeometryLib
{
    public static class Linear
    {

        private static int DiagPos(int colRow, int dimension) => (colRow * (dimension + dimension - colRow + 1)) >> 1;

        private static int DimFromLen(int len) => ((int)Math.Sqrt((len << 3) + 1) - 1) >> 1;

        private static int LenFromDim(int dim) => (dim * (dim + 1)) >> 1;

        #region Double

        private static IEnumerable<double> RowCol(int index, int dimension, double[] sym)
        {
            var i = 0;
            var pos = index;
            for (; i < index; i++, pos += dimension - i)
            {
                yield return sym[pos];
            }
            for (; i < dimension; i++, pos++)
            {
                yield return sym[pos];
            }
        }

        public static double[] MatMulSymMulMatTrans(in double[][] mat, in double[] sym)
        {
            var rows = mat.Length;
            if (rows == 0)
            {
                return Array.Empty<double>();
            }
            var cols = mat[0].Length;
            if (cols != DimFromLen(sym.Length))
            {
                return Array.Empty<double>();
            }
            var ans = new double[LenFromDim(rows)];

            for (var i = 0; i < rows; i++)
            {
                var mati = mat[i];
                var posi = DiagPos(i, rows);
                for (var j = 0; j < cols; j++)
                {
                    var ij = 0.0;
                    var k = 0;
                    foreach (var kj in RowCol(j, cols, sym))
                    {
                        ij += mati[k++] * kj;
                    }
                    for (var l = i; l < rows; l++)
                    {
                        ans[posi + l - i] += ij * mat[l][j];
                    }
                }
            }
            return ans;
        }

        #endregion

        #region Decimal

        private static IEnumerable<decimal> RowCol(int index, int dimension, decimal[] sym)
        {
            var i = 0;
            var pos = index;
            for (; i < index; i++, pos += dimension - i)
            {
                yield return sym[pos];
            }
            for (; i < dimension; i++, pos++)
            {
                yield return sym[pos];
            }
        }

        public static decimal[] MatMulSymMulMatTrans(in decimal[][] mat, in decimal[] sym)
        {
            var rows = mat.Length;
            if (rows == 0)
            {
                return Array.Empty<decimal>();
            }
            var cols = mat[0].Length;
            if (cols != DimFromLen(sym.Length))
            {
                return Array.Empty<decimal>();
            }
            var ans = new decimal[LenFromDim(rows)];

            for (var i = 0; i < rows; i++)
            {
                var mati = mat[i];
                var posi = DiagPos(i, rows);
                for (var j = 0; j < cols; j++)
                {
                    var ij = 0m;
                    var k = 0;
                    foreach (var kj in RowCol(j, cols, sym))
                    {
                        ij += mati[k++] * kj;
                    }
                    for (var l = i; l < rows; l++)
                    {
                        ans[posi + l - i] += ij * mat[l][j];
                    }
                }
            }
            return ans;
        }

        #endregion
    }
}
