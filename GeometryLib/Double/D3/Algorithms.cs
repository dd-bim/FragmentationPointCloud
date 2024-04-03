namespace GeometryLib.Double.D3
{
    public static class Algorithms
    {
        public static bool DistinctToConvexPolyhedron(in Plane[] convexPolyhedron, in Vector point)
        {
            foreach (var plane in convexPolyhedron)
            {
                if(plane.Dist(point) <= 0)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
