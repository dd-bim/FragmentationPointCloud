using GeometryLib.Double.D2;

using System;

using static GeometryLib.Double.Constants;
using static System.Math;

namespace GeometryLib.Double.D3
{
    public readonly struct StochasticPlane : IPlane
    {
        public RotMatrix Rotation { get; }

        public Direction Normal => Rotation.AxisZ;

        public Direction PlaneX => Rotation.AxisX;

        public Direction PlaneY => Rotation.AxisY;

        public Vector Position { get; }

        public D6.SpdMatrix Cxx { get; }

        public double D { get; }

        public StochasticPlane(Vector position, Direction normal, D6.SpdMatrix cxx)
        {
            Rotation = new RotMatrix(normal, Axes.Z);
            Position = position;
            D = -normal.Dot(position);
            Cxx = cxx;
        }

        public StochasticPlane(in Vector position, in Direction normal, Direction planeX, in D6.SpdMatrix cxx)
        {
            Rotation = new RotMatrix(normal, Axes.Z, planeX);
            Position = position;
            D = -normal.Dot(position);
            Cxx = cxx;
        }


        public double Dist(Vector point) => Normal.Dot(point) + D;

        public bool ApproxEquals(in IPlane other, in double maxDifferenceD, in double maxDifferenceCosOne = TRIGTOL) =>
            Abs(1.0 - Normal.Dot(other.Normal)) <= maxDifferenceCosOne
            && Abs(D - other.D) <= maxDifferenceD;


        public D2.Direction ToSystem(in Direction direction)
        {
            return new D2.Direction(direction.Dot(PlaneX), direction.Dot(PlaneY));
        }

        public D2.Direction ToSystem(in Direction direction, out double scale)
        {
            var x = direction.Dot(PlaneX);
            var y = direction.Dot(PlaneY);
            return D2.Direction.Create(x, y, out scale);
        }

        public D2.Vector ToPlaneSystem(in Vector vector)
        {
            var p = vector - Position;
            return new D2.Vector(p.Dot(PlaneX), p.Dot(PlaneY));
        }

        public D2.Vector ToPlaneSystem(in Vector vector, out double z)
        {
            var p = vector - Position;
            z = p.Dot(Normal);
            return new D2.Vector(p.Dot(PlaneX), p.Dot(PlaneY));
        }

        //public D2.Line ToSystem(in Line line) => new D2.Line(ToSystem(line.Direction, out _), ToSystem(line.Position));

        //public D2.Line ToSystem(in Line line, out double scale, out double z) => new D2.Line(ToSystem(line.Direction, out scale), ToSystem(line.Position, out z));



        public Direction FromSystem(in D2.Direction direction) => Rotation * direction;

        public Vector FromPlaneSystem(in D2.Vector vector)
        {
            return PlaneX.Mul(vector.x) + PlaneY.Mul(vector.y) + Position;
        }

        public StochasticVector FromSystemWithCxx(in D2.Vector vector)
        {
            // Grundannahme: PlaneY von PlaneX und Normale abgeleitet (Normale x PlaneX) 
            // und PlaneX orthogonal zur Normale (PlaneY x Normale)
            // Berechnung nach: PlaneX.Mul(vector.x) + PlaneY.Mul(vector.y) + Position
            var (vx, vy) = vector;
            var (xx, xy, xz) = PlaneX;
            var (nx, ny, nz) = Normal * vx;
            var nxxx = nx * xx;
            var nxxy = nx * xy;
            var nxxz = nx * xz;
            var nyxx = ny * xx;
            var nyxy = ny * xy;
            var nyxz = ny * xz;
            var nzxx = nz * xx;
            var nzxy = nz * xy;
            var nzxz = nz * xz;
            var vyxx = vy * xx;
            var vyxy = vy * xy;
            var vyxz = vy * xz;
            var F = new double[][]{
                new[] {              -nzxz - nyxy, vyxz + nyxx + nyxx - nxxy, nzxx + nzxx - nxxz - vyxy, 1, 0, 0 },
                new[] { nxxy + nxxy - nyxx - vyxz,              -nzxz - nxxx, nzxy + nzxy - nyxz + vyxx, 0, 1, 0 },
                new[] { nxxz + nxxz - nzxx + vyxy, nyxz + nyxz - nzxy - vyxx,              -nyxy - nxxx, 0, 0, 1 }
            };

            var cxx = Cxx.MatMulSymMulTrans6x3(F);
            return new StochasticVector(FromPlaneSystem(vector), cxx);
        }

        public StochasticVector AddCxx(in Vector vector) => FromSystemWithCxx(ToPlaneSystem(vector));

        //public Line FromSystem(in D2.Line line) => new Line(FromSystem(line.Position), FromSystem(line.Direction));

        public Vector Intersection(in Vector rayPosition, in Direction rayDirection, out double dist)
        {
            var d = Normal.Dot(rayDirection);
            var n = Dist(rayPosition);
            dist = n / d;
            var locp = dist * rayDirection;
            var vec = rayPosition - locp;

            return vec;
        }

        public bool Intersection(in Vector rayPosition, in Direction rayDirection,
            in double minDist, in double maxDist, in double maxRayPlaneTiltCos, 
            out Vector vector)
        {
            var d = -Normal.Dot(rayDirection);
            // Nur ursprünglich negative Cos Werte nutzen, da ansonsten der Strahl 
            // durch die Rückseite der Ebene gänge
            if (d < maxRayPlaneTiltCos)
            {
                vector = default;
                return false;
            }
            var n = Dist(rayPosition);
            var dist = n / d;
            if (dist < minDist || dist > maxDist)
            {
                vector = default;
                return false;
            }
            var locp = dist * rayDirection;
            vector = rayPosition + locp;

            return true;
        }

        public bool Intersection2(in Vector rayPosition, in Direction rayDirection,
            in double minDist, in double maxDist, in double maxRayPlaneTiltCos,
            out D2.Vector vector)
        {
            var d = Normal.Dot(rayDirection);
            if (Math.Abs(d) < maxRayPlaneTiltCos)
            {
                vector = default;
                return false;
            }
            var n = Dist(rayPosition);
            var dist = -n / d;
            if (dist < minDist || dist > maxDist)
            {
                vector = default;
                return false;
            }
            var locp = dist * rayDirection;
            vector = ToPlaneSystem(rayPosition + locp);

            return true;
        }

        public Plane GetPlane()
        {
            return new Plane(Position, Normal, PlaneX);
        }

        ///// <summary>
        /////  Schnittgerade zweier Ebenen, falls nicht parallel
        ///// </summary>
        //public bool Intersection(in IPlane other, out Line line)
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

        //public bool Intersection2(in IPlane other, out D2.Line line)
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
        //    line = new D2.Line(ToSystem(dir), ToSystem(pos));
        //    return true;
        //}

        //public bool IntersectionWithHorizontalPlane(in double otherZ, out Line line)
        //{
        //    var ba = new Vector(Normal.y, -Normal.x, 0);//Normal.Cross(other.Normal);
        //    var dir = Direction.Create(ba, out double length);
        //    if (length < TRIGTOL)
        //    {
        //        line = default;
        //        return false;
        //    }

        //    var ad = new Vector(-dir.y, dir.x, 0);// other.Normal.Cross(dir);
        //    var db = dir.Cross(Normal);
        //    var den = dir.Dot(ba);
        //    var pos = ((D * ad) - (otherZ * db)) / den;
        //    line = new Line(pos, dir);

        //    return true;
        //}

        //public bool IntersectionWithHorizontalPlane2(in double otherZ, out D2.Line line)
        //{
        //    var ba = new Vector(Normal.y, -Normal.x, 0);//Normal.Cross(other.Normal);
        //    var dir = Direction.Create(ba, out double length);
        //    if (length < TRIGTOL)
        //    {
        //        line = default;
        //        return false;
        //    }

        //    var ad = new Vector(-dir.y, dir.x, 0);// other.Normal.Cross(dir);
        //    var db = dir.Cross(Normal);
        //    var den = dir.Dot(ba);
        //    var pos = ((D * ad) - (otherZ * db)) / den;
        //    line = new D2.Line(ToSystem(dir), ToSystem(pos));

        //    return true;
        //}


        //public D2.StochasticVector ToLocal(in Vector vector)
        //{
        //    var vl = vector - Position;
        //    var F = new Linear.Matrix(new double[,]
        //    {
        //        { vl.x, vl.y, vl.z,   0,    0,    0, -PlaneX.x, -PlaneX.y, -PlaneX.z, 0, 0, 0},
        //        {  0,      0,    0,vl.x, vl.y, vl.z, -PlaneY.x, -PlaneY.y, -PlaneY.z, 0, 0, 0}
        //    });
        //    return new D2.StochasticVector(
        //        new D2.Vector(PlaneX.Dot(vl), PlaneY.Dot(vl)),
        //        new D2.SymMatrix(Cxx.MatMulSymMulMatTrans(F)));
        //}

        //public bool Intersection(in Plane other, out StochasticLine line)
        //{
        //    var u = Normal.Cross(other.Normal);
        //    if (!Direction.Create(u, out var dir3, out double length) || length < TRIGTOL
        //        || !D2.Direction.Create(PlaneX.Dot(dir3), PlaneY.Dot(dir3), out var dir))
        //    {
        //        line = default;
        //        return false;
        //    }
        //    var (ux, uy, uz) = u;
        //    var (nx, ny, nz) = Normal;
        //    var (onx, ony, onz) = other.Normal;
        //    var (tx, ty, tz) = Position;
        //    var (pxx, pxy, pxz) = PlaneX;
        //    var (pyx, pyy, pyz) = PlaneY;
        //    var oD = other.D;
        //    var ux2 = ux * ux;
        //    var uy2 = uy * uy;

        //    var m = new double[4, 12];

        //    // dx / plx ply pln plt
        //    m[0, 0] = ux / length;
        //    m[0, 1] = uy / length;
        //    m[0, 2] = uz / length;
        //    m[0, 6] = (ony * pxz - onz * pxy) / length;
        //    m[0, 7] = (onz * pxx - onx * pxz) / length;
        //    m[0, 8] = (onx * pxy - ony * pxx) / length;

        //    // dy / plx ply pln plt
        //    m[1, 3] = ux / length;
        //    m[1, 4] = uy / length;
        //    m[1, 5] = uz / length;
        //    m[1, 6] = (ony * pyz - onz * pyy) / length;
        //    m[1, 7] = (onz * pyx - onx * pyz) / length;
        //    m[1, 8] = (onx * pyy - ony * pyx) / length;


        //    double ax = Abs(nx), ay = Abs(ny), az = Abs(nz);
        //    Vector p3;
        //    if (ax > ay && ax > az)
        //    {
        //        p3 = new Vector(-Position.x, (oD * nz - D * onz) / ux - Position.y, (D * ony - oD * ny) / ux - Position.z);
        //        // px / plx ply pln plt
        //        m[2, 0] = -tx;
        //        m[2, 1] = -(ty * ux + D * onz - oD * nz) / ux;
        //        m[2, 2] = -(tx * uz + tz * (ux + nz * ony) + ny * (ony * ty + onx * tx + oD)) / ux;
        //        var t = onz * pxy - ony * pxz;
        //        m[2, 6] = (t * tx) / ux;
        //        m[2, 7] = (t * (ty * ux + D * onz - oD * nz)) / ux2;
        //        m[2, 8] = ((t * tz - oD * pxy) * ux + ony * (D * ony - oD * ny) * pxz + (oD * nz * ony - D * ony * onz) * pxy) / ux2;
        //        t = nz * pxy - ny * pxz;
        //        m[2, 9] = (onx * t - pxz * uz - pxy * uy - pxx * ux) / ux;
        //        m[2, 10] = (ony * t) / ux;
        //        m[2, 11] = (nz * (onz * pxy - ony * pxz) - pxz * ux) / ux;

        //        // py / plx ply pln plt
        //        m[3, 3] = m[2, 0];
        //        m[3, 4] = m[2, 1];
        //        m[3, 5] = m[2, 2];
        //        t = onz * pyy - ony * pyz;
        //        m[3, 6] = (t * tx) / ux;
        //        m[3, 7] = (t * (ty * ux + D * onz - oD * nz)) / ux2;
        //        m[3, 8] = ((t * tz - oD * pxy) * ux + ony * (D * ony - oD * ny) * pxz + (oD * nz * ony - D * ony * onz) * pxy) / ux2;
        //        t = nz * pyy - ny * pyz;
        //        m[3, 9] = (onx * t - pyz * uz - pyy * uy - pyx * ux) / ux;
        //        m[3, 10] = (ony * t) / ux;
        //        m[3, 11] = (nz * (onz * pyy - ony * pyz) - pyz * ux) / ux;
        //    }
        //    else if (ay < ax && ay > az)
        //    {
        //        p3 = new Vector((D * onz - oD * nz) / u.y - Position.x, -Position.y, (oD * nx - D * onx) / u.y - Position.z);
        //        // px / plx ply pln plt
        //        m[2, 0] = -(tx * uy - D * onz + oD * nz) / uy;
        //        m[2, 1] = -ty;
        //        m[2, 2] = -(ty * uz + tz * (uy - nz * onx) - nx * (ony * ty + onx * tx + oD)) / uy;
        //        var t = onx * pxz - onz * pxx;
        //        m[2, 6] = (t * (tx * uy - D * onz + oD * nz)) / uy2;
        //        m[2, 7] = (t * ty) / uy;
        //        m[2, 8] = ((t * tz + oD * pxx) * uy + onx * (D * onx - oD * nx) * pxz + (oD * nz * onx - D * onx * onz) * pxx) / uy2;

        //        m[2, 9] = (onx * (nx * pxz - nz * pxx)) / uy;
        //        m[2, 10] = -(pxy * uy + pxx * (ux + nz * ony) - ny * onx * pxz) / uy;
        //        m[2, 11] = (nz * (onx * pxz - onz * pxx) - pxz * uy) / uy;

        //        // py / plx ply pln plt
        //        m[3, 3] = m[2, 0];
        //        m[3, 4] = m[2, 1];
        //        m[3, 5] = m[2, 2];
        //        t = onx * pyz - onz * pyx;
        //        m[3, 6] = (t * (tx * uy - D * onz + oD * nz)) / uy2;
        //        m[3, 7] = (t * ty) / uy;
        //        m[3, 8] = ((t * tz + oD * pyx) * uy + onx * (D * onx - oD * nx) * pyz + (oD * nz * onx - D * onx * onz) * pyx) / uy2;

        //        m[3, 9] = (onx * (nx * pyz - nz * pyx)) / uy;
        //        m[3, 10] = -(pyy * uy + pyx * (ux + nz * ony) - ny * onx * pyz) / uy;
        //        m[3, 11] = (nz * (onx * pyz - onz * pyx) - pyz * uy) / uy;
        //    }
        //    else
        //    {
        //        p3 = new Vector((oD * ny - D * ony) / uz - Position.x, (D * onx - oD * nx) / uz - Position.y, -Position.z);
        //        // px / plx ply pln plt
        //        m[2, 0] = -(tx * uz + D * ony - oD * ny) / uz;
        //        m[2, 1] = -(ty * uz - D * onx + oD * nx) / uz;
        //        m[2, 2] = -tz;
        //        var t = ony * pxx - onx * pxy;
        //        m[2, 6] = (t * tx - oD * pxy) / uz;
        //        m[2, 7] = (t * ty + oD * pxx) / uz;
        //        m[2, 8] = (t * tz) / uz;

        //        m[2, 9] = -(onx * (nx * pxy - ny * pxx)) / uz;
        //        m[2, 10] = -(pxy * (uz + ny * onx) - ny * ony * pxx) / uz;
        //        m[2, 11] = -(pxz * uz + pxx * (ux - ny * onz) + nz * onx * pxy) / uz;

        //        // py / plx ply pln plt
        //        m[3, 3] = m[2, 0];
        //        m[3, 4] = m[2, 1];
        //        m[3, 5] = m[2, 2];
        //        t = ony * pyx - onx * pyy;
        //        m[3, 6] = (t * tx - oD * pyy) / uz;
        //        m[3, 7] = (t * ty + oD * pyx) / uz;
        //        m[3, 8] = (t * tz) / uz;

        //        m[3, 9] = -(onx * (nx * pyy - ny * pyx)) / uz;
        //        m[3, 10] = -(pyy * (uz + ny * onx) - ny * ony * pyx) / uz;
        //        m[3, 11] = -(pyz * uz + pyx * (ux - ny * onz) + nz * onx * pyy) / uz;
        //    }
        //    var F = new Linear.Matrix(m);

        //    line = new D2.StochasticLine(new D2.Line(dir, new D2.Vector(PlaneX.Dot(p3), PlaneY.Dot(p3))), Cxx.MatMulSymMulMatTrans(F));

        //    return true;
        //}


        //public StochasticVector Intersection(in Line line)
        //{
        //    var d = Normal.Dot(line.Direction);
        //    var n = Dist(line.Position);
        //    var dist = n / d;
        //    var locp = dist * line.Direction;
        //    var vec = line.Position - locp;
        //    var (ddx, ddy, ddz) = -line.Direction / d;
        //    var (px, py, pz) = vec;

        //    var F = new double[][] {
        //        new[]{ px * ddx, py * ddx, pz * ddx, ddx },
        //        new[]{ px * ddy, py * ddy, pz * ddy, ddy },
        //        new[]{ px * ddz, py * ddz, pz * ddz, ddz }
        //    };

        //    var gvec = new StochasticVector(vec, Cxx.MatMulSymMulTrans4x3(F));

        //    return gvec;
        //}

    }

}