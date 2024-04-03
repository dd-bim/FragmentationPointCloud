using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeometryLib.Double.D3
{
    public readonly struct StochasticVector
    {
        public Vector Vector { get; }

        public SpdMatrix Cxx { get; }

        public double x => Vector.x;

        public double y => Vector.y;

        public double z => Vector.z;

        public StochasticVector(Vector vector, SpdMatrix cxx)
        {
            Vector = vector;
            Cxx = cxx;
        }

        public StochasticVector Mean(in StochasticVector other)
        {
            var tp = Cxx.CholeskyInv();
            var op = other.Cxx.CholeskyInv();
            var qxx = (tp + op).CholeskyInv();
            var vec = qxx * (tp * Vector + op * other.Vector);
            var tv = vec - Vector;
            var ov = vec - other.Vector;
            var vPv = tp.RowMulSymMulCol(tv) + op.RowMulSymMulCol(ov);
            var cxx = qxx * (vPv / 3.0);

            return new StochasticVector(vec, cxx);
        }
    }
}
