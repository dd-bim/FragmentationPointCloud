using Playground.Raum.Topology;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Playground.Raum.Geometry;

public readonly record struct Epsilon(double Scale, double OriginX, double OriginY)
{
    public static bool Create(byte digits, BoundingBox box, out Epsilon epsilon)
    {
        if (digits > 28)
        {
            epsilon = default;
            return false;
        }
        double scale = scaleFromDigits(digits);
        double range = double.Max(box.RangeX, box.RangeY);
        if (double.Ceiling(range * scale) > long.MaxValue)
        {
            epsilon = default;
            return false;
        }
        epsilon = new Epsilon(scale, box.MinX, box.MinY);
        return true;
    }

    private static double scaleFromDigits(byte digits) => (double)(1m / new decimal(1, 0, 0, false, digits));

    public VecI Convert(double x, double y)
    {
        long xi = (long)double.Round((x - OriginX) * Scale);
        long yi = (long)double.Round((y - OriginY) * Scale);
        return new VecI(xi, yi);
    }


    public (double x, double y) Convert(VecI xy)
    {
        double x = (xy.X / Scale) + OriginX;
        double y = (xy.Y / Scale) + OriginY;
        return (x, y);
    }

    internal bool Convert(Vertex v, out (double x, double y) point)
    {
        if (v.Point.HasValue)
        {
            point = Convert(v.Point.Value);
            return true;
        }
        var (source, target) = v.RefHalfEdge.Edge;
        bool exact = VecI.PointOnEdge(source, target, v.RefHalfEdge.Position, out var pointi);
        point = Convert(pointi);
        return exact;
    }

}



