import argparse
import json
import logging
import multiprocessing
import os
import sys
import time
import traceback

import OCC.Core.Bnd
import ifcopenshell
from OCC.Core import BRepBndLib, BRepBuilderAPI
from OCC.Extend import TopologyUtils
from ifcopenshell import geom

from common import transformation
from faceExtraction import wktExtraction

for handler in logging.root.handlers[:]:
    logging.root.removeHandler(handler)

totalStart = time.time()

parser = argparse.ArgumentParser(description='Extract planar faces with parameters and BBoxes from IFC-File')
parser.add_argument('-i', required=True, help='the input Ifc-File')
parser.add_argument('-faces', action='store_true', help='If flag is set, face information are calculated and stored in file -faceFile')
parser.add_argument('-faceFile', default='./faceInfo.csv', help='The path to the face info output csv file')
parser.add_argument('-boxes', action='store_true', help='If flag is set, bounding boxes for products will be calcualted and stord in file -boxFile')
parser.add_argument('-boxFile', default='./boxInfo.csv', help='The path to the box info output csv file')
parser.add_argument('-boxBuffer', help="Size of the buffer arround extracted bounding boxes in meter", default=0.0, type=float)
parser.add_argument('-entityList', help='JSON file of IfcProducts that should be processed or excluded')
parser.add_argument('-buildingCS', action="store_true", help='Generate Patches in Coordinate System of Building NOT Site')
parser.add_argument('-stateID', help="the state / phase id for the analyzed IFC file", default=-999, type=int)


args = parser.parse_args()
ifc_path = os.path.abspath(args.i)

calc_faces = args.faces
calc_boxes = args.boxes
enlarge_boxes = True if args.boxBuffer != 0.0 else False

if calc_faces and calc_boxes == False:
    logging.error("No output type specified. Terminating process")
    sys.exit("No output type specified. Terminating process")


outFileFolder = os.path.dirname(ifc_path)
logFilePath = os.path.join(outFileFolder, 'IFCFaceExtractorLog.log')

logging.basicConfig(filename=logFilePath, filemode='w', format='%(asctime)s - %(name)s - %(levelname)s - %(message)s', level=logging.INFO)

logging.info('starting IFCExtractor for file ' + ifc_path)
print('starting IFCExtractor for file ' + ifc_path)

ifc_file = ifcopenshell.open(ifc_path)
logging.info('finished opening IFC file')
print('finished opening IFC file')

includingEntities, excludingEntities = None, None
products = []
if args.entityList:
    try:
        with open(args.entityList) as json_file:
            entityList = json.load(json_file)

        includingEntities = entityList['includeList']
        excludingEntities = entityList['excludeList']
        if len(includingEntities) > 0 and len(excludingEntities) == 0:
            includingEntities = includingEntities
            excludingEntities = None
        elif len(excludingEntities) > 0 and len(includingEntities) == 0:
            excludingEntities = excludingEntities
            includingEntities = None
        elif len(includingEntities) > 0 and len(excludingEntities) > 0:
            logging.error('Can not specify including and excluding entites simultaneously. Stopping process')
            sys.exit('Can not specify including and excluding entites simultaneously. Stopping process')

    except Exception as ex:
        traceback.print_exc()
        logging.exception("Exception occured")
        sys.exit()

#keine JSON spezifiziert
else:
    products = ifc_file.by_type('IfcProduct')
    logging.info(f'No selection restrictions are given. Proceeding for all {len(products)} IfcProducts')
    print(f'No selection restrictions are given. Proceeding for all {len(products)} IfcProducts')


shapes_iterator = []
product_geom_dict = {}

iterator_settings = ifcopenshell.geom.settings()
iterator_settings.set(iterator_settings.USE_PYTHON_OPENCASCADE, True)
iterator_settings.set(iterator_settings.DISABLE_TRIANGULATION, False)
iterator_settings.set(iterator_settings.USE_BREP_DATA, True)

iterator_start = time.time()

iterator = ifcopenshell.geom.iterator(iterator_settings, ifc_file, multiprocessing.cpu_count(),
                                      include=includingEntities, exclude=excludingEntities)

if iterator.initialize():
    logging.info('Starting geometry creation with iterator')
    print('Starting geometry creation with iterator')
    while True:
        shape_tuple = iterator.get()
        product_geom_dict[shape_tuple.data.guid] = [ifc_file.by_guid(shape_tuple.data.guid), shape_tuple.geometry]
        if not iterator.next():
            break

iterator_end = time.time()

logging.info(f"Iterator took {iterator_end-iterator_start} seconds for geometry creation")
print(f"Iterator took {iterator_end-iterator_start} seconds for geometry creation")


if args.buildingCS:
    logging.info("Starting to transform geometries into building coordinate system")
    print("Starting to transform geometries into building coordinate system")

    building = ifc_file.by_type('IfcBuilding')[0]
    buildingTrsfMatrix = transformation.getCombinedAxis2Plc(building).getTrsfMatrix().Inverted()
    brepTransformator = BRepBuilderAPI.BRepBuilderAPI_Transform(buildingTrsfMatrix)

    transformed_shape_dict = {}
    for guid, shape in product_geom_dict.values():
        brepTransformator.Perform(shape)
        transformed_shape_dict[guid] = [ifc_file.by_guid(guid), brepTransformator.Shape()]

    product_geom_dict = transformed_shape_dict

logging.info("Starting with extraction of geometric properties for products")
print("Starting with extraction of geometric properties for products")

allBIMFaces = []
allBoxes = {}
allOBBoxes = {}
for product_geom_list in product_geom_dict.values():
    try:
        if calc_faces:
            faceId = 0
            for face in TopologyUtils.TopologyExplorer(product_geom_list[1]).faces():
                allBIMFaces.append(wktExtraction.getBIMFaceInfo(face, product_geom_list[0], args.stateID, faceId))
                faceId += 1
        if calc_boxes:
            bbox = OCC.Core.Bnd.Bnd_Box()
            BRepBndLib.brepbndlib.Add(product_geom_list[1], bbox)
            if enlarge_boxes:
                bbox.Enlarge(args.boxBuffer)
            allBoxes[product_geom_list[0].GlobalId] = bbox

            obbox = OCC.Core.Bnd.Bnd_OBB()
            BRepBndLib.brepbndlib.AddOBB(product_geom_list[1], obbox, True, True, True)
            if enlarge_boxes:
                obbox.Enlarge(args.boxBuffer)
            allOBBoxes[product_geom_list[0].GlobalId] = obbox

    except Exception as ex:
        logging.error('{}\n'.format(ex))
        print(ex)

if calc_faces and args.boxBuffer != 0.0:
    logging.info(f"Enlarged boxes by {args.boxBuffer} meters")
    print(f"Enlarged box by {args.boxBuffer} meters")

logging.info("Finished with extraction of geometric properties for products")
print("Finished with extraction of geometric properties for products")


logging.info("Writing results to file")
print("Writing results to file")

if calc_faces:
    with open(args.faceFile, 'w') as f:#, open(args.boxFile, 'w') as box_file:
        f.write("StateId;ObjectGuid;FaceId;Polygon\n")
        f.write("\n".join([x.toCSVString() for x in allBIMFaces if x is not None]))

if calc_boxes:
    with open(args.boxFile, 'w') as box_file:
        box_file.write("Oriented;StateId;ObjectGuid;Element;BBoxMinX;BBoxMinY;BBoxMinZ;BBoxMaxX;BBoxMaxY;BBoxMaxZ;OBoxCenterX;OBoxCenterY;OBoxCenterZ;" +
                       "OBoxXDirX;OBoxXDirY;OBoxXDirZ;OBoxYDirX;OBoxYDirY;OBoxYDirZ;OBoxZDirX;OBoxZDirY;OBoxZDirZ;" +
                       "OBoxXHSize;OBoxYHSize;OBoxZHSize\n")

        for guid, box in allBoxes.items():
            xmin, ymin, zmin, xmax, ymax, zmax = box.Get()

            center_obox = allOBBoxes[guid].Center()
            xDir = allOBBoxes[guid].XDirection()
            yDir = allOBBoxes[guid].YDirection()
            zDir = allOBBoxes[guid].ZDirection()

            xSize = allOBBoxes[guid].XHSize()
            ySize = allOBBoxes[guid].YHSize()
            zSize = allOBBoxes[guid].ZHSize()

            box_file.write(f"True;{args.stateID};{guid};0;{xmin};{ymin};{zmin};{xmax};{ymax};{zmax};{center_obox.X()};{center_obox.Y()};{center_obox.Z()};" +
                           f"{xDir.X()};{xDir.Y()};{xDir.Z()};{yDir.X()};{yDir.Y()};{yDir.Z()};{zDir.X()};{zDir.Y()};{zDir.Z()};" +
                           f"{xSize};{ySize};{zSize}\n")

        #box_file.write("\n".join([wktExtraction.box_to_string(value, key, args.stateID) for key, value in allBoxes.items()]))

logging.info("Finished Program")
print("Finished Program")