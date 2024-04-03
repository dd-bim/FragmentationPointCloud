using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeometryLib.Double.D3
{
    public readonly struct TriMatrix
    {
        public readonly double xx, xy, xz, yy, yz, zz;

        public TriMatrix(
            double xx, double xy, double xz,
                       double yy, double yz,
                                  double zz)
        {
            this.xx = xx;
            this.xy = xy;
            this.xz = xz;
            this.yy = yy;
            this.yz = yz;
            this.zz = zz;
        }

        public void Deconstruct(
            out double outXx, out double outXy, out double outXz,
                              out double outYy, out double outYz,
                                                out double outZz)
        {
            outXx = xx;
            outXy = xy;
            outXz = xz;
            outYy = yy;
            outYz = yz;
            outZz = zz;
        }

        public TriMatrix Inv()
        {
            var (xx, xy, xz, yy, yz, zz) = this;
            zz = 1.0 / zz;
            yy = 1.0 / yy;
            yz = -yz * zz * yy;
            xx = 1.0 / xx;
            xz = -(xy * yz + xz * zz) * xx;
            xy = -xy * yy * xx;

            return new TriMatrix(xx, xy, xz, yy, yz, zz);
        }

        public SpdMatrix SelfMul()
        {
            var (xx, xy, xz, yy, yz, zz) = this;
            // this' * this
            return new SpdMatrix(
                xx * xx + xy * xy + xz * xz, xy * yy + xz * yz, xz * zz, 
                                             yy * yy + yz * yz, yz * zz, 
                                                                zz * zz);
        }
    }
}
