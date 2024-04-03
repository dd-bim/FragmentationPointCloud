//using System;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Globalization;
//using System.Text;

//namespace GeometryLib.Double.Linear
//{
//    //public readonly struct Matrix
//    //{
//    //    private static readonly Matrix empty = new Matrix(ImmutableArray<ImmutableArray<double>>.Empty);

//    //    public static ref readonly Matrix Empty => ref empty;

//    //    public readonly ImmutableArray<ImmutableArray<double>> Data;

//    //    public int Rows => Data.Length;

//    //    public int Cols => Rows > 0 ? Data[0].Length : 0;

//    //    public double this[int row, int col] => Data[row][col];

//    //    public ImmutableArray<double> this[int row] => Data[row];

//    //    internal Matrix(in ImmutableArray<ImmutableArray<double>> data)
//    //    {
//    //        this.Data = data;
//    //    }

//    //    public Matrix(in double value) : this(ImmutableArray.Create(ImmutableArray.Create(value))) { }

//    //    public Matrix(in D2.Vector vector, bool asRow = false)
//    //    {
//    //        if (asRow)
//    //        {
//    //            this.Data = ImmutableArray.Create(ImmutableArray.CreateRange(
//    //                new[] { vector.x, vector.y }));
//    //        }
//    //        else
//    //        {
//    //            this.Data = ImmutableArray.CreateRange(
//    //            new[] {
//    //                ImmutableArray.Create(vector.x),
//    //                ImmutableArray.Create(vector.y)
//    //            });
//    //        }
//    //    }

//    //    public Matrix(in D2.Vector vector1, in D2.Vector vector2, bool asRow = false)
//    //    {
//    //        if (asRow)
//    //        {
//    //            this.Data = ImmutableArray.Create(ImmutableArray.CreateRange(
//    //                new[] { vector1.x, vector1.y, vector2.x, vector2.y }));
//    //        }
//    //        else
//    //        {
//    //            this.Data = ImmutableArray.CreateRange(
//    //            new[] {
//    //                ImmutableArray.Create(vector1.x),
//    //                ImmutableArray.Create(vector1.y),
//    //                ImmutableArray.Create(vector2.x),
//    //                ImmutableArray.Create(vector2.y)
//    //            });
//    //        }
//    //    }

//    //    public Matrix(in D3.Vector vector, bool asRow = false)
//    //    {
//    //        if (asRow)
//    //        {
//    //            this.Data = ImmutableArray.Create(ImmutableArray.CreateRange(
//    //                new[] { vector.x, vector.y, vector.z }));
//    //        }
//    //        else
//    //        {
//    //            this.Data = ImmutableArray.CreateRange(
//    //            new[] {
//    //                ImmutableArray.Create(vector.x),
//    //                ImmutableArray.Create(vector.y),
//    //                ImmutableArray.Create(vector.z)
//    //            });
//    //        }
//    //    }

//    //    public Matrix(in IReadOnlyList<double> vector, bool asRow = false)
//    //    {
//    //        if (asRow)
//    //        {
//    //            this.Data = ImmutableArray.Create(ImmutableArray.CreateRange(vector));
//    //        }
//    //        else
//    //        {
//    //            var data = new ImmutableArray<double>[vector.Count];
//    //            for (int i = 0; i < data.Length; i++)
//    //            {
//    //                data[i] = ImmutableArray.Create(vector[i]);
//    //            }
//    //            this.Data = data.ToImmutableArray();
//    //        }
//    //    }

//    //    public static Matrix FromRows(params Matrix[] rows)
//    //    {
//    //        var c = -1;
//    //        var r = 0;
//    //        foreach (var row in rows)
//    //        {
//    //            if (c >= 0 && row.Cols != c)
//    //            {
//    //                return Empty;
//    //            }
//    //            c = row.Cols;
//    //            r += row.Rows;
//    //        }
//    //        var data = ImmutableArray.CreateBuilder<ImmutableArray<double>>(r);
//    //        foreach (var row in rows)
//    //        {
//    //            foreach (var rowrow in row.Data)
//    //            {
//    //                data.Add(rowrow);
//    //            }
//    //        }
//    //        return new Matrix(data.ToImmutable());
//    //    }

//    //    public Matrix(in D3.Vector vector1, in D3.Vector vector2, bool asRow = false)
//    //    {
//    //        if (asRow)
//    //        {
//    //            this.Data = ImmutableArray.Create(ImmutableArray.CreateRange(
//    //                new[] { vector1.x, vector1.y, vector1.z, vector2.x, vector2.y, vector2.z }));
//    //        }
//    //        else
//    //        {
//    //            this.Data = ImmutableArray.CreateRange(
//    //            new[] {
//    //                ImmutableArray.Create(vector1.x),
//    //                ImmutableArray.Create(vector1.y),
//    //                ImmutableArray.Create(vector1.z),
//    //                ImmutableArray.Create(vector2.x),
//    //                ImmutableArray.Create(vector2.y),
//    //                ImmutableArray.Create(vector2.z)
//    //            });
//    //        }
//    //    }

//    //    public Matrix(int rows, int cols, in double fill)
//    //    {
//    //        var data = new ImmutableArray<double>[rows];
//    //        for (int i = 0; i < rows; i++)
//    //        {
//    //            var datai = new double[cols];
//    //            for (int j = 0; j < cols; j++)
//    //            {
//    //                datai[j] = fill;
//    //            }
//    //            data[i] = datai.ToImmutableArray();
//    //        }
//    //        this.Data = data.ToImmutableArray();
//    //    }

//    //    public Matrix(in double[,] mat)
//    //    {
//    //        int rows = mat.GetLength(0);
//    //        int cols = mat.GetLength(1);
//    //        var data = new ImmutableArray<double>[rows];
//    //        for (int i = 0; i < rows; i++)
//    //        {
//    //            var datai = new double[cols];
//    //            for (int j = 0; j < cols; j++)
//    //            {
//    //                datai[j] = mat[i, j];
//    //            }
//    //            data[i] = datai.ToImmutableArray();
//    //        }
//    //        this.Data = data.ToImmutableArray();
//    //    }

//    //    public double Trace()
//    //    {
//    //        int cnt = Math.Min(Rows, Cols);
//    //        double sum = 0.0;
//    //        for (int i = 0; i < cnt; i++)
//    //        {
//    //                sum += Data[i][i];
//    //        }
//    //        return sum;
//    //    }

//    //    public static Matrix Unit(int dimension)
//    //    {
//    //        var data = new ImmutableArray<double>[dimension];
//    //        for (int i = 0; i < dimension; i++)
//    //        {
//    //            var row = new double[dimension];
//    //            row[i] = 1.0;
//    //            data[i] = row.ToImmutableArray();
//    //        }
//    //        return new Matrix(data.ToImmutableArray());
//    //    }

//    //    public static Matrix Diag(in IReadOnlyList<double> vector)
//    //    {
//    //        int dim = vector.Count;
//    //        var data = new ImmutableArray<double>[dim];
//    //        for (int i = 0; i < dim; i++)
//    //        {
//    //            var row = new double[dim];
//    //            row[i] = vector[i];
//    //            data[i] = row.ToImmutableArray();
//    //        }
//    //        return new Matrix(data.ToImmutableArray());
//    //    }

//    //    public ImmutableArray<double> Diag()
//    //    {
//    //        var arr = new double[Math.Min(Rows, Cols)];
//    //        for (int i = 0; i < arr.Length; i++)
//    //        {
//    //            arr[i] = Data[i][i];
//    //        }
//    //        return ImmutableArray.CreateRange(arr);
//    //    }


//    //    public static bool Create(in ImmutableArray<ImmutableArray<double>> data, out Matrix mat)
//    //    {
//    //        if (data.Length > 0)
//    //        {
//    //            int cols = data[0].Length;
//    //            for (int i = 1; i < data.Length; i++)
//    //            {
//    //                if (cols != data[i].Length)
//    //                {
//    //                    mat = default;
//    //                    return false;
//    //                }
//    //            }
//    //        }
//    //        mat = new Matrix(data);
//    //        return true;
//    //    }

//    //    public static bool Create(in IReadOnlyList<ImmutableArray<double>> data, out Matrix mat) => Create(data.ToImmutableArray(), out mat);

//    //    public static bool Create(in IReadOnlyList<IReadOnlyList<double>> data, out Matrix mat)
//    //    {
//    //        if (data.Count == 0)
//    //        {
//    //            mat = new Matrix(ImmutableArray<ImmutableArray<double>>.Empty);
//    //            return true;
//    //        }
//    //        else
//    //        {
//    //            int cols = data[0].Count;
//    //            var idata = new ImmutableArray<double>[data.Count];
//    //            for (int i = 0; i < idata.Length; i++)
//    //            {
//    //                idata[i] = data[i].ToImmutableArray();
//    //                if (cols != data[i].Count)
//    //                {
//    //                    mat = default;
//    //                    return false;
//    //                }
//    //            }
//    //            mat = new Matrix(idata.ToImmutableArray());
//    //            return true;
//    //        }
//    //    }

//    //    public ImmutableArray<double> Row(int i) => Data[i];

//    //    public ImmutableArray<double> Col(int i)
//    //    {
//    //        var col = new double[Rows];
//    //        for (int j = 0; j < Rows; j++)
//    //        {
//    //            col[j] = Data[j][i];
//    //        }
//    //        return col.ToImmutableArray();
//    //    }

//    //    public Matrix Trans()
//    //    {
//    //        if (Rows == 0)
//    //        {
//    //            return Empty;
//    //        }
//    //        int cols = Cols;
//    //        if (cols == 0)
//    //        {
//    //            return Empty;
//    //        }

//    //        var res = new ImmutableArray<double>[cols];

//    //        for (int i = 0; i < cols; i++)
//    //        {
//    //            var resi = new double[Rows];
//    //            for (int j = 0; j < Rows; j++)
//    //            {
//    //                resi[j] = Data[j][i];
//    //            }
//    //            res[i] = resi.ToImmutableArray();
//    //        }
//    //        return new Matrix(res.ToImmutableArray());
//    //    }

//    //    public ImmutableArray<double> Sum(bool rowSum = false)
//    //    {
//    //        double[] ret;
//    //        if (rowSum)
//    //        {
//    //            ret = new double[Cols];
//    //            for (int i = 0; i < Rows; i++)
//    //            {
//    //                var row = Data[i];
//    //                for (int j = 0; j < Cols; j++)
//    //                {
//    //                    ret[j] += row[j];
//    //                }
//    //            }
//    //        }
//    //        else
//    //        {
//    //            ret = new double[Rows];
//    //            for (int i = 0; i < Rows; i++)
//    //            {
//    //                var sum = 0.0;
//    //                var row = Data[i];
//    //                for (int j = 0; j < Cols; j++)
//    //                {
//    //                    sum += row[j];
//    //                }
//    //                ret[i] = sum;
//    //            }
//    //        }
//    //        return ret.ToImmutableArray();
//    //    }

//    //    public ImmutableArray<double> AbsSum(bool rowSum = false)
//    //    {
//    //        double[] ret;
//    //        if (rowSum)
//    //        {
//    //            ret = new double[Cols];
//    //            for (int i = 0; i < Rows; i++)
//    //            {
//    //                var row = Data[i];
//    //                for (int j = 0; j < Cols; j++)
//    //                {
//    //                    ret[j] += Math.Abs(row[j]);
//    //                }
//    //            }
//    //        }
//    //        else
//    //        {
//    //            ret = new double[Rows];
//    //            for (int i = 0; i < Rows; i++)
//    //            {
//    //                var sum = 0.0;
//    //                var row = Data[i];
//    //                for (int j = 0; j < Cols; j++)
//    //                {
//    //                    sum += Math.Abs(row[j]);
//    //                }
//    //                ret[i] = sum;
//    //            }
//    //        }
//    //        return ret.ToImmutableArray();
//    //    }

//    //    public ImmutableArray<double> SquareSum(bool rowSum = false)
//    //    {
//    //        double[] ret;
//    //        if (rowSum)
//    //        {
//    //            ret = new double[Cols];
//    //            for (int i = 0; i < Rows; i++)
//    //            {
//    //                var row = Data[i];
//    //                for (int j = 0; j < Cols; j++)
//    //                {
//    //                    ret[j] += row[j]*row[j];
//    //                }
//    //            }
//    //        }
//    //        else
//    //        {
//    //            ret = new double[Rows];
//    //            for (int i = 0; i < Rows; i++)
//    //            {
//    //                var sum = 0.0;
//    //                var row = Data[i];
//    //                for (int j = 0; j < Cols; j++)
//    //                {
//    //                    sum += row[j] * row[j];
//    //                }
//    //                ret[i] = sum;
//    //            }
//    //        }
//    //        return ret.ToImmutableArray();
//    //    }


//    //    public Matrix Mul(in double b)
//    //    {
//    //        if (Rows == 0)
//    //        {
//    //            return Empty;
//    //        }
//    //        int cols = Cols;
//    //        if (cols == 0)
//    //        {
//    //            return Empty;
//    //        }

//    //        var res = new ImmutableArray<double>[Rows];

//    //        for (int i = 0; i < Rows; i++)
//    //        {
//    //            var resi = new double[cols];
//    //            var ai = Data[i];
//    //            for (int j = 0; j < cols; j++)
//    //            {
//    //                resi[j] = ai[j] * b;
//    //            }
//    //            res[i] = resi.ToImmutableArray();
//    //        }
//    //        return new Matrix(res.ToImmutableArray());
//    //    }

//    //    public Matrix Mul(in Matrix b)
//    //    {
//    //        int rows = Rows;
//    //        if (rows == 0)
//    //        {
//    //            return Empty;
//    //        }
//    //        int aCols = Cols;
//    //        if (aCols != b.Rows)
//    //        {
//    //            throw new ArgumentOutOfRangeException();
//    //        }

//    //        int cols = b.Cols;
//    //        if (cols == 0)
//    //        {
//    //            return Empty;
//    //        }

//    //        var res = new ImmutableArray<double>[rows];

//    //        for (int i = 0; i < rows; i++)
//    //        {
//    //            var resi = new double[cols];
//    //            var ai = Data[i];
//    //            for (int k = 0; k < aCols; k++)
//    //            {
//    //                var bk = b.Data[k];
//    //                double aik = ai[k];
//    //                for (int j = 0; j < cols; j++)
//    //                {
//    //                    resi[j] += aik * bk[j];
//    //                }
//    //            }
//    //            res[i] = resi.ToImmutableArray();
//    //        }
//    //        return new Matrix(res.ToImmutableArray());
//    //    }

//    //    public Matrix TransMul(in Matrix b)
//    //    {
//    //        int rows = Cols;
//    //        if (rows == 0)
//    //        {
//    //            return Empty;
//    //        }
//    //        int aCols = Rows;
//    //        if (aCols != b.Rows)
//    //        {
//    //            throw new ArgumentOutOfRangeException();
//    //        }

//    //        int cols = b.Cols;
//    //        if (cols == 0)
//    //        {
//    //            return Empty;
//    //        }

//    //        var res = new ImmutableArray<double>[rows];

//    //        for (int i = 0; i < rows; i++)
//    //        {
//    //            var resi = new double[cols];
//    //            for (int k = 0; k < aCols; k++)
//    //            {
//    //                var bk = b.Data[k];
//    //                var ak = Data[k];
//    //                for (int j = 0; j < cols; j++)
//    //                {
//    //                    resi[j] += ak[i] * bk[j];
//    //                }
//    //            }
//    //            res[i] = resi.ToImmutableArray();
//    //        }
//    //        return new Matrix(res.ToImmutableArray());
//    //    }

//    //    public ImmutableArray<double> RightMulRow(in IReadOnlyList<double> a)
//    //    {
//    //        if (a.Count != Rows)
//    //        {
//    //            throw new ArgumentOutOfRangeException();
//    //        }

//    //        int cols = Cols;
//    //        if (cols == 0)
//    //        {
//    //            return ImmutableArray<double>.Empty;
//    //        }

//    //        var res = new double[cols];

//    //        for (int i = 0; i < Rows; i++)
//    //        {
//    //            var bi = Data[i];
//    //            double ai = a[i];
//    //            for (int j = 0; j < cols; j++)
//    //            {
//    //                res[j] += ai * bi[j];
//    //            }
//    //        }

//    //        return res.ToImmutableArray();
//    //    }

//    //    public Matrix RightMulDiag(in IReadOnlyList<double> a)
//    //    {
//    //        int dim = a.Count;
//    //        if (dim == 0)
//    //        {
//    //            return Empty;
//    //        }
//    //        if (dim != Rows)
//    //        {
//    //            throw new ArgumentOutOfRangeException();
//    //        }

//    //        int cols = Cols;
//    //        if (cols == 0)
//    //        {
//    //            return Empty;
//    //        }

//    //        var res = new ImmutableArray<double>[dim];

//    //        for (int i = 0; i < dim; i++)
//    //        {
//    //            var resi = new double[cols];
//    //            double ai = a[i];
//    //            var bi = Data[i];
//    //            for (int j = 0; j < cols; j++)
//    //            {
//    //                resi[j] = bi[j] * ai;
//    //            }
//    //            res[i] = resi.ToImmutableArray();
//    //        }
//    //        return new Matrix(res.ToImmutableArray());
//    //    }

//    //    public Matrix MulDiag(in IReadOnlyList<double> b)
//    //    {
//    //        int rows = Rows;
//    //        if (rows == 0)
//    //        {
//    //            return Empty;
//    //        }
//    //        int cols = Cols;
//    //        if (cols != b.Count)
//    //        {
//    //            throw new ArgumentOutOfRangeException();
//    //        }

//    //        if (cols == 0)
//    //        {
//    //            return Empty;
//    //        }

//    //        var res = new ImmutableArray<double>[rows];

//    //        for (int i = 0; i < rows; i++)
//    //        {
//    //            var resi = new double[cols];
//    //            var ai = Data[i];
//    //            for (int j = 0; j < cols; j++)
//    //            {
//    //                resi[j] = ai[j] * b[j];
//    //            }
//    //            res[i] = resi.ToImmutableArray();
//    //        }
//    //        return new Matrix(res.ToImmutableArray());
//    //    }

//    //    public ImmutableArray<double> MulCol(in IReadOnlyList<double> b)
//    //    {
//    //        int rows = Rows;
//    //        if (rows == 0)
//    //        {
//    //            return ImmutableArray<double>.Empty;
//    //        }
//    //        int cols = Cols;
//    //        if (cols != b.Count)
//    //        {
//    //            throw new ArgumentOutOfRangeException();
//    //        }

//    //        var res = new double[rows];

//    //        for (int i = 0; i < rows; i++)
//    //        {
//    //            var ai = Data[i];
//    //            double resi = 0.0;
//    //            for (int j = 0; j < cols; j++)
//    //            {
//    //                resi += ai[j] * b[j];
//    //            }
//    //            res[i] = resi;
//    //        }
//    //        return res.ToImmutableArray();
//    //    }

//    //    public Matrix MulTrans(in Matrix b)
//    //    {
//    //        int rows = Rows;
//    //        int cols = b.Rows;
//    //        if (rows == 0 || cols == 0)
//    //        {
//    //            return Empty;
//    //        }
//    //        int aCols = Cols;
//    //        if (aCols != b.Cols)
//    //        {
//    //            throw new ArgumentOutOfRangeException();
//    //        }

//    //        var res = new ImmutableArray<double>[rows];

//    //        for (int i = 0; i < rows; i++)
//    //        {
//    //            var resi = new double[cols];
//    //            var ai = Data[i];
//    //            for (int j = 0; j < cols; j++)
//    //            {
//    //                var bj = b.Data[j];
//    //                double ij = 0.0;
//    //                for (int k = 0; k < aCols; k++)
//    //                {
//    //                    ij += ai[k] * bj[k];
//    //                }
//    //                resi[j] = ij;
//    //            }
//    //            res[i] = resi.ToImmutableArray();
//    //        }
//    //        return new Matrix(res.ToImmutableArray());
//    //    }

//    //    public Matrix Add(double b)
//    //    {
//    //        if (Rows == 0)
//    //        {
//    //            return Empty;
//    //        }
//    //        int cols = Cols;
//    //        if (cols == 0)
//    //        {
//    //            return Empty;
//    //        }

//    //        var res = new ImmutableArray<double>[Rows];

//    //        for (int i = 0; i < Rows; i++)
//    //        {
//    //            var resi = new double[cols];
//    //            var ai = Data[i];
//    //            for (int j = 0; j < cols; j++)
//    //            {
//    //                resi[j] = ai[j] + b;
//    //            }
//    //            res[i] = resi.ToImmutableArray();
//    //        }
//    //        return new Matrix(res.ToImmutableArray());
//    //    }

//    //    public Matrix SubFrom(double b)
//    //    {
//    //        if (Rows == 0)
//    //        {
//    //            return Empty;
//    //        }
//    //        int cols = Cols;
//    //        if (cols == 0)
//    //        {
//    //            return Empty;
//    //        }

//    //        var res = new ImmutableArray<double>[Rows];

//    //        for (int i = 0; i < Rows; i++)
//    //        {
//    //            var resi = new double[cols];
//    //            var ai = Data[i];
//    //            for (int j = 0; j < cols; j++)
//    //            {
//    //                resi[j] = b - ai[j];
//    //            }
//    //            res[i] = resi.ToImmutableArray();
//    //        }
//    //        return new Matrix(res.ToImmutableArray());
//    //    }

//    //    public Matrix Add(Matrix b)
//    //    {
//    //        int cols = Cols;
//    //        if (Rows != b.Rows || cols != b.Cols)
//    //        {
//    //            throw new ArgumentException();
//    //        }
//    //        if (Rows == 0 || cols == 0)
//    //        {
//    //            return Empty;
//    //        }

//    //        var res = new ImmutableArray<double>[Rows];

//    //        for (int i = 0; i < Rows; i++)
//    //        {
//    //            var resi = new double[cols];
//    //            var ai = Data[i];
//    //            var bi = b.Data[i];
//    //            for (int j = 0; j < cols; j++)
//    //            {
//    //                resi[j] = ai[j] + bi[j];
//    //            }
//    //            res[i] = resi.ToImmutableArray();
//    //        }
//    //        return new Matrix(res.ToImmutableArray());
//    //    }

//    //    public Matrix Sub(Matrix b)
//    //    {
//    //        int cols = Cols;
//    //        if (Rows != b.Rows || cols != b.Cols)
//    //        {
//    //            throw new ArgumentException();
//    //        }
//    //        if (Rows == 0 || cols == 0)
//    //        {
//    //            return Empty;
//    //        }

//    //        var res = new ImmutableArray<double>[Rows];

//    //        for (int i = 0; i < Rows; i++)
//    //        {
//    //            var resi = new double[cols];
//    //            var ai = Data[i];
//    //            var bi = b.Data[i];
//    //            for (int j = 0; j < cols; j++)
//    //            {
//    //                resi[j] = ai[j] - bi[j];
//    //            }
//    //            res[i] = resi.ToImmutableArray();
//    //        }
//    //        return new Matrix(res.ToImmutableArray());
//    //    }

//    //    public static Matrix operator *(Matrix a, Matrix b) => a.Mul(b);

//    //    public static Matrix operator *(Matrix a, double b) => a.Mul(b);

//    //    public static Matrix operator *(double a, Matrix b) => b.Mul(a);

//    //    public static Matrix operator +(Matrix a, double b) => a.Add(b);

//    //    public static Matrix operator +(double a, Matrix b) => b.Add(a);

//    //    public static Matrix operator -(Matrix a, double b) => a.Add(-b);

//    //    public static Matrix operator -(double a, Matrix b) => b.SubFrom(a);

//    //    public static Matrix operator +(Matrix a, Matrix b) => a.Add(b);

//    //    public static Matrix operator -(Matrix a, Matrix b) => a.Sub(b);





//    //    public override string ToString()
//    //    {
//    //        if (Rows == 0)
//    //        {
//    //            return "[]";
//    //        }
//    //        int cols = Cols;
//    //        var lines = new string[Rows][];
//    //        var maxlen = new int[cols];
//    //        for (int i = 0; i < Rows; i++)
//    //        {
//    //            var row = Data[i];
//    //            var line = new string[cols];
//    //            for (int j = 0; j < cols; j++)
//    //            {
//    //                var s = row[j].ToString("G17", CultureInfo.InvariantCulture);
//    //                if (s.Length > maxlen[j])
//    //                {
//    //                    maxlen[j] = s.Length;
//    //                }
//    //                line[j] = s;
//    //            }
//    //            lines[i] = line;
//    //        }
//    //        for (int j = 1; j < cols; j++)
//    //        {
//    //            maxlen[j]++;
//    //        }
//    //        var sb = new StringBuilder();
//    //        for (int i = 0; i < Rows; i++)
//    //        {
//    //            var row = lines[i];
//    //            sb.Append('[');
//    //            for (int j = 0; j < cols; j++)
//    //            {
//    //                sb.Append(row[j].PadLeft(maxlen[j]));
//    //            }
//    //            sb.AppendLine("]");
//    //        }
//    //        return sb.ToString();
//    //    }

//    //}
//}
