import argparse
import itertools
import json
import os
import subprocess
import sys

import ifcopenshell
from OCC.Core import gp, BRepAlgoAPI
from OCC.Extend import TopologyUtils
from ifcopenshell import geom

import contourHelper
from common import ifcUtils

parser = argparse.ArgumentParser(description='Extract Building Contour from Ifc-File')
parser.add_argument('-i', required=True, help='the input Ifc-File')
parser.add_argument('-o', required=True, help='the path to the output directory')
parser.add_argument('-s', required=True, help='Nr of storey for which section is made')
parser.add_argument('-height', default=1.0, help='height offset where section is made based from elevation of storey')
parser.add_argument('-entityList', help='List of IfcProducts that should be processed')

args = parser.parse_args()

ifc_file = ifcopenshell.open(args.i)
settings = geom.settings()
settings.set(settings.USE_PYTHON_OPENCASCADE, True)

lengthUnit = ifcUtils.getLengthUnit(ifc_file)
heightFactor = 1
if lengthUnit == 'millimeter':
    heightFactor = 1000

storeys = ifc_file.by_type('IfcBuildingStorey')
elementsInStorey = ifcUtils.getProductsForBuildingStorey(storeys[int(args.s)])

if args.entityList:
    with open(args.entityList) as json_file:
        entityList = json.load(json_file)

    if len(entityList['includeList']) > 0:
        entitiesToInclude = []
        for elemType in entityList['includeList']:
            entitiesToInclude.append(ifc_file.by_type(elemType))
        entitiesToInclude = list(itertools.chain.from_iterable(entitiesToInclude))

        elementsInStorey = [x for x in elementsInStorey if x in entitiesToInclude]

    if len(entityList['excludeList']) > 0:
        products = ifc_file.by_type('IfcProduct')
        entitiesToExclude = []
        for elemType in entityList['excludeList']:
            entitiesToExclude.append(ifc_file.by_type(elemType))
        entitiesToExclude = list(itertools.chain.from_iterable(entitiesToExclude))

        elementsInStorey = [x for x in elementsInStorey if x not in entitiesToExclude]

shapes = []
for elem in elementsInStorey:
    try:
        shapes.append(geom.create_shape(settings, elem).geometry)
    except:
        pass

storyElevation = storeys[int(args.s)].Elevation
if storyElevation is not None:
    section_height = (storyElevation / heightFactor) + float(args.height)
else:
    section_height =ifcUtils.getMediumHeightFromShapes(shapes)

section_plane = gp.gp_Pln(gp.gp_Pnt(0, 0, section_height), gp.gp_Dir(0, 0, 1))

all_section_edges = []
for idx, shape in enumerate(shapes):
    try:
        section = BRepAlgoAPI.BRepAlgoAPI_Section(shape, section_plane).Shape()
        all_section_edges.append(list(TopologyUtils.TopologyExplorer(section).edges()))
    except:
        pass

edges = list(itertools.chain.from_iterable(all_section_edges))

if len(edges) < 1:
    print('No edges at section! (maybe use other section height?)')
    sys.exit()

points = contourHelper.getPointListFromEdgeList(edges)

sectionSegments = [contourHelper.createSegment2dFromOCCEdge(x) for x in edges]

with open('./bin/input.txt', 'w') as f:
    for seg in sectionSegments:
        f.write("{} {} {} {}\n".format(seg[0], seg[1], seg[2], seg[3]))

subprocess.run(["./bin/IfcGeometryExtractor.exe", "./bin/input.txt", "./bin/output.txt"])

contourPoints = []
with open('./bin/output.txt', 'r') as file:
    for line in file:
        split = line.split(' ')
        contourPoints.append(gp.gp_Pnt(float(split[0]), float(split[1]), section_height))

wire = contourHelper.buildWireFromPointList(contourPoints)
shapes.append(wire)

out_file_name = os.path.join(os.path.abspath(args.o), "contour_wkt.txt")
contourHelper.writeWKTStringToFile(wire, out_file_name)

#from common import visualisation
#visualisation.quickViewFromShapeList(wire)

#from OCC.Core import BRepBuilderAPI
#face = BRepBuilderAPI.BRepBuilderAPI_MakeFace(wire).Face()