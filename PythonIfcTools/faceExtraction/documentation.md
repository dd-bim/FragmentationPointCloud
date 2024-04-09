# IfcFaceExtractor

## Purpose
This little tool helps to extract planar faces from IFC building models. 

## Usage
The general usage of the tool is

`IFCFaceExtractor.exe -i <inputFile> -o <outputFile>`

where `-i` is the path to the input IFC-File and `-o` is the path to the
output CSV-File. 

Additonal options are added to the end of the command:
- `-entityList myJSON.json` JSON-File containing the IFC-Objects to process or to ignore
- `-stateID 123` ID specifying the processed construction phase / construction state (default is -999)
- `-buildingCS` If this flag is set, the faces are exported in the building coordinate system rather than in the IFCSite coordinate system

## Filtering IFC-Objects for Processing
By default all `IfcProducts` with a `Representation` are processed. Use the `entityJson.json`-file to restrict the processed objects
The following example file only processes Walls and Doors:

`{
	"includeList": [
		"IfcWall",
        "IfcDoor"
	],
	"excludeList": []
}`

To process all `IfcProducts` except a Walls and Doors modify the file to:

`{
	"includeList": [
	],
	"excludeList": [
        "IfcWall",
        "IfcDoor"
    ]
}`

It is not possible to use both entities in the include and exclude list!

## Build Instructions
PyInstaller is used to build a stand-alone windows executable. For building the exe go the 
faceExtraction directory and use the following command:

`pyinstaller -F --distpath ./faceExtraction/dist/ faceExtraction/IFCFaceExtractor.py`


# IFCFaceBoxExtractor
## Purpose
This little tool helps to extract planar faces and the bounding boxes of the IfcProdcuts
from IFC building models. 

## Usage
The general usage of the tool is

`IFCFaceExtractor.exe -i <inputFile> `

where `-i` is the path to the input IFC-File.

The process can be parametrized using the following options:
 - `-faces` use this flag to extract the face information
 - `-faceFile` the default location for storing face information is `./faceInfo.csv`. Use 
   this flag to specifiy another output file location
 - `-boxes` use this flag to extract bounding box information and 
orientend bounding box information for the `IfcProducts`
 - `-boxFile` the default location for storing face information is `./boxInfo.csv`. Use 
   this flag to specifiy another output file location
 - `-boxBuffer` use this flag to enlarge the tight boundig box of the `IfcProduct`. The buffer
   has to be specified in meters.
 - `-entityList` use this flag to restrict the set of processed products. See above for
	more information
 - `-buildingCS` use this flag to transform the box and faces to the coordinate system of
	the building and NOT site
 - `-stateID` accepts integer input to specify the ID of the construction phase. Default is
	`-999`