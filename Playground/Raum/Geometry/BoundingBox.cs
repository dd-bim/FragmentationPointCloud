using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Playground.Raum.Geometry;

public record BoundingBox
{
    public double MinX { get; private set; } = double.PositiveInfinity;

    public double MinY { get; private set; } = double.PositiveInfinity;

    public double MaxX { get; private set; } = double.NegativeInfinity;

    public double MaxY { get; private set; } = double.NegativeInfinity;

    public double RangeX => MaxX - MinX;

    public double RangeY => MaxY - MinY;

    public void Extend(double x, double y)
    {
        MinX = double.Min(MinX, x);
        MinY = double.Min(MinY, y);
        MaxX = double.Max(MaxX, x);
        MaxY = double.Max(MaxY, y);
    }

    public void Extend((double x, double y)[] ring)
    {
        foreach (var (x, y) in ring)
        {
            Extend(x, y);
        }
    }


    public void Extend((double x, double y)[][] polygon)
    {
        foreach (var ring in polygon)
        {
            Extend(ring);
        }
    }

    public void Extend((double x, double y)[][][] multiPolygon)
    {
        foreach (var polygon in multiPolygon)
        {
            Extend(polygon);
        }
    }

}