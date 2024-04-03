using System;


namespace GeometryLib.Int32.D2
{
    public readonly struct Triangle : IEquatable<Triangle>
    {

        public Vector a { get; }

        public Vector b { get; }

        public Vector c { get; }

        public long Determinant { get; }

        private Triangle(Vector a, Vector b, Vector c, long determinant)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            Determinant = determinant;
        }

        public static bool Create(Vector a, Vector b, Vector c, out Triangle triangle)
        {
            var det = a.Det(b, c);
            if(det <= 0)
            {
                triangle = default;
                return false;
            }
            triangle = new Triangle(a, b, c, det);
            return true;
        }

        public bool Equals(Triangle other) => Determinant.Equals(other.Determinant)
            && ((a.Equals(other.a) && b.Equals(other.b) && c.Equals(other.c))
             || (a.Equals(other.b) && b.Equals(other.c) && c.Equals(other.a))
             || (a.Equals(other.c) && b.Equals(other.a) && c.Equals(other.b)));

        public override bool Equals(object? obj) => obj is Vector point && Equals(point);

        public override int GetHashCode() => Determinant.GetHashCode();

        public static bool operator ==(Triangle left, Triangle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Triangle left, Triangle right)
        {
            return !(left == right);
        }
    }
}
