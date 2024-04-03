using System.Collections.Generic;

namespace GeometryLib.Double.D3
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

        public static BBox FromVectors(in IReadOnlyCollection<Vector> vectors)
        {
            var bbox = Empty;
            foreach (var v in vectors)
            {
                bbox += v;
            }
            return bbox;
        }

        public Vector Centre => Min.Mid(Max);

        public Vector Range => Max - Min;

        public BBox Extend(in Vector vector) => new BBox(
            vector.Min(Min),
            vector.Max(Max));

        public BBox Combine(in BBox other) =>
            new BBox(
                Min.Min(other.Min),
                Max.Max(other.Max)
            );

        public bool DoOverlap(in BBox other) =>
            (other.Min.x < Max.x) && (Min.x < other.Max.x) 
            && (other.Min.y < Max.y) && (Min.y < other.Max.y)
            && (other.Min.z < Max.z) && (Min.z < other.Max.z);

        public bool DoOverlapOrTouch(in BBox other) =>
            (other.Min.x <= Max.x) && (Min.x <= other.Max.x) 
            && (other.Min.y <= Max.y) && (Min.y <= other.Max.y)
            && (other.Min.z <= Max.z) && (Min.z <= other.Max.z);

        public bool Encloses(in BBox other) =>
            (other.Min.x >= Min.x) && (other.Max.x <= Max.x) 
            && (other.Min.y >= Min.y) && (other.Max.y <= Max.y) 
            && (other.Min.z >= Min.z) && (other.Max.z <= Max.z);

        public bool Distinct(in BBox other) =>
            (other.Max.x < Min.x) || (other.Min.x > Max.x) 
            || (other.Max.y < Min.y) || (other.Min.y > Max.y)
            || (other.Max.z < Min.z) || (other.Min.z > Max.z);

        public bool Encloses(in Vector vector) =>
            (vector.x > Min.x) && (vector.x < Max.x) 
            && (vector.y > Min.y) && (vector.y < Max.y)
            && (vector.z > Min.z) && (vector.z < Max.z);

        public bool EnclosesOrTouch(in Vector vector) =>
            (vector.x >= Min.x) && (vector.x <= Max.x) 
            && (vector.y >= Min.y) && (vector.y <= Max.y)
            && (vector.z >= Min.z) && (vector.z <= Max.z);

        public bool Distinct(in Vector vector) =>
            (vector.x < Min.x) || (vector.x > Max.x) 
            || (vector.y < Min.y) || (vector.y > Max.y)
            || (vector.z < Min.z) || (vector.z > Max.z);


        public BBox Buffer(double value) => new BBox(new Vector(Min.x - value, Min.y - value, Min.z - value), new Vector(Max.x + value, Max.y + value, Max.z + value));


        public static BBox operator +(in BBox box, in Vector vector) => box.Extend(vector);

        public static BBox operator +(in BBox left, in BBox right) => left.Combine(right);

        public override string ToString() => $"Min({Min}) Max({Max})";
    }
}
