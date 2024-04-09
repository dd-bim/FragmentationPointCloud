import itertools

from OCC.Core import Bnd, BRepBndLib, BRep, BRepBuilderAPI
from OCC.Extend import TopologyUtils, ShapeFactory


def vertex2pnt(vertex):
    return BRep.BRep_Tool.Pnt(vertex)

def getProductsForBuildingStorey(IfcBuildingStorey):
    returnList = []
    for item in IfcBuildingStorey.ContainsElements:
        returnList.append(item.RelatedElements)

    return list(itertools.chain(*returnList))


def getMediumHeightFromShapes(shapes):
    obb = Bnd.Bnd_OBB()
    for shape in shapes:
        BRepBndLib.brepbndlib_AddOBB(shape, obb)
    return obb.Center().Z()


def getPointListFromEdgeList(edges):

    pointList = []

    for edge in edges:
        vertices = list(TopologyUtils.TopologyExplorer(edge).vertices())
        pointList.append([vertex2pnt(x) for x in vertices])

    pointList = itertools.chain.from_iterable(pointList)
    pointSet = set(pointList)
    return list(pointSet)


def createSegment2dFromOCCEdge(edge):
    vertIter = TopologyUtils.TopologyExplorer(edge).vertices()
    start = vertex2pnt(next(vertIter)).Coord()[0:2]
    end = vertex2pnt(next(vertIter)).Coord()[0:2]

    return [start[0], start[1], end[0], end[1]]


def buildWireFromPointList(ptList):
    nrOfPoints = len(ptList)

    edgeList = []
    for idx, point in enumerate(ptList):
        if idx < nrOfPoints-1:
            if not ptList[idx].IsEqual(ptList[idx+1], 0.0001):
                edgeList.append(BRepBuilderAPI.BRepBuilderAPI_MakeEdge(ptList[idx], ptList[idx+1]).Edge())
        else:
            if not ptList[idx].IsEqual(ptList[0], 0.0001):
                edgeList.append(BRepBuilderAPI.BRepBuilderAPI_MakeEdge(ptList[idx], ptList[0]).Edge())

    wire = ShapeFactory.make_wire(edgeList)
    return wire