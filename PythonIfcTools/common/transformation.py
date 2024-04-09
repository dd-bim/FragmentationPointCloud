from OCC.Core import gp, BRepBuilderAPI

from . import calculations


class Axis2Placement3D:

    def __init__(self, location, refDirection, axis):
        self.Location = location
        self.RefDirection = refDirection
        self.Axis = axis
        self.YAxis = calculations.crossFromAxis(axis, refDirection)

    def getTrsfMatrix(self):
        trsf = gp.gp_Trsf()
        a11 = self.RefDirection[0]
        a21 = self.RefDirection[1]
        a31 = self.RefDirection[2]

        a12 = self.YAxis[0]
        a22 = self.YAxis[1]
        a32 = self.YAxis[2]

        a13 = self.Axis[0]
        a23 = self.Axis[1]
        a33 = self.Axis[2]

        a14 = self.Location[0]
        a24 = self.Location[1]
        a34 = self.Location[2]

        trsf.SetValues(a11, a12, a13, a14, a21, a22, a23, a24, a31, a32, a33, a34)
        return trsf

def combine(axis2PlcList):
    q = calculations.Quaternion(axis2PlcList[0].RefDirection, axis2PlcList[0].YAxis, axis2PlcList[0].Axis)
    t = axis2PlcList[0].Location

    for i in range(1, len(axis2PlcList)):
        qi = calculations.Quaternion(axis2PlcList[i].RefDirection, axis2PlcList[i].YAxis, axis2PlcList[i].Axis)
        q = q.multiplyWithQuat(qi)
        tdiff = qi.multiplyWithVector(t)

        tx = axis2PlcList[i].Location[0] + tdiff[0]
        ty = axis2PlcList[i].Location[1] + tdiff[1]
        tz = axis2PlcList[i].Location[2] + tdiff[2]

        t = (tx, ty, tz)

    newAxis2Plc = Axis2Placement3D(t, q.getXAxis(), q.getZAxis())

    return newAxis2Plc

def getAllAxis2PlcForEntity(ifcEntity):
    plcList = getAllRelPlacementsForEntity(ifcEntity)

    returnList = []

    for entry in plcList:

        if entry == None:
            continue

        relPlc = entry.RelativePlacement

        axis = (0, 0, 1) if relPlc.Axis == None else relPlc.Axis.DirectionRatios
        refDir = (1, 0, 0) if relPlc.RefDirection == None else relPlc.RefDirection.DirectionRatios
        loc = relPlc.Location.Coordinates

        a2p = Axis2Placement3D(loc, refDir, axis)
        returnList.append(a2p)

    return returnList


def getCombinedAxis2Plc(ifcEntity):
    a2pList = getAllAxis2PlcForEntity(ifcEntity)

    return combine(a2pList)

def getAllRelPlacements(ifcEntity, allPlaceList):
    if hasattr(ifcEntity, "PlacementRelTo"):
        allPlaceList.append(ifcEntity.PlacementRelTo)
        getAllRelPlacements(ifcEntity.PlacementRelTo, allPlaceList)


def getAllRelPlacementsForEntity(ifcEntity):
    placeList = []
    placeList.append(ifcEntity.ObjectPlacement)
    getAllRelPlacements(ifcEntity.ObjectPlacement, placeList)
    return placeList

###########################################
### Stop entity
###########################################
def getCombinedAxis2PlcUpToEntity(ifcentity, stopEntity):
    relPlcList = getAllRelPlacementsForEntityUpToEntity(ifcentity, stopEntity)
    a2pList = [localPlacementIfc2Axis2Placement(x) for x in relPlcList]
    return combine(a2pList)

def getAllRelPlacementsForEntityUpToEntity(ifcEntity, stopEntity):
    placeList = []
    placeList.append(ifcEntity.ObjectPlacement)
    getAllRelPlacementsUpToEntity(ifcEntity.ObjectPlacement, placeList, stopEntity)
    return placeList

def getAllRelPlacementsUpToEntity(ifcEntity, allPlaceList, stopEntity):
    if hasattr(ifcEntity, "PlacementRelTo") and not ifcEntity.PlacementRelTo.PlacesObject[0].is_a(stopEntity):
        allPlaceList.append(ifcEntity.PlacementRelTo)
        getAllRelPlacementsUpToEntity(ifcEntity.PlacementRelTo, allPlaceList,stopEntity)

def localPlacementIfc2Axis2Placement(ifcLocalPlacement):
    relPlc = ifcLocalPlacement.RelativePlacement

    axis = (0, 0, 1) if relPlc.Axis == None else relPlc.Axis.DirectionRatios
    refDir = (1, 0, 0) if relPlc.RefDirection == None else relPlc.RefDirection.DirectionRatios
    loc = relPlc.Location.Coordinates

    return Axis2Placement3D(loc, refDir, axis)

def transformListOfOCCShape(shapes, trsfMatrix):
    transformedShapes = []
    brepTransformator = BRepBuilderAPI.BRepBuilderAPI_Transform(trsfMatrix)

    for shape in shapes:
        brepTransformator.Perform(shape)
        transformedShapes.append(brepTransformator.Shape())

    return transformedShapes