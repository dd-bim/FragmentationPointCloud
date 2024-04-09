import logging

from OCC.Core import BRepTools, BRep, GeomAbs, Bnd, BRepBndLib, gp
from OCC.Core.BRepAdaptor import BRepAdaptor_Surface
from OCC.Extend import TopologyUtils


class patchInfo:
    def __init__(self, StateId, ObjectGuid, FaceId, Normal, Position, BBoxMin, BBoxMax, Polygon):
        self.StateId = StateId
        self.ObjectGuid = ObjectGuid
        self.FaceId = FaceId
        self.Normal = Normal
        self.Position = Position
        self.BBoxMin = BBoxMin
        self.BBoxMax = BBoxMax
        self.Polygon = Polygon

    def toString(self):
        return "{};{};{};{};{};{};{};{}".format(self.StateId, self.ObjectGuid, self.FaceId, self.Normal, self.Position, self.BBoxMin, self.BBoxMax, self.Polygon)

class BIMFace:
    def __init__(self, StateID, ObjectGuid, FaceID, Polygon):
        self.StateID = StateID
        self.ObjectGuid = ObjectGuid
        self.FaceID = FaceID
        self.Polygon = Polygon

    def toCSVString(self):
        return "{};{};{};{}".format(self.StateID, self.ObjectGuid, self.FaceID, self.Polygon)


def getWirePointListFromFace(face):
    outerWire = BRepTools.breptools_OuterWire(face)

    outerWirePointList = []
    innerWirePointList = []
    for wire in TopologyUtils.TopologyExplorer(face).wires():
        if wire.IsEqual(outerWire):
            for vertex in TopologyUtils.WireExplorer(wire).ordered_vertices():
                outerWirePointList.append(BRep.BRep_Tool.Pnt(vertex))

        else:
            innerLoop = []
            for vertex in TopologyUtils.WireExplorer(wire).ordered_vertices():
                innerLoop.append(BRep.BRep_Tool.Pnt(vertex))
            innerWirePointList.append(innerLoop)

    return [outerWirePointList, innerWirePointList]


def buildWKTPolyFromPtList(ptList):
    outerWirePtList = ptList[0]

    ### outer Wire ###
    outerCoordList = ["{} {} {}".format(pt.X(), pt.Y(), pt.Z()) for pt in outerWirePtList]
    outerCoordList.append("{} {} {}".format(outerWirePtList[0].X(), outerWirePtList[0].Y(), outerWirePtList[0].Z()))
    completeListOuterWire = '(' + ', '.join(outerCoordList) + ')'

    ### inner Wire ###
    allInnerWiresCoordList = []
    if len(ptList[1]) > 0:
        for innerWire in ptList[1]:
            currentInnerWireList = ["{} {} {}".format(pt.X(), pt.Y(), pt.Z()) for pt in innerWire]
            currentInnerWireList.append("{} {} {}".format(innerWire[0].X(), innerWire[0].Y(), innerWire[0].Z()))
            completeInnerWire = "(" + ", ".join(currentInnerWireList) + ')'
            allInnerWiresCoordList.append(completeInnerWire)

    ### buildWKT ###
    if len(allInnerWiresCoordList) > 0:
        WKTRep = "POLYGON Z(" + completeListOuterWire + "," + ",".join(allInnerWiresCoordList) + ")"
    else:
        WKTRep = "POLYGON Z({})".format(completeListOuterWire)

    return WKTRep


def getPatchInfoFromFace(face, entity, faceId, stateId):
    try:
        ptList = getWirePointListFromFace(face)
        wkt = buildWKTPolyFromPtList(ptList)
        box = getBBoxForFace(face)
        surf = BRepAdaptor_Surface(face, True)
        if surf.GetType() == GeomAbs.GeomAbs_Plane:
            pln = surf.Plane()
            location = pln.Location()
            normal = pln.Axis().Direction()

            pf = patchInfo(stateId, entity.GlobalId, faceId,
                                "{} {} {}".format(normal.X(), normal.Y(), normal.Z()),
                                "POINT({} {} {})".format(location.X(), location.Y(), location.Z()),
                                box[0], box[1], wkt)
            return pf

        else:
            logging.warning('Face is not planar. Such faces are not implemented yet. \nEntitity is {}'.format(entity))

    except Exception as ex:
        print(ex)
        logging.exception('Exception occured')

def getBIMFaceInfo(face,entity, stateId, faceId):
    try:
        surf = BRepAdaptor_Surface(face)
        if surf.GetType() == GeomAbs.GeomAbs_Plane:
            ptList = getWirePointListFromFace(face)
            wktPoly = buildWKTPolyFromPtList(ptList)

            return BIMFace(stateId, entity.GlobalId, faceId, wktPoly)

        else:
            logging.warning('Face is not planar. Such faces are not implemented yet. \nEntitity is {}'.format(entity))
            #return None
    except Exception as ex:
        print(ex)
        logging.exception(ex)

def getBBoxForFace(face):
    box = Bnd.Bnd_Box()
    BRepBndLib.brepbndlib_Add(face, box)
    xmin, ymin, zmin, xmax, ymax, zmax = box.Get()
    return ("POINT({} {} {})".format(xmin, ymin, zmin), "POINT({} {} {})".format(xmax, ymax, zmax))

def getBBoxPointsForFace(face):
    box = Bnd.Bnd_Box()
    BRepBndLib.brepbndlib_Add(face, box)
    xmin, ymin, zmin, xmax, ymax, zmax = box.Get()
    return (gp.gp_Pnt(xmin, ymin, zmin), gp.gp_Pnt(xmax, ymax, zmax))

def box_to_string(box_geom, guid, stateID=-999):
    xmin, ymin, zmin, xmax, ymax, zmax = box_geom.Get()
    return(f"{stateID};{guid};{xmin};{ymin};{zmin};{xmax};{ymax};{zmax}")