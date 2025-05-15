using System;
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

        //public void ToString(string separator, out string exteriors, out string interiors)
        //{
        //    var exts = new string[Exteriors.Count];
        //    for (var i = 0; i < Exteriors.Count; i++)
        //    {
        //        exts[i] = Exteriors[i].ToWktString();
        //    }
        //    var ints = new string[Interiors.Count];
        //    for (var i = 0; i < Interiors.Count; i++)
        //    {
        //        ints[i] = Interiors[i].ToWktString();
        //    }
        //    exteriors = '(' + string.Join(separator, exts) + ')';
        //    interiors = '(' + string.Join(separator, ints) + ')';
        //}

        //public override string ToString()
        //{
        //    ToString(",", out var exts, out var ints);
        //    return exts + " " + ints;
        //}


    }
}