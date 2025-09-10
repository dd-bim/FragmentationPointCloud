using Autodesk.Revit.DB;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit.Data
{
    public sealed record DataPlane(in XYZ Normal, in XYZ XVec, in XYZ YVec, in XYZ Origin)
    {

        public static DataPlane CreateByThreePoints(in XYZ a, in XYZ b, in XYZ c)
        {
            var ab = b - a;
            var ac = c - a;
            var normal = ab.CrossProduct(ac).Normalize();
            var xVec = ab.Normalize();
            var yVec = normal.CrossProduct(xVec).Normalize();
            return new DataPlane(normal, xVec, yVec, a);
        }

        public static DataPlane CreateByOriginAndBasis(in XYZ origin, in XYZ xVec, in XYZ yVec)
        {
            var n = xVec.CrossProduct(yVec).Normalize();
            var x = xVec.Normalize();
            var y = n.CrossProduct(x).Normalize();
            return new DataPlane(n, x, y, origin);
        }

        public XYZ FromPlaneSystem(in UV uv)
        {
            return Origin + (uv.U * XVec) + (uv.V * YVec);
        }

        public void Project(in XYZ xyz, out UV uv, out double distanceToPlane)
        {
            var v = xyz - Origin;
            distanceToPlane = v.DotProduct(Normal);
            var vProj = v - (distanceToPlane * Normal);
            uv = new UV(vProj.DotProduct(XVec), vProj.DotProduct(YVec));
        }


    }

}
