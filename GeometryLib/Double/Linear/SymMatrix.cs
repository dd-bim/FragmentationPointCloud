//using GeometryLib.Double.D3;

//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Globalization;
//using System.Text;

//using static GeometryLib.Double.Constants;

//namespace GeometryLib.Double.Linear
//{
//    //public readonly struct SymMatrix : IReadOnlyList<double>
//    //{
//    //    private static readonly SymMatrix empty = new SymMatrix(0, ImmutableArray<double>.Empty);

//    //    public static ref readonly SymMatrix Empty => ref empty;


//    //    // data.Length = Dimension * (Dimension + 1) / 2
//    //    private readonly ImmutableArray<double> _data;

//    //    // Dimension = (sqrt(8 * data.Length + 1) - 1) / 2
//    //    public int Dimension { get; }

//    //    public int Count => _data.Length;

//    //    public double this[int index] => _data[index];

//    //    public double this[int row, int col] => _data[Index(row, col)];

//    //    internal SymMatrix(int dimension, in ImmutableArray<double> data)
//    //    {
//    //        Dimension = dimension;
//    //        this._data = data;
//    //    }
//    //    public SymMatrix(in D2.SymMatrix sym)
//    //    {
//    //        Dimension = 2;
//    //        this._data = ImmutableArray.CreateRange(new[] { sym.xx, sym.xy, sym.yy });
//    //    }

//    //    public SymMatrix(in D3.SymMatrix sym)
//    //    {
//    //        Dimension = 3;
//    //        this._data = ImmutableArray.CreateRange(new[] { sym.xx, sym.xy, sym.xz, sym.yy, sym.yz, sym.zz });
//    //    }

//    //    public SymMatrix(in D2.SymMatrix sym1, in D2.SymMatrix sym2)
//    //    {
//    //        Dimension = 4;
//    //        this._data = ImmutableArray.CreateRange(new[] {
//    //            sym1.xx, sym1.xy, 0, 0,
//    //                     sym1.yy, 0, 0,
//    //            sym2.xx, sym2.xy,
//    //                     sym2.yy
//    //        });
//    //    }

//    //    public SymMatrix(in D3.SymMatrix sym1, in D3.SymMatrix sym2)
//    //    {
//    //        Dimension = 6;
//    //        this._data = ImmutableArray.CreateRange(new[] {
//    //            sym1.xx, sym1.xy, sym1.xz, 0, 0, 0,
//    //                     sym1.yy, sym1.yz, 0, 0, 0,
//    //                              sym1.zz, 0, 0, 0,
//    //            sym2.xx, sym2.xy, sym2.xz,
//    //                     sym2.yy, sym2.yz,
//    //                              sym2.zz
//    //        });
//    //    }

//    //    public SymMatrix(int dimension)
//    //    {
//    //        Dimension = dimension;
//    //        this._data = ImmutableArray.CreateRange(new double[LenFromDim(dimension)]);
//    //    }

//    //    public SymMatrix(int dimension, in double fill)
//    //    {
//    //        Dimension = dimension;
//    //        var arr = new double[LenFromDim(dimension)];
//    //        for (int i = 0; i < arr.Length; i++)
//    //        {
//    //            arr[i] = fill;
//    //        }
//    //        this._data = ImmutableArray.CreateRange(arr);
//    //    }

//    //    public static SymMatrix Unit(int dimension)
//    //    {
//    //        var arr = new double[LenFromDim(dimension)];
//    //        for (int i = 0, p = 0; i < dimension; p += dimension - i, i++)
//    //        {
//    //            arr[p] = 1.0;
//    //        }
//    //        return new SymMatrix(dimension, ImmutableArray.CreateRange(arr));
//    //    }

//    //    public static SymMatrix Diag(in IReadOnlyList<double> vector)
//    //    {
//    //        int dim = vector.Count;
//    //        var arr = new double[LenFromDim(dim)];
//    //        for (int i = 0, p = 0; i < dim; p += dim - i, i++)
//    //        {
//    //            arr[p] = vector[i];
//    //        }
//    //        return new SymMatrix(dim, ImmutableArray.CreateRange(arr));
//    //    }

//    //    public ImmutableArray<double> Diag()
//    //    {
//    //        var arr = new double[Dimension];
//    //        for (int i = 0, p = 0; i < Dimension; p += Dimension - i, i++)
//    //        {
//    //            arr[i] = _data[p];
//    //        }
//    //        return ImmutableArray.CreateRange(arr);
//    //    }

//    //    public SymMatrix SetRange(int startIndex, in SymMatrix other)
//    //    {
//    //        if ((other.Dimension + startIndex) > Dimension)
//    //        {
//    //            throw new ArgumentException();
//    //        }
//    //        var ans = _data;
//    //        for (int i = 0, op = 0; i < other.Dimension; i++)
//    //        {
//    //            int p = DiagPos(startIndex + i);
//    //            for (int j = i; j < other.Dimension; j++, p++, op++)
//    //            {
//    //                ans = ans.SetItem(p, other._data[op]);
//    //            }
//    //        }
//    //        return new SymMatrix(Dimension, ans);
//    //    }

//    //    public SymMatrix SetRange(int startIndex, in D3.SymMatrix other)
//    //    {
//    //        if ((3 + startIndex) > Dimension)
//    //        {
//    //            throw new ArgumentException();
//    //        }

//    //        int p = DiagPos(startIndex);
//    //        var ans = _data.SetItem(p++, other.xx);
//    //        ans = ans.SetItem(p++, other.xy);
//    //        ans = ans.SetItem(p, other.xz);
//    //        p = DiagPos(startIndex + 1);
//    //        ans = ans.SetItem(p++, other.yy);
//    //        ans = ans.SetItem(p, other.yz);
//    //        ans = ans.SetItem(DiagPos(startIndex + 2), other.zz);
//    //        return new SymMatrix(Dimension, ans);
//    //    }

//    //    public SymMatrix SetRange(int startIndex, in IReadOnlyList<double> diag)
//    //    {
//    //        if ((diag.Count + startIndex) > Dimension)
//    //        {
//    //            throw new ArgumentException();
//    //        }
//    //        var ans = _data;
//    //        for (int i = 0, p = DiagPos(startIndex); i < diag.Count; p += Dimension - i - startIndex, i++)
//    //        {
//    //            ans = ans.SetItem(p, diag[i]);
//    //        }
//    //        return new SymMatrix(Dimension, ans);
//    //    }

//    //    public SymMatrix SetRange(int startIndex, in Vector diag)
//    //    {
//    //        if ((3 + startIndex) > Dimension)
//    //        {
//    //            throw new ArgumentException();
//    //        }

//    //        var ans = _data.SetItem(DiagPos(startIndex), diag.x);
//    //        ans = ans.SetItem(DiagPos(startIndex + 1), diag.y);
//    //        ans = ans.SetItem(DiagPos(startIndex + 2), diag.z);
//    //        return new SymMatrix(Dimension, ans);
//    //    }

//    //    public SymMatrix Set(int index, double diagValue)
//    //    {
//    //        if (index >= Dimension)
//    //        {
//    //            throw new ArgumentException();
//    //        }
//    //        return new SymMatrix(Dimension, _data.SetItem(DiagPos(index), diagValue));
//    //    }

//    //    // TODO: auch für Linear mit Länge
//    //    public D3.SymMatrix GetRange(int startIndex)
//    //    {
//    //        if ((3 + startIndex) > Dimension)
//    //        {
//    //            throw new ArgumentException();
//    //        }
//    //        int px = DiagPos(startIndex);
//    //        int py = DiagPos(startIndex + 1);
//    //        return new D3.SymMatrix(
//    //            _data[px++], _data[px++], _data[px], 
//    //            _data[py++], _data[py],
//    //            _data[DiagPos(startIndex + 2)]);
//    //    }

//    //    private static int LenFromDim(int dim) => (dim * (dim + 1)) >> 1;

//    //    private static int DimFromLen(int len) => ((int)Math.Sqrt((len << 3) + 1) - 1) >> 1;

//    //    private int DiagPos(int colRow) => (colRow * (Dimension + Dimension - colRow + 1)) >> 1;

//    //    private static int DiagPos(int colRow, int dimension) => (colRow * (dimension + dimension - colRow + 1)) >> 1;

//    //    private int Index(int row, int col)
//    //    {
//    //        if (row > col)
//    //        {
//    //            (row, col) = (col, row);
//    //        }
//    //        if (col >= Dimension)
//    //        {
//    //            throw new IndexOutOfRangeException();
//    //        }
//    //        return col + ((row * (Dimension + Dimension - 1 - row)) >> 1);
//    //    }


//    //    public static bool Create(in IReadOnlyList<double> upperRows, out SymMatrix mat)
//    //    {
//    //        int dim = DimFromLen(upperRows.Count);
//    //        if (LenFromDim(dim) != upperRows.Count)
//    //        {
//    //            mat = default;
//    //            return false;
//    //        }
//    //        mat = new SymMatrix(dim, upperRows.ToImmutableArray());
//    //        return true;
//    //    }

//    //    public static bool Create(in Matrix mat, out SymMatrix sym, double tol = EPS)
//    //    {
//    //        int dim = DimFromLen(mat.Rows);
//    //        if (dim == 0)
//    //        {
//    //            sym = Empty;
//    //            return true;
//    //        }
//    //        if (dim != mat.Cols)
//    //        {
//    //            sym = default;
//    //            return false;
//    //        }
//    //        var data = new double[LenFromDim(dim)];
//    //        int first = 0;
//    //        for (int i = 0; i < dim; i++)
//    //        {
//    //            mat.Data[i].CopyTo(i, data, first, dim - i);
//    //            for (int j = 0; j < dim; j++)
//    //            {
//    //                if (j == i)
//    //                {
//    //                    continue;
//    //                }
//    //                double diff = Math.Abs(mat[i, j] - mat[j, i]);
//    //                if (diff > tol)
//    //                {
//    //                    sym = default;
//    //                    return false;
//    //                }
//    //            }
//    //            first += dim - i;
//    //        }
//    //        sym = new SymMatrix(dim, data.ToImmutableArray());
//    //        return true;
//    //    }

//    //    public IEnumerable<double> RowCol(int index)
//    //    {
//    //        int i = 0;
//    //        int pos = index;
//    //        for (; i < index; i++, pos += Dimension - i)
//    //        {
//    //            yield return _data[pos];
//    //        }
//    //        for (; i < Dimension; i++, pos++)
//    //        {
//    //            yield return _data[pos];
//    //        }
//    //    }

//    //    //public double[] GetRowCol(int index)
//    //    //{
//    //    //    var ret = new double[Dimension];
//    //    //    //  | 0 1 | 0 1 2 | 0 1 2 3 | 0  1  2  3  4 | 0  1  2  3  4  5
//    //    //    // -+-----+-------+---------+---------------+-----------------
//    //    //    // 0| 0 1 | 0 1 2 | 0 1 2 3 | 0  1  2  3  4 | 0  1  2  3  4  5
//    //    //    // 1|   2 |   3 4 |   4 5 6 |    5  6  7  8 |    6  7  8  9 10
//    //    //    // 2|     |     5 |     7 8 |       9 10 11 |      11 12 13 14
//    //    //    // 3|     |       |       9 |         12 13 |         15 16 17 
//    //    //    // 4|     |       |         |            14 |            18 19
//    //    //    // 5|     |       |         |               |               20
//    //    //    //   3     5       7         9               11
//    //    //    int pos = diagPos(index);
//    //    //    int len = Dimension - index;
//    //    //    data.CopyTo(pos, ret, index, len);
//    //    //    for (int i = 0; i < index; i++)
//    //    //    {
//    //    //        ret[i] = data[index + ((i * (Dimension + Dimension - 1 - i)) >> 1)];
//    //    //    }
//    //    //    return ret;
//    //    //}

//    //    public SymMatrix Mul(in double factor)
//    //    {
//    //        var mulData = new double[this._data.Length];
//    //        for (int i = 0; i < mulData.Length; i++)
//    //        {
//    //            mulData[i] = this._data[i] * factor;
//    //        }
//    //        return new SymMatrix(Dimension, mulData.ToImmutableArray());
//    //    }

//    //    public SymMatrix Add(in double other)
//    //    {
//    //        var addData = new double[this._data.Length];
//    //        for (int i = 0; i < addData.Length; i++)
//    //        {
//    //            addData[i] = this._data[i] + other;
//    //        }
//    //        return new SymMatrix(Dimension, addData.ToImmutableArray());
//    //    }

//    //    public SymMatrix SubFrom(in double other)
//    //    {
//    //        var subData = new double[this._data.Length];
//    //        for (int i = 0; i < subData.Length; i++)
//    //        {
//    //            subData[i] = other - this._data[i];
//    //        }
//    //        return new SymMatrix(Dimension, subData.ToImmutableArray());
//    //    }

//    //    public SymMatrix Add(in SymMatrix other)
//    //    {
//    //        if (Dimension != other.Dimension)
//    //        {
//    //            throw new ArgumentException();
//    //        }

//    //        var addData = new double[this._data.Length];
//    //        for (int i = 0; i < addData.Length; i++)
//    //        {
//    //            addData[i] = this._data[i] + other._data[i];
//    //        }
//    //        return new SymMatrix(Dimension, addData.ToImmutableArray());
//    //    }

//    //    public SymMatrix Sub(in SymMatrix other)
//    //    {
//    //        if (Dimension != other.Dimension)
//    //        {
//    //            throw new ArgumentException();
//    //        }

//    //        var subData = new double[this._data.Length];
//    //        for (int i = 0; i < subData.Length; i++)
//    //        {
//    //            subData[i] = this._data[i] - other._data[i];
//    //        }
//    //        return new SymMatrix(Dimension, subData.ToImmutableArray());
//    //    }

//    //    public static explicit operator Matrix(in SymMatrix sym)
//    //    {
//    //        var m = new ImmutableArray<double>[sym.Dimension];
//    //        for (int i = 0; i < sym.Dimension; i++)
//    //        {
//    //            m[i] = sym.RowCol(i).ToImmutableArray();
//    //        }
//    //        return new Matrix(m.ToImmutableArray());
//    //    }

//    //    public SymMatrix MatMulSymMulMatTrans(Matrix mat)
//    //    {
//    //        int rows = mat.Rows;
//    //        if (rows == 0)
//    //        {
//    //            return new SymMatrix(0, ImmutableArray<double>.Empty);
//    //        }
//    //        int cols = mat.Cols;
//    //        if (cols != Dimension)
//    //        {
//    //            throw new ArgumentException();
//    //        }
//    //        var ans = new double[LenFromDim(rows)];

//    //        for (int i = 0; i < rows; i++)
//    //        {
//    //            var mati = mat[i];
//    //            int posi = DiagPos(i, rows);
//    //            for (int j = 0; j < cols; j++)
//    //            {
//    //                double ij = 0.0;
//    //                int k = 0;
//    //                foreach (double kj in RowCol(j))
//    //                {
//    //                    ij += mati[k++] * kj;
//    //                }
//    //                for (int l = i; l < rows; l++)
//    //                {
//    //                    ans[posi + l - i] += ij * mat[l][j];
//    //                }
//    //            }
//    //        }
//    //        return new SymMatrix(rows, ans.ToImmutableArray());
//    //    }

//    //    public double RowMulSymMulCol(double[] vec)
//    //    {
//    //        int cols = vec.Length;
//    //        if (cols != Dimension)
//    //        {
//    //            throw new ArgumentException();
//    //        }
//    //        double ans = 0.0;

//    //        for (int j = 0; j < cols; j++)
//    //        {
//    //            double ij = 0.0;
//    //            int k = 0;
//    //            foreach (double kj in RowCol(j))
//    //            {
//    //                ij += vec[k++] * kj;
//    //            }
//    //            ans += ij * vec[j];
//    //        }
//    //        return ans;
//    //    }

//    //    public Matrix RightMul(Matrix mat)
//    //    {
//    //        int rows = mat.Rows;
//    //        if (rows == 0)
//    //        {
//    //            return Matrix.Empty;
//    //        }
//    //        int cols = mat.Cols;
//    //        if (cols != Dimension)
//    //        {
//    //            throw new ArgumentException();
//    //        }
//    //        var ans = new ImmutableArray<double>[rows];

//    //        for (int i = 0; i < rows; i++)
//    //        {
//    //            var mati = mat[i];
//    //            var ansi = new double[cols];
//    //            for (int j = 0; j < cols; j++)
//    //            {
//    //                double ij = 0.0;
//    //                int k = 0;
//    //                foreach (double kj in RowCol(j))
//    //                {
//    //                    ij += mati[k++] * kj;
//    //                }
//    //                ansi[j] = ij;
//    //            }
//    //            ans[i] = ansi.ToImmutableArray();
//    //        }
//    //        return new Matrix(ans.ToImmutableArray());
//    //    }

//    //    public Matrix Mul(in Matrix b)
//    //    {
//    //        int rows = Dimension;
//    //        if (rows == 0)
//    //        {
//    //            return Matrix.Empty;
//    //        }
//    //        int aCols = Dimension;
//    //        if (aCols != b.Rows)
//    //        {
//    //            throw new ArgumentOutOfRangeException();
//    //        }

//    //        int cols = b.Cols;
//    //        if (cols == 0)
//    //        {
//    //            return Matrix.Empty;
//    //        }

//    //        var res = new ImmutableArray<double>[rows];

//    //        for (int i = 0; i < rows; i++)
//    //        {
//    //            var resi = new double[cols];
//    //            int k = 0;
//    //            foreach(var aik in RowCol(i))
//    //            {
//    //                var bk = b.Data[k];
//    //                for (int j = 0; j < cols; j++)
//    //                {
//    //                    resi[j] += aik * bk[j];
//    //                }
//    //                k++;
//    //            }
//    //            res[i] = resi.ToImmutableArray();
//    //        }
//    //        return new Matrix(res.ToImmutableArray());
//    //    }
//    //    public Matrix Mul(in SymMatrix b)
//    //    {
//    //        int dim = Dimension;
//    //        if (dim == 0)
//    //        {
//    //            return Matrix.Empty;
//    //        }
//    //        if (dim != b.Dimension)
//    //        {
//    //            return Matrix.Empty;
//    //        }

//    //        var res = new ImmutableArray<double>[dim];

//    //        for (int i = 0; i < dim; i++)
//    //        {
//    //            var resi = new double[dim];
//    //            int k = 0;
//    //            foreach (var aik in RowCol(i))
//    //            {
//    //                int j = 0;
//    //                foreach (var bkj in b.RowCol(k))
//    //                {
//    //                    resi[j] += aik * bkj;
//    //                    j++;
//    //                }
//    //                k++;
//    //            }
//    //            res[i] = resi.ToImmutableArray();
//    //        }
//    //        return new Matrix(res.ToImmutableArray());
//    //    }


//    //    public static Matrix operator *(in Matrix left, in SymMatrix right) => right.RightMul(left);

//    //    public static SymMatrix operator *(in SymMatrix left, in double right) => left.Mul(right);

//    //    public static Matrix operator *(in SymMatrix left, in Matrix right) => left.Mul(right);

//    //    public static Matrix operator *(in SymMatrix left, in SymMatrix right) => left.Mul(right);

//    //    public static SymMatrix operator *(in double left, in SymMatrix right) => right.Mul(left);

//    //    public static SymMatrix operator +(in SymMatrix left, in double right) => left.Add(right);

//    //    public static SymMatrix operator +(in double left, in SymMatrix right) => right.Add(left);

//    //    public static SymMatrix operator -(in SymMatrix left, double right) => left.Add(-right);

//    //    public static SymMatrix operator -(in double left, in SymMatrix right) => right.SubFrom(left);

//    //    public static SymMatrix operator +(SymMatrix a, SymMatrix b) => a.Add(b);

//    //    public static SymMatrix operator -(SymMatrix a, SymMatrix b) => a.Sub(b);


//    //    public IEnumerator<double> GetEnumerator()
//    //    {
//    //        return ((IEnumerable<double>)_data).GetEnumerator();
//    //    }

//    //    IEnumerator IEnumerable.GetEnumerator()
//    //    {
//    //        return ((IEnumerable)_data).GetEnumerator();
//    //    }

//    //    public override string ToString()
//    //    {
//    //        if (Dimension == 0)
//    //        {
//    //            return "[]";
//    //        }
//    //        var lines = new string[Dimension][];
//    //        var maxlen = new int[Dimension];
//    //        int first = 0;
//    //        for (int i = 0; i < Dimension; i++)
//    //        {
//    //            var line = new string[Dimension];
//    //            for (int k = 0; k < Dimension; k++)
//    //            {
//    //                if (k < i)
//    //                {
//    //                    line[k] = "";
//    //                }
//    //                else
//    //                {
//    //                    var s = _data[first + k - i].ToString("G17", CultureInfo.InvariantCulture);
//    //                    if (s.Length > maxlen[k])
//    //                    {
//    //                        maxlen[k] = s.Length;
//    //                    }
//    //                    line[k] = s;
//    //                }
//    //            }
//    //            lines[i] = line;
//    //            first += Dimension - i;
//    //        }
//    //        for (int j = 1; j < Dimension; j++)
//    //        {
//    //            maxlen[j]++;
//    //        }
//    //        var sb = new StringBuilder();
//    //        for (int i = 0; i < Dimension; i++)
//    //        {
//    //            var row = lines[i];
//    //            sb.Append('[');
//    //            for (int j = 0; j < Dimension; j++)
//    //            {
//    //                sb.Append(row[j].PadLeft(maxlen[j]));
//    //            }
//    //            sb.AppendLine("]");
//    //        }
//    //        return sb.ToString();
//    //    }

//    //    public string ToArrayString(string separator = " ")
//    //    {
//    //        var strings = new string[_data.Length];
//    //        for (int i = 0; i < _data.Length; i++)
//    //        {
//    //            strings[i] = _data[i].ToString("G17", CultureInfo.InvariantCulture);
//    //        }
//    //        return string.Join(separator, strings);
//    //    }

//    //    public static bool TryParseArray(string s, out SymMatrix mat, string separator = " ")
//    //    {
//    //        int start = 0;
//    //        int end = s.IndexOf(separator, start);
//    //        var data = ImmutableArray.CreateBuilder<double>();
//    //        while (end > start)
//    //        {
//    //            if (!double.TryParse(s[start..end], NumberStyles.Float, CultureInfo.InvariantCulture, out double x))
//    //            {
//    //                mat = default;
//    //                return false;
//    //            }
//    //            data.Add(x);
//    //            start = Math.Min(s.Length, end + 1);
//    //            end = s.IndexOf(separator, start);
//    //            if (end < 0)
//    //            {
//    //                end = s.Length;
//    //            }
//    //        }
//    //        return Create(data, out mat);
//    //    }
//    //}
//}
