#import ifcopenshell

def getLengthUnit(ifc_file):
    usedUnits = ifc_file.by_type('IfcUnitAssignment')[0].Units

    for unit in usedUnits:
        if unit.UnitType == 'LENGTHUNIT':
            if unit.Name == 'METRE' or unit.Name == 'METER':
                if unit.Prefix is None:
                    return 'meter'
                if unit.Prefix == 'MILLI':
                    return 'millimeter'