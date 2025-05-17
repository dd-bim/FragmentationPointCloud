using System;
using System.Collections;
using System.Runtime.InteropServices;
using Autodesk.Revit.DB;
using System.Runtime.InteropServices.Marshalling;


namespace Revit.Data;

public enum GpcOperation
{
    Difference = 0,     
    Intersection = 1,   
    XOr = 2,            
    Union = 3           
}

public readonly struct VertexList(bool isHole, UV[] vertices)
{
    public int NofVertices => Vertices.Length;
    public readonly bool IsHole = isHole;
    public readonly UV[] Vertices = vertices;
}

public readonly struct Polygon(VertexList[] contours)
{
    public int NofContours => Contours.Length;
    public readonly VertexList[] Contours = contours;
}

public static partial class GpcWrapper
{
    public static Polygon Clip(GpcOperation operation, Polygon subjectPolygon, Polygon clipPolygon)
    {
        var gpcPolygon = new GpcPolygon();
        var gpcSubjectPolygon = GpcWrapper.PolygonTo_gpc_polygon(subjectPolygon);
        var gpcClipPolygon = GpcWrapper.PolygonTo_gpc_polygon(clipPolygon);

        GpcPolygonClip(operation, ref gpcSubjectPolygon, ref gpcClipPolygon, ref gpcPolygon);
        var polygon = GpcWrapper.Gpc_polygon_ToPolygon(gpcPolygon);

        GpcWrapper.Free_gpc_polygon(gpcSubjectPolygon);
        GpcWrapper.Free_gpc_polygon(gpcClipPolygon);
        GpcWrapper.GpcFreePolygon(ref gpcPolygon);

        return polygon;
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
        gpcPol.hole = Marshal.AllocCoTaskMem(polygon.NofContours * Marshal.SizeOf(hole[0]));

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
            for (int j = 0; j < polygon.Contours[i].NofVertices; j++)
            {
                var uv = contour.Vertices[j];
                var gpcVtx = new GpcVertex
                {
                    x = uv.U,
                    y = uv.V
                };
                Marshal.StructureToPtr(gpcVtx, ptr2, false);
                ptr2 = checked((int)ptr2 + Marshal.SizeOf(gpcVtx)); // Checked conversion added
            }
            Marshal.StructureToPtr(gpcVtxList, ptr, false);
            ptr = checked((int)ptr + Marshal.SizeOf(gpcVtxList)); // Checked conversion added
        }

        return gpcPol;
    }

    private static Polygon Gpc_polygon_ToPolygon(GpcPolygon gpcPolygon)
    {
        var contours = new VertexList[gpcPolygon.num_contours];
        int[] holeInt = new int[gpcPolygon.num_contours];

        if (gpcPolygon.num_contours > 0)
            Marshal.Copy(gpcPolygon.hole, holeInt, 0, gpcPolygon.num_contours);

        IntPtr ptr = gpcPolygon.contour;
        for (int i = 0; i < gpcPolygon.num_contours; i++)
        {
            var gpcVtxList = (GpcVertexList)Marshal.PtrToStructure(ptr, typeof(GpcVertexList));
            var vertices = new UV[gpcVtxList.num_vertices];
            IntPtr ptr2 = gpcVtxList.vertex;
            for (int j = 0; j < gpcVtxList.num_vertices; j++)
            {
                var gpcVtx = (GpcVertex)Marshal.PtrToStructure(ptr2, typeof(GpcVertex));
                vertices[j] = new UV(gpcVtx.x, gpcVtx.y);
                ptr2 = checked((IntPtr)((long)ptr2 + Marshal.SizeOf(gpcVtx))); // Checked conversion added
            }
            ptr = checked((IntPtr)((long)ptr + Marshal.SizeOf(gpcVtxList))); // Checked conversion added
            contours[i] = new VertexList(holeInt[i] != 0, vertices);
        }

        return new Polygon(contours);
    }

    private static void Free_gpc_polygon(GpcPolygon gpcPol)
    {
        Marshal.FreeCoTaskMem(gpcPol.hole);
        IntPtr ptr = gpcPol.contour;
        for (int i = 0; i < gpcPol.num_contours; i++)
        {
            var gpcVtxList = (GpcVertexList)Marshal.PtrToStructure(ptr, typeof(GpcVertexList));
            Marshal.FreeCoTaskMem(gpcVtxList.vertex);
            ptr = checked((IntPtr)((long)ptr + Marshal.SizeOf(gpcVtxList))); // Checked conversion added
        }
        Marshal.FreeCoTaskMem(gpcPol.contour);
    }

    [LibraryImport("gpc.dll", EntryPoint = "gpc_polygon_clip", StringMarshalling = StringMarshalling.Custom)]
    private static extern void GpcPolygonClip(GpcOperation setOperation, ref GpcPolygon subjectPolygon, ref GpcPolygon clipPolygon, ref GpcPolygon resultPolygon);

    [LibraryImport("gpc.dll", EntryPoint = "gpc_free_polygon", StringMarshalling = StringMarshalling.Custom)]
    private static extern void GpcFreePolygon(ref GpcPolygon polygon);

    [StructLayout(LayoutKind.Sequential)]
    private struct GpcVertex
    {
        public double x;
        public double y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GpcVertexList
    {
        public int num_vertices;
        public IntPtr vertex;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GpcPolygon
    {
        public int num_contours;
        public IntPtr hole;
        public IntPtr contour;
    }
}