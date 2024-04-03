using System;

namespace GeometryLib.Double.D2
{
    //public readonly struct StochasticLine
    //{
    //    public Line Line { get; }

    //    //Dir / Pos
    //    public Linear.SymMatrix Cxx { get; }

    //    public StochasticLine(Line line, Linear.SymMatrix cxx)
    //    {
    //        Line = line;
    //        Cxx = cxx;
    //    }

    //    public bool Intersection(in Line other, out StochasticVector p)
    //    {
    //        Vector u = Line.Direction;
    //        Vector v = other.Direction;
    //        double d = u.Det(v);
    //        if (Math.Abs(d) < Constants.TRIGTOL)
    //        {
    //            p = default;
    //            return false;
    //        }

    //        double s = v.Det(Line.Position - other.Position) / d;

    //        var (tx, ty) = Line.Position;
    //        var (dx, dy) = Line.Direction;
    //        var (otx, oty) = other.Position;
    //        var (odx, ody) = other.Direction;
    //        var d2 = d * d;

    //        // dx,dy,tx,ty,odx,ody,otx,oty
    //        var F = new Linear.Matrix(new double[,]
    //        {
    //            {
    //                 (odx * (dx * (ody * ty - ody * oty) - dy * s)) / d,
    //                -(dx * odx * (odx * ty - s - odx * oty)) / d,
    //                -(dy * odx) / d,
    //                dx * odx,
    //            },
    //            {
    //                (dy * ody * (odx * ty - s - odx * oty)) / d,
    //                -(dy * odx * (odx * ty - odx * oty) - dx * ody * s)/d,
    //                -(dy * ody) / d,
    //                dx * ody - d + 1,
    //             }
    //        });

    //        p = new StochasticVector(Line.Position + s * u, new SymMatrix(Cxx.MatMulSymMulMatTrans(F)));

    //        return true;
    //    }

    //    public bool Intersection(in StochasticLine other, out StochasticVector p)
    //    {
    //        Vector u = Line.Direction;
    //        Vector v = other.Line.Direction;
    //        double d = u.Det(v);
    //        if (Math.Abs(d) < Constants.TRIGTOL)
    //        {
    //            p = default;
    //            return false;
    //        }

    //        double s = v.Det(Line.Position - other.Line.Position) / d;

    //        var (tx, ty) = Line.Position;
    //        var (dx, dy) = Line.Direction;
    //        var (otx, oty) = other.Line.Position;
    //        var (odx, ody) = other.Line.Direction;
    //        var d2 = d * d;

    //        // dx,dy,tx,ty,odx,ody,otx,oty
    //        var F = new Linear.Matrix(new double[,]
    //        {
    //            {
    //                 (odx * (dx * (ody * ty - ody * oty) - dy * s)) / d,
    //                -(dx * odx * (odx * ty - s - odx * oty)) / d,
    //                -(dy * odx) / d,
    //                dx * odx,
    //                (d * (d * dx * (ty - oty) + dy * (otx - tx)) + dy * dy * odx * (otx - tx)) / d2,
    //                (dx * dy * odx * (tx - otx)) / d2,
    //                (dy * odx + d) / d,
    //                -dx * odx
    //            },
    //            {
    //                (dy * ody * (odx * ty - s - odx * oty)) / d,
    //                -(dy * odx * (odx * ty - odx * oty) - dx * ody * s)/d,
    //                -(dy * ody) / d,
    //                dx * ody - d + 1,
    //                (dy * (d2 * (ty - oty) + dy * (ody * otx - ody * tx))) / d2,
    //                (dy * dy * odx * (tx - otx)) / d2,
    //                (dy * ody) / d,
    //                -(dx * ody - d)
    //            }
    //        });

    //        var cxx = new Linear.SymMatrix(8).SetRange(0, Cxx).SetRange(4, other.Cxx);

    //        p = new StochasticVector(Line.Position + s * u, new SymMatrix(cxx.MatMulSymMulMatTrans(F)));

    //        return true;
    //    }
    //}
}
