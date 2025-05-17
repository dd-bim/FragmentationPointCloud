using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using static Playground.GpcWrapper;


namespace Playground;

public enum GpcOperation
{
    Difference = 0,
    Intersection = 1,
    XOr = 2,
    Union = 3
}

public readonly struct VertexList(bool isHole, (double x, double y)[] vertices)
{
    public int NofVertices => Vertices.Length;
    public readonly bool IsHole = isHole;
    public readonly (double x, double y)[] Vertices = vertices;
}

public readonly struct Polygon
{
    public int NofContours => Contours.Length;
    public readonly VertexList[] Contours;

    public Polygon(VertexList contour)
    {
        Contours = [contour];
    }

    public Polygon(VertexList[] contours)
    {
        Contours = contours;
    }
}

internal static partial class GpcWrapper
{
    public static Polygon Clip(GpcOperation operation, Polygon subjectPolygon, Polygon clipPolygon)
    {
        var gpcPolygon = new GpcPolygon();
        var gpcSubjectPolygon = PolygonTo_gpc_polygon(subjectPolygon);
        var gpcClipPolygon = PolygonTo_gpc_polygon(clipPolygon);

        GpcPolygonClip(operation, ref gpcSubjectPolygon, ref gpcClipPolygon, ref gpcPolygon);
        var polygon = Gpc_polygon_ToPolygon(gpcPolygon);

        Free_gpc_polygon(gpcSubjectPolygon);
        Free_gpc_polygon(gpcClipPolygon);
        GpcFreePolygon(ref gpcPolygon);

        return polygon;
    }

    internal static Polygon MakePolygon(Polygon polygon)
    {
        var gpcPolygon1 = PolygonTo_gpc_polygon(polygon);

        // Workaround to make the polygon valid again
        var empty = new GpcPolygon();
        var gpcPolygon2 = new GpcPolygon();
        GpcPolygonClip(GpcOperation.Union, ref empty, ref gpcPolygon1,  ref gpcPolygon2);

        // Free the original polygon and the empty polygon
        var newPolygon = Gpc_polygon_ToPolygon(gpcPolygon2);
        Free_gpc_polygon(gpcPolygon1);
        GpcFreePolygon(ref empty);
        GpcFreePolygon(ref gpcPolygon2);

        return newPolygon;
    }

    private static GpcVertexList VertexListToGpc(VertexList vertexList)
    {
        var gpcVtxList = new GpcVertexList
        {
            num_vertices = vertexList.NofVertices,
            vertex = Marshal.AllocCoTaskMem(vertexList.NofVertices * Marshal.SizeOf(new GpcVertex()))
        };
        IntPtr ptr = gpcVtxList.vertex;
        for (int j = 0; j < vertexList.NofVertices; j++)
        {
            (double x, double y) = vertexList.Vertices[j];
            var gpcVtx = new GpcVertex
            {
                x = x,
                y = y
            };
            Marshal.StructureToPtr(gpcVtx, ptr, false);
            ptr = IntPtr.Add(ptr, Marshal.SizeOf(gpcVtx));
        }

        return gpcVtxList;
    }

    private static GpcPolygon PolygonTo_gpc_polygon(Polygon polygon)
    {
        var gpcPol = new GpcPolygon
        {
            num_contours = polygon.NofContours
        };

        int[] hole = new int[polygon.NofContours];
        for (int i = 0; i < polygon.NofContours; i++)
            hole[i] = polygon.Contours[i].IsHole ? 1 : 0;
        gpcPol.hole = Marshal.AllocCoTaskMem(polygon.NofContours * Marshal.SizeOf(0));

        if (polygon.NofContours > 0)
        {
            Marshal.Copy(hole, 0, gpcPol.hole, polygon.NofContours);
            gpcPol.contour = Marshal.AllocCoTaskMem(polygon.NofContours * Marshal.SizeOf(new GpcVertexList()));
        }
        IntPtr ptr = gpcPol.contour;
        for (int i = 0; i < polygon.NofContours; i++)
        {
            var contour = polygon.Contours[i];
            var gpcVtxList = new GpcVertexList
            {
                num_vertices = contour.NofVertices,
                vertex = Marshal.AllocCoTaskMem(contour.NofVertices * Marshal.SizeOf(new GpcVertex()))
            };
            IntPtr ptr2 = gpcVtxList.vertex;
            for (int j = 0; j < contour.NofVertices; j++)
            {
                (double x, double y) = contour.Vertices[j];
                var gpcVtx = new GpcVertex
                {
                    x = x,
                    y = y
                };
                Marshal.StructureToPtr(gpcVtx, ptr2, false);
                ptr2 = IntPtr.Add(ptr2, Marshal.SizeOf(gpcVtx));
            }
            Marshal.StructureToPtr(gpcVtxList, ptr, false);
            ptr = IntPtr.Add(ptr, Marshal.SizeOf(gpcVtxList));
        }

        return gpcPol;
    }

    private static Polygon Gpc_polygon_ToPolygon(GpcPolygon gpcPolygon)
    {
        if (gpcPolygon.num_contours == 0 || gpcPolygon.hole == IntPtr.Zero || gpcPolygon.contour == IntPtr.Zero)
            return new Polygon([]);

        var contours = new VertexList[gpcPolygon.num_contours];
        int[] holeInt = new int[gpcPolygon.num_contours];

        Marshal.Copy(gpcPolygon.hole, holeInt, 0, gpcPolygon.num_contours);

        IntPtr ptr = gpcPolygon.contour;
        for (int i = 0; i < gpcPolygon.num_contours; i++)
        {
            var gpcVtxList = (GpcVertexList)Marshal.PtrToStructure(ptr, typeof(GpcVertexList))!;
            if (gpcVtxList.vertex == IntPtr.Zero)
                return new Polygon([]);

            var vertices = new (double x, double y)[gpcVtxList.num_vertices];
            IntPtr ptr2 = gpcVtxList.vertex;
            for (int j = 0; j < gpcVtxList.num_vertices; j++)
            {
                var gpcVtx = (GpcVertex)Marshal.PtrToStructure(ptr2, typeof(GpcVertex))!;
                vertices[j] = (gpcVtx.x, gpcVtx.y);
                ptr2 = checked((IntPtr)((long)ptr2 + Marshal.SizeOf(gpcVtx)));
            }
            ptr = checked((IntPtr)((long)ptr + Marshal.SizeOf(gpcVtxList)));
            contours[i] = new VertexList(holeInt[i] != 0, vertices);
        }

        return new Polygon(contours);
    }

    private static void Free_gpc_polygon(GpcPolygon gpcPol)
    {
        if (gpcPol.hole != IntPtr.Zero)
            Marshal.FreeCoTaskMem(gpcPol.hole);

        if (gpcPol.contour == IntPtr.Zero) return;
        IntPtr ptr = gpcPol.contour;
        for (int i = 0; i < gpcPol.num_contours; i++)
        {
            var gpcVtxList = (GpcVertexList)Marshal.PtrToStructure(ptr, typeof(GpcVertexList))!;
            if (gpcVtxList.vertex != IntPtr.Zero)
                Marshal.FreeCoTaskMem(gpcVtxList.vertex);

            ptr = checked((IntPtr)((long)ptr + Marshal.SizeOf(gpcVtxList)));
        }
        Marshal.FreeCoTaskMem(gpcPol.contour);
    }

    [LibraryImport("gpc.dll", EntryPoint = "gpc_polygon_clip", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
    public static partial void GpcPolygonClip(GpcOperation setOperation, ref GpcPolygon subjectPolygon, ref GpcPolygon clipPolygon, ref GpcPolygon resultPolygon);

    [LibraryImport("gpc.dll", EntryPoint = "gpc_free_polygon", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
    public static partial void GpcFreePolygon(ref GpcPolygon polygon);


    [StructLayout(LayoutKind.Sequential)]
    public struct GpcVertex
    {
        public double x;
        public double y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GpcVertexList
    {
        public int num_vertices;
        public IntPtr vertex;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GpcPolygon
    {
        public int num_contours;
        public IntPtr hole;
        public IntPtr contour;
    }
}