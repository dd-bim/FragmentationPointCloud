//using static GeometryLib.Double.Constants;
//using static System.Math;


//namespace GeometryLib.Double.D3
//{
//    public readonly struct Segment
//    {
//        public Vector Source { get; }

//        public Vector Dest { get; }

//        public Segment(in Vector source, in Vector dest)
//        {
//            Source = source;
//            Dest = dest;
//        }

//        public override string ToString()
//        {
//            return $"({Source})-({Dest})";
//        }

//        public bool Intersection(in Plane plane, out Vector point)
//        {
//            var u = Dest - Source;
//            var w = plane.Position - Source;

//            double d = plane.Normal.Dot(u);
//            if (Abs(d) < EPS)
//            {
//                point = default;
//                return false;
//            }
//            double n = plane.Normal.Dot(w) / d;
//            if(n < 0.0 || n > 1.0)
//            {
//                point = default;
//                return false;
//            }
//            point = Source + n * u;
//            return true;
//        }

//        public bool IsInSegment(in Vector point, out double relativePosition)
//        {
//            var ap = point - Source;
//            var ab = Dest - Source;
//            if(ap.Cross(ab).SumSq() > TRIGTOL_SQUARED)
//            {
//                relativePosition = default;
//                return false;
//            }

//            double num = ab.Dot(ap);
//            double den = ab.SumSq();

//            if (num < 0 || den == 0 || num > den)
//            {
//                relativePosition = default;
//                return false;
//            }

//            relativePosition = num / den;
//            return true;
//        }
//    }
//}
