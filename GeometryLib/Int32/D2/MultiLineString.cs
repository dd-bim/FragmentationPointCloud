using BigMath;
using GeometryLib.Interfaces;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

using static System.Math;

namespace GeometryLib.Int32.D2
{
    public readonly struct MultiLineString : IReadOnlyList<LineString>//, IGeometryOgc2<int>
    {

        public int GeometricDimension => 1;

        public ImmutableArray<LineString> LineStrings { get; }

        public BBox BBox { get; }

        public int Count => LineStrings.Length;

        public LineString this[int index] => LineStrings[index];

        public MultiLineString(in LineString lineString)
        {
            LineStrings = ImmutableArray.Create(lineString.MakeInvalidLinearRing());
            BBox = lineString.BBox;
        }

        internal MultiLineString(in BBox box, in ImmutableArray<LineString> lineStrings)
        {
            LineStrings = lineStrings;
            BBox = box;
        }

        public MultiLineString(in IReadOnlyCollection<LineString> lineStrings)
        {
            var lss = ImmutableArray.CreateBuilder<LineString>(lineStrings.Count);
            var box = BBox.Empty;
            foreach (var ls in lineStrings)
            {
                lss.Add(ls.MakeInvalidLinearRing());
                box = box.Combine(ls.BBox);
            }

            LineStrings = lss.MoveToImmutable();
            BBox = box;
        }

        public MultiLineString(in Polygon polygon)
        {
            var lss = ImmutableArray.CreateBuilder<LineString>(polygon.Count);
            foreach (var ls in polygon)
            {
                lss.Add(ls.MakeInvalidLinearRing());
            }

            LineStrings = lss.MoveToImmutable();
            BBox = polygon.BBox;
        }

        public string ToString(string separator)
        {
            var lss = new string[LineStrings.Length];
            for (var i = 0; i < LineStrings.Length; i++)
            {
                lss[i] = LineStrings[i].ToString();
            }
            return '(' + string.Join(separator, lss) + ')';
        }

        public override string ToString() => ToString(",");

        public string ToWktString() => "MULTILINESTRING" + ToString(",");

        private static bool TryParse(in string? input, out MultiLineString lineStringCollection, string separator = ",")
        {
            if (string.IsNullOrEmpty(input))
            {
                lineStringCollection = default;
                return false;
            }
            var start = Min(input!.Length, input.IndexOf('(') + 1);
            var end = start + 2;
            var lss = new List<LineString>();
            while (start > 0 && end < input.Length)
            {
                if (!LineString.TryParse(input[start..], false, out var lineString, out end))
                {
                    lineStringCollection = default;
                    return false;
                }
                lss.Add(lineString);
                end = Min(input.Length, end + start + 1);
                start = input.IndexOf(separator, end, StringComparison.Ordinal) + 1;
            }
            lineStringCollection = new MultiLineString(lss);
            return input.IndexOf(')', end) > 0;
        }

        private static bool TryParseWkt(in string? input, out MultiLineString lineStringCollection)
        {
            if (string.IsNullOrEmpty(input) || input!.Length <= 17)
            {
                lineStringCollection = default;
                return false;
            }
            var start = input.IndexOf("MULTILINESTRING", StringComparison.InvariantCultureIgnoreCase) + 15;
            if (start > 14 && start < input.Length)
            {
                return TryParse(input[start..], out lineStringCollection);
            }

            lineStringCollection = default;
            return false;
        }


        public IEnumerator<LineString> GetEnumerator() => ((IEnumerable<LineString>)LineStrings).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    }
}