namespace GeometryLib.Double
{
    public static class Constants
    {

        public const double EPS = 1.0 / (1L << 52);

        public const double SMALL = 1.0e-14;

        public const double THIRD = 1.0 / 3.0;

        public const double SQRT2 = 1.4142135623730950488016887242097;

        public const double SQRT3 = 1.7320508075688772935274463415059;

        /// <summary>
        ///  kleinstmöglicher Trig/Det Wert
        /// </summary>
        public const double TRIGTOL = 1.0e-11;

        /// <summary>
        ///  kleinstmöglicher Schnitt-Winkel Wert, bei Ebene
        /// </summary>
        public const double PLANETOL = 1.0e-3;

        /// <summary>
        /// Quadrierter kleinstmöglicher Trig/Det Wert
        /// </summary>
        public const double TRIGTOL_SQUARED = TRIGTOL * TRIGTOL;

        public const double DISTTOL = 1.0e-4;

        public const double DISTTOL_SQUARED = DISTTOL * DISTTOL;

        public const double HALFPI = 1.5707963267948966192313216916398;

        public const double TWOPI = 6.283185307179586476925286766559;

        public const double RSQRT2 = 0.70710678118654752440084436210485;

        public const double RSQRT3 = 0.57735026918962576450914878050196;
    }
}
