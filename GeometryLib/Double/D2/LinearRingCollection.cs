using System;

/* Unmerged change from project 'GeometryLib (netstandard2.1)'
Before:
using System.Collections.Generic;
using static System.Math;
After:
using System.Collections.Generic;

using static System.Math;
*/

/* Unmerged change from project 'GeometryLib (netstandard2.0)'
Before:
using System.Collections.Generic;
using static System.Math;
After:
using System.Collections.Generic;

using static System.Math;
*/
using System.Collections.Generic;

namespace GeometryLib.Double.D2
{
    public class LinearRingCollection
    {
        public List<LineString> Exteriors { get; }

        public List<LineString> Interiors { get; }

        public BBox BBox { get; private set; }

        public LinearRingCollection()
        {
            Exteriors = new List<LineString>();
            Interiors = new List<LineString>();
            BBox = BBox.Empty;
        }

        public LinearRingCollection(in LineString linearRing)
        {
            if (linearRing.IsLinearRing)
            {
                if (linearRing.Area > 0)
                {
                    Exteriors = new List<LineString> { linearRing };
                    Interiors = new List<LineString>();
                    BBox = BBox.Combine(linearRing.BBox);
                }
                else
                {
                    Interiors = new List<LineString> { linearRing };
                    Exteriors = new List<LineString>();
                    BBox = BBox.Empty; // eigentlich bräuchte man hier eine inverse BBox
                }
            }
            else
            {
                Exteriors = new List<LineString>();
                Interiors = new List<LineString>();
                BBox = BBox.Empty;
            }
           
        }

        public LinearRingCollection(in IReadOnlyCollection<LineString> linearRings)
        {
            Exteriors = new List<LineString>(linearRings.Count);
            Interiors = new List<LineString>(linearRings.Count);
            foreach (var ring in linearRings)
            {
                if (ring.IsLinearRing)
                {
                    if (ring.Area > 0)
                    {
                        Exteriors.Add(ring);
                        BBox = BBox.Combine(ring.BBox);
                    }
                    else
                    {
                        Interiors.Add(ring);
                    }
                }
            }
        }

        public bool Add(LineString linearRing)
        {
            if (linearRing.IsLinearRing)
            {
                if (linearRing.Area > 0)
                {
                    Exteriors.Add(linearRing);
                    BBox = BBox.Combine(linearRing.BBox);
                }
                else
                {
                    Interiors.Add(linearRing);
                }
                return true;
            }
            return false;
        }

        public void ToString(string separator, out string exteriors, out string interiors)
        {
            var exts = new string[Exteriors.Count];
            for (var i = 0; i < Exteriors.Count; i++)
            {
                exts[i] = Exteriors[i].ToWktString();
            }
            var ints = new string[Interiors.Count];
            for (var i = 0; i < Interiors.Count; i++)
            {
                ints[i] = Interiors[i].ToWktString();
            }
            exteriors = '(' + string.Join(separator, exts) + ')';
            interiors = '(' + string.Join(separator, ints) + ')';
        }

        public override string ToString()
        {
            ToString(",", out var exts, out var ints);
            return exts + " " + ints;
        }

        public void ToWktString(out string exteriors, out string interiors)
        {
            ToString(",", out var exts, out var ints);
            exteriors = WKTNames.GeometryCollection + exts;
            interiors = WKTNames.GeometryCollection + ints;
        }

        public static bool TryParse(in string inputExteriors, in string? inputInteriors, out LinearRingCollection linearRingCollection, string separator = ",")
        {
            linearRingCollection = new LinearRingCollection();
            if (!TryParse(inputExteriors, ref linearRingCollection, separator))
            {
                return false;
            }
            if(inputInteriors != null)
            _ = TryParse(inputInteriors, ref linearRingCollection, separator);
            return true;
        }

        public static bool TryParseWkt(in string inputExteriors, in string? inputInteriors, out LinearRingCollection linearRingCollection)
        {
            linearRingCollection = new LinearRingCollection();
            if (!TryParseWkt(inputExteriors, ref linearRingCollection))
            {
                return false;
            }
            _ = TryParseWkt(inputInteriors, ref linearRingCollection);
            return true;
        }

        private static bool TryParse(in string input, ref LinearRingCollection linearRingCollection, string separator = ",")
        {
            var si = input.IndexOf('(') + 1;
            var lastei = input.LastIndexOf(')');
            if (si > 0 && (lastei - si) > WKTNames.LineString.Length)
            {
                si = input.IndexOf(WKTNames.LineString, si);
                if (si > 0)
                {
                    var ei = input.IndexOf(')', si + WKTNames.LineString.Length) + 1;
                    while (ei > si && ei <= lastei && LineString.TryParseWkt(input[si..ei], out var lineString, true))
                    {
                        linearRingCollection.Add(lineString);
                        var ci = input.IndexOf(',', ei);
                        if (ci < 0) break;
                        si = input.IndexOf(WKTNames.LineString, ci);
                        ei = input.IndexOf(')', si + WKTNames.LineString.Length) + 1;
                    }
                    return true;
                }
            }
           return false;
        }

        private static bool TryParseWkt(in string? input, ref LinearRingCollection linearRingCollection)
        {
            if (input is null)
                return false;
            var wi = input.IndexOf(WKTNames.GeometryCollection, StringComparison.InvariantCultureIgnoreCase);
            return wi >= 0 && TryParse(input[(wi + WKTNames.GeometryCollection.Length)..], ref linearRingCollection);
        }


    }
}
