from OCC.Display.SimpleGui import init_display
from ifcopenshell import geom

def quickViewFromShapeList(shapes):
    display, start_display, add_menu, add_function_to_menu = init_display()

    if hasattr(shapes, '__iter__'):
        for shape in shapes:
            display.DisplayShape(shape)
    else:
        display.DisplayShape(shapes)

    display.FitAll()
    start_display()

def quickViewFromEntityList(entitiesToShow):

    settings = geom.settings()
    settings.set(settings.USE_PYTHON_OPENCASCADE, True)

    geometries = []
    noGeomCounter = 0
    for entity in entitiesToShow:

        if entity.Representation is not None:
            try:
                geometries.append(geom.create_shape(settings, entity).geometry)
            except:
                print('could not generate geoemtry for entity with id ' + entity.GlobalId)
        else:
            noGeomCounter +=1

    print('no Geometry for {} items'.format(noGeomCounter))

    if len(geometries) > 0:
        display, start_display, add_menu, add_function_to_menu = init_display()
        display.DisplayShape(geometries)

        display.FitAll()

        start_display()