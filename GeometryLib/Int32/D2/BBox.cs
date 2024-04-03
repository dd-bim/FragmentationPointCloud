using System.Collections.Generic;

namespace GeometryLib.Int32.D2
{
    /// <summary>
    /// Bounding Box
    /// </summary>
    public readonly struct BBox
    {
        private static readonly BBox empty = new BBox
        (
            new Vector(0, 0),
            new Vector(0, 0)
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
            if (vector.x < 0 || vector.y < 0) throw new System.ArgumentOutOfRangeException();
            this.Min = vector;
            this.Max = vector;
        }

        public BBox(in Vector min, in Vector max)
        {
            if (min.x < 0 || min.y < 0 || min.x > max.x || min.y > max.y) throw new System.ArgumentOutOfRangeException();
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

        public BBox Buffer(int value) => new BBox(new Vector(Min.x - value, Min.y - value), new Vector(Max.x + value, Max.y + value));

        public static BBox operator +(in BBox box, in Vector vector) => box.Extend(vector);

        public static BBox operator +(in BBox left, in BBox right) => left.Combine(right);

        public override string ToString() => $"Min({Min}) Max({Max})";
    }
}
