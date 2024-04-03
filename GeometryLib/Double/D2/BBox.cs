using System.Collections.Generic;

namespace GeometryLib.Double.D2
{
    /// <summary>
    /// Bounding Box
    /// </summary>
    public readonly struct BBox
    {
        private static readonly BBox empty = new BBox
        (
            Vector.PositiveInfinity,
            Vector.NegativeInfinity
        );

        /// <summary>
        /// Minimal point
        /// </summary>
        public Vector Min { get; }

        /// <summary>
        /// Maximal point
        /// </summary>
        public Vector Max { get; }

        /// <summary>
        /// Bounding Box with no extent
        /// </summary>
        public static ref readonly BBox Empty => ref empty;

        public BBox(in Vector vector)
        {
            this.Min = vector;
            this.Max = vector;
        }

        public BBox(in Vector min, in Vector max)
        {
            this.Min = min;
            this.Max = max;
        }

        public static BBox FromVectors(in IReadOnlyList<Vector> vectors, bool isRing = false)
        {
            var bbox = Empty;
            for (var i = isRing ? 1 : 0; i < vectors.Count; i++)
            {
                var v = vectors[i];
                bbox += v;
            }
            return bbox;
        }

        public Vector Centre => Min.Mid(Max);

        public Vector Range => Max - Min;

        public BBox Extend(in Vector vector) => new BBox(
            vector.Min(Min), vector.Max(Max));

        public BBox Combine(in BBox other) =>
            new BBox(Min.Min(other.Min), Max.Max(other.Max));

        public bool DoOverlap(in BBox other) =>
            (other.Min.x < Max.x) && (Min.x < other.Max.x) && (other.Min.y < Max.y) && (Min.y < other.Max.y);

        public bool DoOverlapOrTouch(in BBox other) =>
            (other.Min.x <= Max.x) && (Min.x <= other.Max.x) && (other.Min.y <= Max.y) && (Min.y <= other.Max.y);

        public bool Encloses(in BBox other) =>
            (other.Min.x >= Min.x) && (other.Max.x <= Max.x) && (other.Min.y >= Min.y) && (other.Max.y <= Max.y);

        public bool Distinct(in BBox other) =>
            (other.Max.x < Min.x) || (other.Min.x > Max.x) || (other.Max.y < Min.y) || (other.Min.y > Max.y);

        public bool Encloses(in Vector vector) =>
            (vector.x > Min.x) && (vector.x < Max.x) && (vector.y > Min.y) && (vector.y < Max.y);

        public bool EnclosesOrTouch(in Vector vector) =>
            (vector.x >= Min.x) && (vector.x <= Max.x) && (vector.y >= Min.y) && (vector.y <= Max.y);

        public bool Distinct(in Vector vector) =>
            (vector.x < Min.x) || (vector.x > Max.x) || (vector.y < Min.y) || (vector.y > Max.y);

        //public bool Intersects(in Line line)
        //{
        //    int sign0 = line.SideSign(Min);
        //    int sign1 = line.SideSign(new Vector(Max.x, Min.y));
        //    int sign2 = line.SideSign(Max);
        //    int sign3 = line.SideSign(new Vector(Min.x, Max.y));
        //    return sign0 == 0 || sign1 == 0 || sign2 == 0 || sign3 == 0 
        //           || sign0 != sign1 || sign0 != sign2 || sign0 != sign3
        //           || sign1 != sign2 || sign1 != sign3
        //           || sign2 != sign3;
        //}

        public bool EnclosesOrTouch(in Edge edge) => EnclosesOrTouch(edge.Orig) || EnclosesOrTouch(edge.Dest);

        public BBox Buffer(double value) => new BBox(new Vector(Min.x - value, Min.y - value), new Vector(Max.x + value, Max.y + value));

        public static BBox operator +(in BBox box, in Vector vector) => box.Extend(vector);

        public static BBox operator +(in BBox left, in BBox right) => left.Combine(right);

        public override string ToString() => $"Min({Min}) Max({Max})";
    }
}
