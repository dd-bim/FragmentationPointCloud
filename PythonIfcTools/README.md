# PythonIfcTools

## Extract planar faces with parameters and BBoxes from IFC-File

optional arguments:
  -h, --help            show this help message and exit
  -i I                  the input Ifc-File
  -faces                If flag is set, face information are calculated and stored in file -faceFile
  -faceFile FACEFILE    The path to the face info output csv file
  -boxes                If flag is set, bounding boxes for products will be calcualted and stord in file -boxFile
  -boxFile BOXFILE      The path to the box info output csv file
  -boxBuffer BOXBUFFER  Size of the buffer arround extracted bounding boxes in meter
  -entityList ENTITYLIST
                        JSON file of IfcProducts that should be processed or excluded
  -buildingCS           Generate Patches in Coordinate System of Building NOT Site
  -stateID STATEID      the state / phase id for the analyzed IFC file

## Todo
- write documentation and how to use on computer without conda env
- Re-generated Environment.yml before shipping

## Requirements
In order to use the developed tools, a corresponding Python runtime environment must 
be available on the computer.

The tools were developed with Python version 3.11. The following libraries are also used:
- IfcOpenShell
- PythonOCC
- Numpy
- PyQT5 (nur für Visualisierung notwendig)

It is recommended to create your own virtual Python environment using Miniconda. 
Miniconda can be downloaded and installed [here](https://docs.conda.io/en/latest/miniconda.html). After Miniconda 
has been successfully 
installed, the required Python environment can be restored using the supplied 
Environment.yml file. 

To do this, the Anaconda prompt supplied with Miniconda must first be started and the following command executed:

```
conda env create -f environment.yml
```
This creates a new virtual environment and installs all necessary packages in the standard Miniconda path. 
If a different installation location is required, this can be specified using the `-p` parameter:

```
conda env create -f environment.yml -p D:\dev\envs\env_name
```

