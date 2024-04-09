import argparse
import itertools
import json
import logging
import time

import ifcopenshell
from OCC.Extend import TopologyUtils
from ifcopenshell import geom

import wktExtraction
from common import transformation

for handler in logging.root.handlers[:]:
    logging.root.removeHandler(handler)

#Todo: write log to output Directory!
logging.basicConfig(filename='faceExtractorLog.log', filemode='w', format='%(asctime)s - %(name)s - %(levelname)s - %(message)s', level=logging.INFO)
totalStart = time.time()

parser = argparse.ArgumentParser(description='Extract planar faces and parameters from IFC-File')
parser.add_argument('-i', required=True, help='the input Ifc-File')
parser.add_argument('-o', required=True, help='the output csv-File')
parser.add_argument('-entityList', help='List of IfcProducts that should be processed')
parser.add_argument('-buildingCS', action="store_true", help='Generate Patches in Coordinate System of Building NOT Site')

args = parser.parse_args()

ifc_file = ifcopenshell.open(args.i)
settings = geom.settings()
settings.set(settings.USE_PYTHON_OPENCASCADE, True)

products = []
if args.entityList:
    with open(args.entityList) as json_file:
        entityList = json.load(json_file)

    #JSON enthält Entitäten, die selektiert werden sollen
    if len(entityList['includeList']) > 0 and len(entityList['excludeList'])== 0:
        for entType in entityList['includeList']:
            try:
                selectedElems = ifc_file.by_type(entType)
                logging.info('{} instances of {} have been found in the Ifc-File'.format(len(selectedElems), entType))
                products.append(selectedElems)
            except Exception as ex:
                logging.warning('specified entity type {} was not found in selectd IFC-File'.format(entType))
                print('specified entity type {} was not found in selectd IFC-File'.format(entType))
                print(ex)
                logging.exception("Exception occured")
        # chain entities in one flat list
        products = list(itertools.chain.from_iterable(products))

    #JSON enthält Entitäten die ausgeschlossen werden sollen
    elif len(entityList['excludeList']) > 0:
        products = ifc_file.by_type('IfcProduct')
        entitiesToExclude = []
        for elemType in entityList['excludeList']:
            entitiesToExclude.append(ifc_file.by_type(elemType))
        entitiesToExclude = list(itertools.chain.from_iterable(entitiesToExclude))
        products = [x for x in products if x not in entitiesToExclude]

#keine JSON spezifiziert
else:
    products = ifc_file.by_type('IfcProduct')

nrOfProducts = len(products)
failureCounter = 0

shapeCreateStart = time.time()
shapes = []
for product in products:
    try:
        if product.Representation is not None:
            shapes.append([geom.create_shape(settings, product).geometry, product])
    except Exception as ex:
        failureCounter +=1
        print(product)
        print(ex)

if args.buildingCS:
    building = ifc_file.by_type('IfcBuilding')[0]
    buildingTrsfMatrix = transformation.getCombinedAxis2Plc(building).getTrsfMatrix().Inverted()

    shapesToTransform = [x[0] for x in shapes]
    ifcEntities = [x[1] for x in shapes]
    transformedShapes = transformation.transformListOfOCCShape(shapesToTransform, buildingTrsfMatrix)

    #zip returns list of tuples; map calls list on each tuple of the list
    shapes = list(map(list, zip(transformedShapes, ifcEntities)))


shapeCreateEnd = time.time()

print('shape creation finished after {} seconds'.format(shapeCreateEnd-shapeCreateStart))
logging.info('shape creation finished after {} seconds'.format(shapeCreateEnd-shapeCreateStart))
logging.info('a total of {} shapes were created'.format(len(shapes)))
#print('Could not create shape for {} of {} entities without representation'.format(failureCounter, nrOfProducts))

# Todo: implement usefull stateId
stateId = "12345"

allPatchInfo = []
patchInfoStart = time.time()

for shape in shapes:
    try:
        faceId = 0
        for face in TopologyUtils.TopologyExplorer(shape[0]).faces():
            allPatchInfo.append(wktExtraction.getPatchInfoFromFace(face, shape[1], faceId, stateId))
            faceId += 1
    except Exception as ex:
        logging.error('{}\n'.format(ex))


patchInfoEnd = time.time()
print('patch info extraction finished after {} seconds'.format(patchInfoEnd-patchInfoStart))
logging.info('patch info extraction finished after {} seconds'.format(patchInfoEnd-patchInfoStart))

with open(args.o, 'w') as f:
    f.write("StateId;ObjectGuid;FaceId;Normal;Position;BBoxMin;BBoxMax;Polygon\n")
    f.write("\n".join([x.toString() for x in allPatchInfo if x is not None]))

totalEnd = time.time()
print('whole process took {} seconds'.format(totalEnd-totalStart))
print('see log file for additional info')
logging.info('whole process took {} seconds'.format(totalEnd-totalStart))