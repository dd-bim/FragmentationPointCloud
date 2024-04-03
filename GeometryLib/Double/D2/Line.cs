//using System;

//using static GeometryLib.Double.Constants;

//namespace GeometryLib.Double.D2
//{
//    /// <summary>
//    /// Gerade
//    /// </summary>
//    public readonly struct Line
//    {
//        public Vector Position { get; }

//        public Direction Direction { get; }

//        public Line(in Direction direction, in Vector position)
//        {
//            Position = position;
//            Direction = direction;
//        }

//        public static bool Create(in Vector orig, in Vector dest, out Line line)
//        {
//            var direction = Direction.Create(dest - orig, out var length); 
//            if (double.IsNaN(length) || length < Constants.DISTTOL)
//            {
//                line = default;
//                return false;
//            }
//            line = new Line(direction, orig.Mid(dest));
//            return false;
//        }

//        public static bool Create(in Edge edge, out Line line) => Create(edge.Orig, edge.Dest, out line);

//        public int SideSign(in Vector v) => Math.Sign(v.Sub(Position).Det(Direction));

//        public bool Intersection(in Edge edge, out Vector p)
//        {
//            var aa = edge.Orig - Position;
//            var ab2 = edge.Vector;

//            double num1 = ab2.Det(aa);
//            double num2 = Direction.Det(aa);
//            double den = ab2.Det(Direction);
//            // force positive denominator
//            if (den < 0)
//            {
//                num1 = -num1;
//                num2 = -num2;
//                den = -den;
//            }

//            if (num2 < 0 || den < TRIGTOL || num2 > den)
//            {
//                p = default;
//                return false;
//            }

//            if (Math.Abs(num1) > num2)
//            {
//                num1 /= den;
//                p = new Vector(Position.x + Direction.x * num1, Position.y + Direction.y * num1);
//            }
//            else
//            {
//                num2 /= den;
//                p = new Vector(edge.Orig.x + ab2.x * num2, edge.Orig.y + ab2.y * num2);
//            }
//            return true;
//        }

//        public bool Intersection(in Line other, out Vector p)
//        {
//            var aa = other.Position - Position;

//            double num1 = other.Direction.Det(aa);
//            double num2 = Direction.Det(aa);
//            double den = other.Direction.Det(Direction);

//            if (Math.Abs(den) < TRIGTOL)
//            {
//                p = default;
//                return false;
//            }

//            if (Math.Abs(num1) > Math.Abs(num2))
//            {
//                num1 /= den;
//                p = new Vector(Position.x + Direction.x * num1, Position.y + Direction.y * num1);
//            }
//            else
//            {
//                num2 /= den;
//                p = new Vector(other.Position.x + other.Direction.x * num2, other.Position.y + other.Direction.y * num2);
//            }
//            return true;
//        }

//    }
//}
