using System;

namespace GeometryLib.D3;

public readonly struct Plane
{
    public CoordinateSystem System { get; }

    /// <summary>
    ///     Face Normal, Normalized Vector pointing outside volume
    /// </summary>
    public Direction Normal => System.Rotation.AxisZ;

    /// <summary>
    ///     Arbitrary Point on Face
    /// </summary>
    public Vector Position => System.Position;

    private double D { get; }

    public Direction PlaneX => System.Rotation.AxisX;

    private Plane(CoordinateSystem system)
    {
        System = system;
        D = -(system.Position * system.Rotation.AxisZ);
    }

    public Plane(in Vector position, in Direction normal, Direction? planeX = null) : this(
        new CoordinateSystem(position, normal, Axes.Z, planeX))
    {
    }
    
    public D2.Vector ToPlaneSystem(in Vector vector)
    {
        Vector p = vector - System.Position;
        return new D2.Vector(p.Dot(System.Rotation.AxisX), p.Dot(System.Rotation.AxisY));
    }

    public D2.Vector ToPlaneSystem(in Vector vector, out double z)
    {
        Vector p = vector - System.Position;
        z = p.Dot(System.Rotation.AxisZ);
        return new D2.Vector(p.Dot(System.Rotation.AxisX), p.Dot(System.Rotation.AxisY));
    }

    public Vector FromPlaneSystem(in D2.Vector vector)
    {
        return (System.Rotation.AxisX * vector.x) + (System.Rotation.AxisY * vector.y) + System.Position;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Normal, D);
    }
}