using System.Collections.Generic;

namespace GeometryLib.Double.D3
{
    public interface IPlane
    {
        Direction Normal { get; }

        Vector Position { get; }

        Direction PlaneX { get; }

        double D { get; }

        D2.Vector ToPlaneSystem(in Vector vector);

        D2.Vector ToPlaneSystem(in Vector vector, out double z);

        Vector FromPlaneSystem(in D2.Vector vector);

        bool ApproxEquals(in IPlane other, in double maxDifferenceD, in double maxDifferenceCosOne);

        Plane GetPlane();

    }
}