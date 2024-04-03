using System;
using System.Collections.Generic;

using static GeometryLib.Double.Constants;
using static System.Math;

namespace GeometryLib.Double.D3
{
    public readonly struct Plane : IPlane
    {
        public CoordinateSystem System { get; }

        /// <summary>
        /// Face Normal, Normalized Vector pointing outside volume
        /// </summary>
        public Direction Normal => System.Rotation.AxisZ;

        /// <summary>
        /// Arbitrary Point on Face
        /// </summary>
        public Vector Position => System.Position;

        public double D { get; }

        public Direction PlaneX => System.PlaneX;

        public Direction PlaneY => System.Rotation.AxisY;

        public Plane(CoordinateSystem system)
        {
            System = system;
            D = system.D;
        }

        public Plane(in Vector position, in Direction normal, Direction? planeX = null) : this(new CoordinateSystem(position, normal, Axes.Z, planeX)) { }


        public Plane(in Vector a)
        {
            System = new CoordinateSystem(a, RotMatrix.Unit);
            D = -a.z;
        }

        // Dieser Code ist potentiell identisch mit dem Konstruktor: position und normale!
        //public Plane(in Vector a, in Vector b)
        //{
        //    if (Direction.Create(b - a, out var dir))
        //    {
        //        var az = dir.Perp();
        //        System = new CoordinateSystem(a, az, Axes.Z, dir);
        //        D = -(a.Dot(az));
        //    }
        //    else
        //    {
        //        System = new CoordinateSystem(a, RotMatrix.Unit);
        //        D = -a.z;
        //    }
        //}

        public Plane(in Vector a, in Vector b, in Vector c, bool aAsPosition = false)
        {
            var ab = b - a;
            var normal = (Direction)ab.Cross(c - a);
            var pos = aAsPosition ? a : Vector.Mean(a, b, c);
            System = new CoordinateSystem(pos, normal, Axes.Z, (Direction)ab);
            D = -(pos.Dot(normal));
        }

        public double Dist(Vector point) => Normal.Dot(point - Position);

        public void Difference(in Plane other, out double diffCos, out double diffD)
        {
            diffD = Abs(D - other.D);
            diffCos = Normal.DiffCos(other.Normal);
        }

        public static bool Create(in IReadOnlyList<Vector> vectors, out Plane plane, bool isRing = false)
        {
            var len = isRing ? vectors.Count - 1 : vectors.Count;
            switch (len)
            {
                case -1:
                case 0:
                    plane = default;
                    return false;
                case 1:
                    plane = new Plane(vectors[0]);
                    return true;
                case 2:
                    var dir = (Direction)(vectors[1] - vectors[0]);
                    plane = new Plane(new CoordinateSystem(vectors[0], dir.Perp(), Axes.Z, dir));
                    return true;
                case 3:
                    plane = new Plane(vectors[0], vectors[1], vectors[2], isRing);
                    return true;
                default:
                    var mean = Vector.Mean(vectors, isRing);
                    var red = new Vector[vectors.Count];
                    for (var i = 0; i < len; i++)
                    {
                        red[i] = vectors[i] - mean;
                    }
                    if (isRing)
                    {
                        red[^1] = red[0];
                        var vnormal = red[0].Cross(red[1]);
                        for (var i = 2; i < red.Length; i++)
                        {
                            vnormal += red[i - 1].Cross(red[i]);
                        }
                        plane = new Plane(vectors[0], (Direction)vnormal, (Direction)(vectors[1] - vectors[0]));
                        return true;
                    }
                    else
                    {
                        var cov = red[0].Outer().AddCov(red[1]);
                        for (var i = 2; i < len; i++)
                        {
                            cov = cov.AddCov(red[i]);
                        }
                        var quat = Decomposition.EigenVectors(cov);
                        var sys = new CoordinateSystem(mean, quat);
                        plane = new Plane(sys);
                        return true;
                    }
            }
        }

        public D2.Vector ToPlaneSystem(in Vector vector) => System.ToSystem2d(vector);

        public D2.Vector ToPlaneSystem(in Vector vector, out double z) => System.ToPlaneSystem(vector, out z);

        public D2.Vector[] ToSystem(in IReadOnlyList<Vector> vectors, out double[] zs) => System.ToSystem(vectors, out zs);

        public D2.Direction ToSystem(in Direction direction, out double scale) => System.ToSystem(direction, out scale);

        //public bool ToSystem(in Line line, out D2.Line d2Line, out double z, out double scale) 
        //{
        //    var pos = ToSystem(line.Position, out z);
        //    if(ToSystem(line.Direction, out var dir, out scale))
        //    {
        //        d2Line = new D2.Line(dir, pos);
        //        return true;
        //    }
        //    d2Line = default;
        //    return false;
        //}

        public Vector FromPlaneSystem(in D2.Vector vector) => System.FromPlaneSystem(vector);

        public Direction FromSystem(in D2.Direction direction) => System.FromSystem(direction);

        public Vector[] FromSystem(in IReadOnlyList<D2.Vector> vectors) => System.FromSystem(vectors);

        //public Line FromSystem(in D2.Line line) => new Line(FromSystem(line.Position), FromSystem(line.Direction));

        public Plane Turn() => new Plane(Position, Normal.Turn(), PlaneX); // Achtung evtl. Polygon passt dann nicht mehr!

        ///// <summary>
        /////  Schnittgerade zweier Ebenen, falls nicht parallel
        ///// </summary>
        //public bool Intersection(in Plane other, out Line line)
        //{
        //    var ba = Normal.Cross(other.Normal);
        //    var dir = Direction.Create(ba, out double length);
        //    if (length < TRIGTOL)
        //    {
        //        line = default;
        //        return false;
        //    }

        //    var ad = other.Normal.Cross(dir);
        //    var db = dir.Cross(Normal);
        //    double den = dir.Dot(ba);
        //    var pos = ((D * ad) + (other.D * db)) / den;
        //    line = new Line(pos, dir);

        //    return true;
        //}

        ///// <summary>
        /////  Schnittgerade zweier Ebenen, falls nicht parallel
        ///// </summary>
        //public bool Intersection(in Plane other, out D2.Line line)
        //{
        //    var ba = Normal.Cross(other.Normal);
        //    if (!Direction.Create(ba, out var dir, out double length) || length < TRIGTOL || !ToSystem(dir, out var dir2, out _ ))
        //    {
        //        line = default;
        //        return false;
        //    }

        //    var ad = other.Normal.Cross(dir);
        //    var db = dir.Cross(Normal);
        //    double den = dir.Dot(ba);
        //    var pos = ((D * ad) + (other.D * db)) / den;

        //    line = new D2.Line(dir2, ToSystem(pos));

        //    return true;
        //}

        public bool ApproxEquals(in IPlane other, in double maxDifferenceD, in double maxDifferenceCosOne = TRIGTOL) => 
            System.ApproxEquals(other, maxDifferenceD, maxDifferenceCosOne);


        public override int GetHashCode()
        {
#if NETSTANDARD2_0 || NET472 || NET48 || NET462
            return Normal.GetHashCode() ^ D.GetHashCode();
#else
            return HashCode.Combine(Normal, D);
#endif
        }

        public Plane GetPlane() => this;

        //public Linear.Matrix FMatrixGlobalLocal()
        //{
        //    // Funktionsmatrix für global nach lokal
        //    return new Linear.Matrix(new[,] {
        //        {   -PlaneX.x, -PlaneX.y, -PlaneX.z },
        //        {   -PlaneY.x, -PlaneY.y, -PlaneY.z }
        //    });

        //}

    }
}
