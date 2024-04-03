//using System;
//using System.Collections.Generic;

//namespace GeometryLib.Double.Linear
//{
//    public static class Algebra
//    {

//        private static int DiagPos(int colRow, int dimension) => (colRow * (dimension + dimension - colRow + 1)) >> 1;

//        private static int DimFromLen(int len) => ((int)Math.Sqrt((len << 3) + 1) - 1) >> 1;

//        private static int LenFromDim(int dim) => (dim * (dim + 1)) >> 1;

//        private static IEnumerable<double> RowCol(int index, int dimension, double[] sym)
//        {
//            int i = 0;
//            int pos = index;
//            for (; i < index; i++, pos += dimension - i)
//            {
//                yield return sym[pos];
//            }
//            for (; i < dimension; i++, pos++)
//            {
//                yield return sym[pos];
//            }
//        }

//        public static double[] MatMulSymMulMatTrans(in double[][] mat, in double[] sym)
//        {
//            int rows = mat.Length;
//            if (rows == 0)
//            {
//                return Array.Empty<double>();
//            }
//            int cols = mat[0].Length;
//            if (cols != DimFromLen(sym.Length))
//            {
//                return Array.Empty<double>();
//            }
//            var ans = new double[LenFromDim(rows)];

//            for (int i = 0; i < rows; i++)
//            {
//                var mati = mat[i];
//                int posi = DiagPos(i, rows);
//                for (int j = 0; j < cols; j++)
//                {
//                    double ij = 0.0;
//                    int k = 0;
//                    foreach (double kj in RowCol(j, cols, sym))
//                    {
//                        ij += mati[k++] * kj;
//                    }
//                    for (int l = i; l < rows; l++)
//                    {
//                        ans[posi + l - i] += ij * mat[l][j];
//                    }
//                }
//            }
//            return ans;
//        }

//    }
//}
