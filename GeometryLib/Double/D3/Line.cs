

//using static GeometryLib.Double.Constants;
//using static System.Math;

//namespace GeometryLib.Double.D3
//{
//    public readonly struct Line
//    {
//        public Vector Position { get; }

//        public Direction Direction { get; }

//        public Line(Vector position, Direction direction)
//        {
//            Position = position;
//            Direction = direction;
//        }

//        public static Line Create(Vector source, Vector dest)=> new Line(source.Mid(dest), (Direction)(dest - source));

//        public bool Intersection(in Plane plane, out Vector point)
//        {
//            double d = plane.Normal.Dot(Direction);
//            if(Abs(d) < EPS)
//            {
//                point = default;
//                return false;
//            }
//            double n = plane.Normal.Dot(plane.Position - Position);
//            if (Abs(n) < EPS)
//            {
//                point = Position;
//                return true;
//            }
//            double dist = n / d;
//            point = Position + dist * Direction;
//            return true;
//        }

//        public bool IsInLine(in Vector point) => Direction.Cross(point - Position).SumSq() < TRIGTOL_SQUARED;

//    }
//}
