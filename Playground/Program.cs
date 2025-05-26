// See https://aka.ms/new-console-template for more information

using Raum2D;
using Raum2D.Features;
using Raum2D.Geometry;
using Raum2D.Topology;

string[] wkts =
[
    "POLYGON ((35 10, 45 45, 15 40, 10 20, 35 10),(20 30, 35 35, 30 20,20 30))",
    //"POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))",
    //"LINESTRING (30 10, 10 30, 40 40)",
    //"POINT (30 10)",
    //"MULTIPOINT ((10 40), (40 30), (20 20), (30 10))",
    //"MULTIPOINT (10 40, 40 30, 20 20, 30 10)",
    //"MULTILINESTRING ((10 10, 20 20, 10 40),\r\n(40 40, 30 30, 40 20, 30 10))",
    //"MULTIPOLYGON (((30 10, 45 40, 10 40, 30 10)),\r\n((15 5, 40 10, 10 20, 5 10, 15 5)))",
    "MULTIPOLYGON (((40 40, 20 45, 45 30, 40 40)),\r\n((20 35, 10 30, 10 10, 30 5, 45 20, 20 35),\r\n(30 20, 20 15, 20 25, 30 20)))",
];
var features = new OgcSf[wkts.Length];
for (int i = 0; i < wkts.Length; i++)
{
    if (OgcSf.TryParseWkt(wkts[i], out var feature))
    {
    }
    else
    {
        Console.WriteLine($"Failed to parse WKT: {wkts[i]}");
    }
    features[i] = feature;

    Console.WriteLine(feature);
}
OgcSf.WriteSVG("result", features);

if(Operation.Create(out var operations, features))
{
    // Perform union operation
    if (operations.Union(0, 1, out var union))
    {
        Console.WriteLine("Union Result:");
        Console.WriteLine(union);
        OgcSf.WriteSVG("union", union);
    }

    // Perform intersection operation
    if (operations.Intersection(0, 1, out var intersection))
    {
        Console.WriteLine("Intersection Result:");
        Console.WriteLine(intersection);
        OgcSf.WriteSVG("intersection", intersection);
    }

    // Perform difference operation
    if (operations.Difference(0, 1, out var difference))
    {
        Console.WriteLine("Difference Result:");
        Console.WriteLine(difference);
        OgcSf.WriteSVG("difference", difference);
    }

    // Perform XOR operation
    if (operations.Xor(0, 1, out var xor))
    {
        Console.WriteLine("XOR Result:");
        Console.WriteLine(xor);
        OgcSf.WriteSVG("xor", xor);
    }

}
else
{
    Console.WriteLine("Failed to create operations.");
}

//static void WriteCollection((double x, double y)[][][][] collection)
//{
//    for (int i = 0; i < collection.Length; i++)
//    {
//        var item = collection[i];
//        Console.WriteLine($"Geometry {i + 1}:");
//        WriteMultiPolygon(item, 1);
//    }
//}

//static void WriteMultiPolygon((double x, double y)[][][] multiPolygon, int leftMargin = 0)
//{
//    var leftMarginString = new string(' ', leftMargin * 2);
//    for (int i = 0; i < multiPolygon.Length; i++)
//        {
//            var subItem = multiPolygon[i];
//            Console.WriteLine($"{leftMarginString}Polygon {i + 1}:");
//            for (int j = 0; j < subItem.Length; j++)
//            {
//                var subSubItem = subItem[j];
//                Console.WriteLine($"{leftMarginString}  Ring {j + 1}:");
//                for (int k = 0; k < subSubItem.Length; k++)
//                {
//                    var point = subSubItem[k];
//                    Console.WriteLine(FormattableString.Invariant($"{leftMarginString}    Point {k + 1}: {point}"));
//                }
//            }
//        }
//}




//var a = new (double x, double y)[]
//{
//    (1, 2),
//    (2, 3),
//    (3, 2),
//    (2, 1),
//    (1, 2),
//};

//var b = new (double x, double y)[]
//{
//    (2, 2),
//    (3, 1),
//    (4, 2),
//    (3, 3),
//    (2, 2),
//};

//var c = new (double x, double y)[]
//{
//    (0, 0),
//    (5, 0),
//    (5, 5),
//    (0, 5),
//    (0, 0),
//};

//var d = new (double x, double y)[]
//{
//    (5, 0),
//    (5, 1),
//    (6, 1),
//    (6, 0),
//    (5, 0),
//};
//var e = new (double x, double y)[]
//{
//    (1, 2.8),
//    (4, 2.9),
//    (4, 4),
//    (1, 4),
//    (1, 2.8),
//};
//var f = new (double x, double y)[]
//{
//    (2.7, 0.5),
//    (2.8, 5.1),
//    (6.1, 5.1),
//    (5.9, 0.6),
//    (2.7, 0.5),
//};

//var box = new BoundingBoxXY();
//box.Extend(a);
//box.Extend(b);
//box.Extend(c);
//box.Extend(d);
//box.Extend(e);
//box.Extend(f);

//if (!Transformation.Create(6,box, out var epsilon))
//{
//    Console.WriteLine("Epsilon could not be created.");
//    return;
//}
//var partition = new Partition(epsilon);

//var (features, multiPolygons) = Partition.CreateFromRegions(partition, [[e,a,b,d,c]], [[f]]);
//partition.WriteSvg("result");
//Polygon.WriteMultiPolygonSVG("resultPoly", multiPolygons);

//Console.WriteLine("Result:");
//WriteCollection(multiPolygons);
//Console.WriteLine();

//var union = partition.Union(features[0].Id, features[1].Id);

//Console.WriteLine("Union:");
//WriteMultiPolygon(union);
//Polygon.WriteMultiPolygonSVG("union", union);
//Console.WriteLine();

//var intersection = partition.Intersection(features[0].Id, features[1].Id);
//Console.WriteLine("Intersection:");
//WriteMultiPolygon(intersection);
//Polygon.WriteMultiPolygonSVG("intersection", intersection);
//Console.WriteLine();

//var difference = partition.Difference(features[0].Id, features[1].Id);
//Console.WriteLine("Difference:");
//WriteMultiPolygon(difference);
//Polygon.WriteMultiPolygonSVG("difference", difference);
//Console.WriteLine();

//Console.WriteLine("Xor:");
//var xor = partition.Xor(features[0].Id, features[1].Id);
//WriteMultiPolygon(xor);
//Polygon.WriteMultiPolygonSVG("xor", xor);
//Console.WriteLine();

//var partition2 = new Partition(epsilon);
//var (features2, multiPolygons2) = Partition.CreateFromRegions(partition, [[e, a, b, d, c, f]]);
//partition.WriteSvg("result2");
//Polygon.WriteMultiPolygonSVG("resultPoly2", multiPolygons2[0]);
//Console.WriteLine("Result2:");
//WriteCollection(multiPolygons2);
//Console.WriteLine();


Console.WriteLine("fertig!");
