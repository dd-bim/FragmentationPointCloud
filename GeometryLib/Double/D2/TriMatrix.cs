using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeometryLib.Double.D2
{
    public readonly struct TriMatrix
    {
        public readonly double xx, xy, yy;

        public TriMatrix(double xx, double xy, double yy)
        {
            this.xx = xx;
            this.xy = xy;
            this.yy = yy;
        }
        public void Deconstruct(out double outXx, out double outXy, out double outYy)
        {
            outXx = xx;
            outXy = xy;
            outYy = yy;
        }

        public TriMatrix Inv()
        {
            var (a, b, c) = this;
            // Chol Inv
            var yy = 1.0 / c;
            var xx = 1.0 / a;
            var xy = -b * yy * xx;
            return new TriMatrix(xx, xy, yy);
        }

        public SpdMatrix SelfMul()
        {
            var (xx, xy, yy) = this;
            // this' * this
            return new SpdMatrix(
                xx * xx + xy * xy, xy * yy, 
                                   yy * yy);
        }

    }
}
