# FragmentationPointCloud
Revit plugin to split large point clouds into small component point clouds using BIM components.
![grafik](https://github.com/user-attachments/assets/aa9aa6a7-8fa4-4f51-9a1a-7ece803e2417)

## SectionBox
The 3D section area in Revit (internally referred to as SectionBox) can be rotated around the Z-axis and cut to size. The internally saved BoundingBox does not change when rotating because this rotation is saved via a separate transformation. If the SectionBox is not rotated, the unit matrix is present. If the PBP is rotated, the transformation on the SectionBox does not change.
One corner is visually marked with a rotation arrow. This corner represents the maximum upper point (SectionBox.Max) and the opposite corner at the bottom is the minimum point of the SectionBox (SectionBox.Min).
The centre, the angle of rotation and the extent of the SectionBox are required for the oriented bounding box: 
*Calculation of the centre via vector calculation with the two extreme points  
*Calculation of the angle of rotation using the rotation matrix
*Calculation of the dimensions by back transformation of the SectionBox and difference formation of X, Y and Z coordinate values



![grafik](https://github.com/user-attachments/assets/366b974e-8304-4f66-bc08-d91eb2dc6054)
