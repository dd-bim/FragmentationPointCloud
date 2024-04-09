import json

class entityList:

    def __init__(self):
        data = {}
        data['includeList'] = []
        data['excludeList'] = []
        self.data = data

    def toFile(self, filePath):
        with open(filePath, 'w') as outfile:
            json.dump(self.data, outfile)


class entityListProgressPatch(entityList):

    def __init__(self):
        super().__init__()
        self.data['includeList'] = [
            'IfcWall',
            'IfcSlab',
            'IfcRoof',
            'IfcBuildingElementProxy',
            'IfcBeam',
            'IfcFooting',
            'IfcColumn',
            'IfcStair',
            'IfcPile',
            'IfcPlate'
        ]
