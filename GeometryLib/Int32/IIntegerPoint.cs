using System;

namespace GeometryLib.Int32
{
    public interface IIntegerPoint:IEquatable<IIntegerPoint>
    {
        GeometryLib.Double.D2.Vector ToDouble();

        double DoubleX { get; }

        double DoubleY { get; }

        int x { get; }
        int y { get; }
    }
}
