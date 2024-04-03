//using GeometryLib.Int32;

//using System;
//using System.Diagnostics.Contracts;

//using static System.Math;

//using F = GeometryLib.Int32.Fraction.D2;
//using I = GeometryLib.Int32.D2;

//namespace GeometryLib.Double.D2
//{
//    public struct Conversion
//    {
//        private const double Sqrt2 = 1.4142135623730950488016887242;

//        // smaller as possible to allow external points
//        public static readonly double MaxValue = I.Vector.MaxValue - 2;

//        public readonly double Scale;

//        public readonly BBox BBox;

//        public double PointDistance => Sqrt2 / Scale;

//        private static double? GetScale(double range, double smallestNumber)
//        {
//            double scale = Ceiling(1 / smallestNumber);
//            return Ceiling(range * scale) > MaxValue ? null : scale;
//        }

//        public Conversion(in BBox box, double scale)
//        {
//            BBox = box;
//            Scale = scale;
//        }

//        public static bool Create(in BBox box, double smallestNumber, out Conversion conversion)
//        {
//            var scale = GetScale(Max(box.Range.x, box.Range.y), smallestNumber);
//            if (scale.HasValue)
//            {
//                conversion = new Conversion(box, scale.Value);
//                return true;
//            }
//            conversion = default;
//            return true;
//        } 

//        [Pure]
//        public I.Vector Convert(in Vector p)
//        {
//            double x = Round((p.x - BBox.Min.x) * Scale);
//            double y = Round((p.y - BBox.Min.y) * Scale);
//            return new I.Vector((int)x, (int)y);
//        }

//        public Vector Convert(in I.Vector p) =>
//            new Vector((p.x / Scale) + BBox.Min.x, (p.y / Scale) + BBox.Min.y);


//        public Vector Convert(in F.Vector p) =>
//            new Vector((p.DoubleX / Scale) + BBox.Min.x, (p.DoubleY / Scale) + BBox.Min.y);


//        public Vector Convert(in IIntegerPoint p) => p is I.Vector ip ? Convert(ip) : Convert((F.Vector)p);

//    }
//}