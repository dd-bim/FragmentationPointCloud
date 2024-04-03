//using static GeometryLib.Double.Constants;
//using static System.Math;

//namespace GeometryLib.Double.D3
//{
//    public readonly struct Ray
//    {
//        public Vector Origin { get; }

//        public Direction Direction { get; }

//        public Ray(Vector position, Direction direction)
//        {
//            Origin = position;
//            Direction = direction;
//        }

//        public static Ray Create(Vector origin, Vector destination) => new Ray(origin, (Direction)(destination - origin));

//        public bool Intersection(in Plane plane, out Vector point, out double distance)
//        {
//            double d = plane.Normal.Dot(Direction);
//            if (d < EPS)
//            {
//                point = default;
//                distance = default;
//                return false;
//            }
//            double n = plane.Normal.Dot(plane.Position - Origin);
//            if (Abs(n) < EPS)
//            {
//                point = Origin;
//                distance = default;
//                return true;
//            }
//            distance = n / d;
//            point = Origin + distance * Direction;
//            return true;
//        }

//        public bool IsInRay(in Vector point)
//        {
//            var vec = point - Origin;
//            return Direction.Cross(vec).SumSq() < TRIGTOL_SQUARED && Direction.Dot(vec) > 0;
//        }
//    }
//}
