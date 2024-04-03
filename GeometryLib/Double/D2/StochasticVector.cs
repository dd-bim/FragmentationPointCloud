namespace GeometryLib.Double.D2
{
    public readonly struct StochasticVector
    {
        public Vector Vector { get; }

        public SpdMatrix Cxx { get; }

        public double x => Vector.x;

        public double y => Vector.y;

        public StochasticVector(Vector vector, SpdMatrix cxx)
        {
            Vector = vector;
            Cxx = cxx;
        }

        public StochasticVector(D3.StochasticVector svec)
        {
            Vector = new Vector(svec.Vector);
            Cxx = new SpdMatrix(svec.Cxx);
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
            var cxx = qxx * (vPv / 2.0);

            return new StochasticVector(vec, cxx);
        }
    }
}
