using System;
using System.Collections.Generic;

namespace GeometryLib.Double.D3;

public readonly struct CoordinateSystem
{
    private static readonly CoordinateSystem zero = new(Vector.Zero, RotMatrix.Unit);

    public static ref readonly CoordinateSystem Zero => ref zero;

    public Vector Position { get; }

    public RotMatrix Rotation { get; }

    public Direction Normal => Rotation.AxisZ;

    public Direction PlaneX => Rotation.AxisX;

    public double D => -(Position * Rotation.AxisZ);

    public CoordinateSystem(in Vector? position, in RotMatrix rotation)
    {
        Position = position ?? Vector.Zero;
        Rotation = rotation;
    }

    public CoordinateSystem(in Vector? position, in D2.Direction azimuthalRotation)
    {
        Position = position ?? Vector.Zero;
        Rotation = new RotMatrix(
            new Direction(azimuthalRotation.x, azimuthalRotation.y, 0.0, true), Axes.X,
            new Direction(-azimuthalRotation.y, azimuthalRotation.x, 0.0, true));
    }

    public CoordinateSystem(in Vector? position, in double azimuthalRotation) : this(position,
        new D2.Direction(azimuthalRotation))
    {
    }

    public CoordinateSystem(in Vector? position, in Quaternion quaternion) : this(position, new RotMatrix(quaternion))
    {
    }

    public CoordinateSystem(in Vector? position, in Direction reference, in Axes referenceAxis, Vector? next = null)
        : this(position, new RotMatrix(reference, referenceAxis, next))
    {
    }

    public Vector ToSystem(in Vector vector)
    {
        return (vector - Position) * Rotation;
        //  new Vector(p.Dot(AxisX), p.Dot(AxisY), p.Dot(AxisZ));
    }

    public D2.Vector ToPlaneSystem(in Vector vector, out double z)
    {
        Vector p = vector - Position;
        z = p.Dot(Rotation.AxisZ);
        return new D2.Vector(p.Dot(Rotation.AxisX), p.Dot(Rotation.AxisY));
    }

    public D2.Vector ToSystem2d(in Vector vector)
    {
        Vector p = vector - Position;
        return new D2.Vector(p.Dot(Rotation.AxisX), p.Dot(Rotation.AxisY));
    }

    public Vector[] ToSystem(in IReadOnlyList<Vector> vectors)
    {
        var transformed = new Vector[vectors.Count];
        for (var i = 0; i < transformed.Length; i++) transformed[i] = ToSystem(vectors[i]);
        return transformed;
    }

    public D2.Vector[] ToSystem(in IReadOnlyList<Vector> vectors, out double[] zs)
    {
        var transformed = new D2.Vector[vectors.Count];
        zs = new double[transformed.Length];
        for (var i = 0; i < transformed.Length; i++)
        {
            transformed[i] = ToPlaneSystem(vectors[i], out double z);
            zs[i] = z;
        }

        return transformed;
    }

    public Direction ToSystem(in Direction vector)
    {
        return vector * Rotation;
    }

    public D2.Direction ToSystem(in Direction direction, out double scale)
    {
        Direction dir = direction * Rotation;
        return D2.Direction.Create(dir.x, dir.y, out scale);
    }

    public Plane ToSystem(in Plane plane)
    {
        Vector position = ToSystem(plane.Position);
        Direction normal = ToSystem(plane.Normal);
        Direction planeX = ToSystem(plane.PlaneX);
        return new Plane(position, normal, planeX);
    }

    public Vector FromSystem(in Vector vector)
    {
        return Rotation * vector + Position;
    }

    public Vector FromPlaneSystem(in D2.Vector vector)
    {
        return Rotation.AxisX.Mul(vector.x) + Rotation.AxisY.Mul(vector.y) + Position;
    }

    public Vector[] FromSystem(in IReadOnlyList<Vector> vectors)
    {
        var transformed = new Vector[vectors.Count];
        for (var i = 0; i < transformed.Length; i++) transformed[i] = FromSystem(vectors[i]);
        return transformed;
    }

    public Vector[] FromSystem(in IReadOnlyList<D2.Vector> vectors)
    {
        var transformed = new Vector[vectors.Count];
        for (var i = 0; i < transformed.Length; i++) transformed[i] = FromPlaneSystem(vectors[i]);
        return transformed;
    }

    public Direction FromSystem(in Direction vector)
    {
        return Rotation * vector;
    }

    public Direction FromSystem(in D2.Direction vector)
    {
        return Rotation * vector;
    }

    public Plane FromSystem(in Plane plane)
    {
        Vector position = FromSystem(plane.Position);
        Direction normal = FromSystem(plane.Normal);
        Direction planeX = FromSystem(plane.PlaneX);
        return new Plane(position, normal, planeX);
    }


    public CoordinateSystem Combine(in CoordinateSystem other)
    {
        // Lösung mit Quaternionen, aufwändiger aber numerisch stabiler
        var q = new Quaternion(other.Rotation);
        Vector p = other.Position + q.Transform(Position);
        q *= new Quaternion(Rotation);
        return new CoordinateSystem(p, q);
    }

    public static CoordinateSystem Combine(params CoordinateSystem[] systems)
    {
        return Combine((IReadOnlyList<CoordinateSystem>)systems);
    }

    public static CoordinateSystem Combine(in IReadOnlyList<CoordinateSystem> systems)
    {
        if (systems.Count < 1) return Zero;
        // Lösung mit Quaternionen, aufwändiger aber numerisch stabiler
        var q = new Quaternion(systems[0].Rotation);
        Vector t = systems[0].Position;

        for (var i = 1; i < systems.Count; i++)
        {
            var qi = new Quaternion(systems[i].Rotation);
            q = qi * q;
            t = systems[i].Position + qi.Transform(t);
        }

        return new CoordinateSystem(t, q);
    }

    public override string ToString()
    {
        return $"T:{Position}\r\nR:{Rotation}";
    }

    //D2.Vector Plane.ToPlaneSystem(in Vector vector)=> ToSystem2d(vector);

    public bool ApproxEquals(in Plane other, in double maxDifferenceD,
        in double maxDifferenceCosOne = Constants.TRIGTOL)
    {
        return Math.Abs(1.0 - Normal.Dot(other.Normal)) <= maxDifferenceCosOne
               && Math.Abs(D - other.D) <= maxDifferenceD;
    }

    public Plane GetPlane()
    {
        return new Plane(this);
    }
}