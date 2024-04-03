namespace GeometryLib.Int32
{
    internal static class Helper
    {
        public static void Sort(ref int a, ref int b) => (a, b) = a > b ? (b, a) : (a, b);

        public static void Sort(ref int a, ref int b, ref int c)
        {
            Sort(ref a, ref b);
            Sort(ref b, ref c);
            Sort(ref a, ref b);
        }
    }
}
