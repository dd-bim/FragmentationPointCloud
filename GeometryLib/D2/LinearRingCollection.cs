using System.Collections.Generic;

namespace GeometryLib.D2;

public class LinearRingCollection
{
    public List<LineString> Exteriors { get; } = [];

    public List<LineString> Interiors { get; } = [];

    public bool Add(LineString linearRing)
    {
        if (!linearRing.IsLinearRing) return false;
        if (linearRing.Area > 0)
        {
            Exteriors.Add(linearRing);
        }
        else
        {
            Interiors.Add(linearRing);
        }

        return true;

    }
}