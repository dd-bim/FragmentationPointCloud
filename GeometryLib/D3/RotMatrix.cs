using System;
using System.Globalization;
using System.Text;

namespace GeometryLib.D3;

public enum Axes
{
    X,
    Y,
    Z
}

public readonly struct RotMatrix
{
    public Direction AxisX { get; }

    public Direction AxisY { get; }

    public Direction AxisZ { get; }

    private double xx => AxisX.x;
    private double yx => AxisX.y;
    private double zx => AxisX.z;
    private double xy => AxisY.x;
    private double yy => AxisY.y;
    private double zy => AxisY.z;
    private double xz => AxisZ.x;
    private double yz => AxisZ.y;
    private double zz => AxisZ.z;

    public RotMatrix(in Direction reference, in Axes referenceAxis, Vector? next = null)
    {
        var second = next.HasValue ? reference.MakePerp(next.Value, out var third) : reference.Perp(out third);
        switch (referenceAxis)
        {
            case Axes.X:
                AxisX = reference;
                AxisY = second;
                AxisZ = third;
                break;
            case Axes.Y:
                AxisY = reference;
                AxisZ = second;
                AxisX = third;
                break;
            case Axes.Z:
            default:
                AxisZ = reference;
                AxisX = second;
                AxisY = third;
                break;
        }
    }


    public static Vector operator *(in RotMatrix r, in Vector v)
    {
        return new Vector(r.xx * v.x + r.xy * v.y + r.xz * v.z, r.yx * v.x + r.yy * v.y + r.yz * v.z, r.zx * v.x + r.zy * v.y + r.zz * v.z);
    }

    public static Vector operator *(in RotMatrix r, in D2.Vector v)
    {
        return new Vector(r.xx * v.x + r.xy * v.y, r.yx * v.x + r.yy * v.y, r.zx * v.x + r.zy * v.y);
    }

    public static Direction operator *(in RotMatrix r, in Direction v)
    {
        return new Direction(r.xx * v.x + r.xy * v.y + r.xz * v.z, r.yx * v.x + r.yy * v.y + r.yz * v.z, r.zx * v.x + r.zy * v.y + r.zz * v.z);
    }

    public static Direction operator *(in RotMatrix r, in D2.Direction v)
    {
        return new Direction(r.xx * v.x + r.xy * v.y, r.yx * v.x + r.yy * v.y, r.zx * v.x + r.zy * v.y);
    }


    public static Vector operator *(in Vector v, in RotMatrix r)
    {
        return new Vector(r.AxisX.Dot(v), r.AxisY.Dot(v), r.AxisZ.Dot(v));
    }

    public static Direction operator *(in Direction v, in RotMatrix r)
    {
        return new Direction(r.AxisX.Dot(v), r.AxisY.Dot(v), r.AxisZ.Dot(v));
    }


    public override string ToString()
    {
        string[] lineX =
        [
            xx.ToString("G17", CultureInfo.InvariantCulture), xy.ToString("G17", CultureInfo.InvariantCulture),
            xz.ToString("G17", CultureInfo.InvariantCulture)
        ];
        string[] lineY =
        [
            yx.ToString("G17", CultureInfo.InvariantCulture), yy.ToString("G17", CultureInfo.InvariantCulture),
            yz.ToString("G17", CultureInfo.InvariantCulture)
        ];
        string[] lineZ =
        [
            zx.ToString("G17", CultureInfo.InvariantCulture), zy.ToString("G17", CultureInfo.InvariantCulture),
            zz.ToString("G17", CultureInfo.InvariantCulture)
        ];
        int maxX = Math.Max(lineX[0].Length, Math.Max(lineY[0].Length, lineZ[0].Length));
        int maxY = Math.Max(lineX[1].Length, Math.Max(lineY[1].Length, lineZ[1].Length));
        int maxZ = Math.Max(lineX[2].Length, Math.Max(lineY[2].Length, lineZ[2].Length));
        string fX = "[{0,-" + maxX;
        string fY = "} {1,-" + maxY;
        string fZ = "} {2,-" + maxZ + "}]";
        string f = fX + fY + fZ;

        var sb = new StringBuilder();
        sb.AppendLine(string.Format(f, lineX[0], lineX[1], lineX[2]));
        sb.AppendLine(string.Format(f, lineY[0], lineY[1], lineY[2]));
        sb.AppendLine(string.Format(f, lineZ[0], lineZ[1], lineZ[2]));

        return sb.ToString();
    }
}