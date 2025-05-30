
using Playground;

using System.Security.Cryptography;

//var vertices = new (double x, double y)[]
//{
//    (0, 0),
//    (2, 1),
//    (4, 0),
//    (3, 2),
//    (1, 2),
//    (0, 3),
//    (3, 3)
//};

//var triangles = new int[] 
//{ 
//    0, 1, 4, 
//    3, 4, 1,
//    1, 2, 3,
//    3, 6, 4,
//    4, 5, 0
//};

var dim = 10;
var vertices = new (double x, double y)[dim * dim];
var triangles = new int[(dim - 1) * (dim - 1) * 6];

for (int y = 0; y < dim; y++)
{
    for (int x = 0; x < dim; x++)
    {
        vertices[y * dim + x] = (x, y);
    }
}
for (int i = 0; i < dim - 1; i++)
{
    int row = i * (dim - 1) * 6;
    int btm = i * dim;
    int top = btm + dim;
    for (int j = 0; j < dim - 1; j++)
    {
        triangles[row + j * 6 + 0] = btm + j; 
        triangles[row + j * 6 + 1] = btm + j + 1;
        triangles[row + j * 6 + 2] = top + j + 1;
        triangles[row + j * 6 + 3] = btm + j;
        triangles[row + j * 6 + 4] = top + j + 1;
        triangles[row + j * 6 + 5] = top + j;
    }
}



if (!Tin.Create(vertices, triangles, out var tin))
{
    Console.WriteLine("Failed to create Tin.");
}
else
{
    Console.WriteLine("Tin created successfully!");
    Console.WriteLine($"Vertices: {string.Join(", ", tin.Vertices.Select(v => $"({v.x}, {v.y})"))}");
    Console.WriteLine($"Indizes  : {string.Join(", ", Enumerable.Range(0, tin.Triangles.Length).Chunk(3).Select(t => $"{t[0],2} {t[1],2} {t[2],2}"))}");
    Console.WriteLine($"Triangles: {string.Join(", ", tin.Triangles.Chunk(3).Select(t => $"{t[0],2} {t[1],2} {t[2],2}"))}");
    Console.WriteLine($"Opposites: {string.Join(", ", tin.Opposites.Chunk(3).Select(t => $"{t[0],2} {t[1],2} {t[2],2}"))}");
    Console.WriteLine($"Hull: {string.Join(", ", tin.Hull)}");
    Console.WriteLine($"IsInterior: {string.Join(", ", tin.IsInterior.Cast<bool>())}");

    //var p = (1d, 2d);
    //Console.WriteLine(FormattableString.Invariant($"Test point {p} intersects: {tin.Intersects(p)}"));
    //p = (2, 2.6);
    //Console.WriteLine(FormattableString.Invariant($"Test point {p} intersects: {tin.Intersects(p)}"));
    //p = (0.5, 1);
    //Console.WriteLine(FormattableString.Invariant($"Test point {p} intersects: {tin.Intersects(p)}"));
    //p = (2, 1.5);
    //Console.WriteLine(FormattableString.Invariant($"Test point {p} intersects: {tin.Intersects(p)}"));
    //p = (1.5, 0);
    //Console.WriteLine(FormattableString.Invariant($"Test point {p} intersects: {tin.Intersects(p)}"));
    //p = (4, 2);
    //Console.WriteLine(FormattableString.Invariant($"Test point {p} intersects: {tin.Intersects(p)}"));


}

















//// See https://aka.ms/new-console-template for more information

//using Raum2D;
//using Raum2D.Features;
//using Raum2D.Geometry;
//using Raum2D.Topology;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Logging.Console;


//// Update the logger initialization to use the correct method
//Raum2D.Common.Logger = LoggerFactory
//    .Create(static builder => builder.AddSimpleConsole(options =>
//    {
//        options.SingleLine = true;
//        options.TimestampFormat = "HH:mm:ss ";
//        options.ColorBehavior = LoggerColorBehavior.Enabled;
//        options.IncludeScopes = true;
//    }))
//    .CreateLogger("Raum2D");

//string[] wkts =
//[
//    "POLYGON ((35 10, 45 45, 15 40, 10 20, 35 10),(20 30, 35 35, 30 20,20 30))",
//    //"POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))",
//    //"LINESTRING (30 10, 10 30, 40 40)",
//    //"POINT (30 10)",
//    //"MULTIPOINT ((10 40), (40 30), (20 20), (30 10))",
//    //"MULTIPOINT (10 40, 40 30, 20 20, 30 10)",
//    //"MULTILINESTRING ((10 10, 20 20, 10 40),\r\n(40 40, 30 30, 40 20, 30 10))",
//    //"MULTIPOLYGON (((30 10, 45 40, 10 40, 30 10)),\r\n((15 5, 40 10, 10 20, 5 10, 15 5)))",
//    "MULTIPOLYGON (((40 40, 20 45, 45 30, 40 40)),\r\n((20 35, 10 30, 10 10, 30 5, 45 20, 20 35),\r\n(30 20, 20 15, 20 25, 30 20)))",
//];
//var features = new SimpleFeature[wkts.Length];
//for (int i = 0; i < wkts.Length; i++)
//{
//    if (SimpleFeature.TryParseWkt(wkts[i], out var feature))
//    {
//    }
//    else
//    {
//        Console.WriteLine($"Failed to parse WKT: {wkts[i]}");
//    }
//    features[i] = feature;

//    Console.WriteLine(feature);
//}
//SimpleFeature.WriteSVG("result", features);

//if (Operation.Create(out var operations, features))
//{
//    // Perform union operation
//    if (operations.Boolean(Operation.BooleanType.Union, features[0], features[1], out var union))
//    {
//        Console.WriteLine("Union Result:");
//        Console.WriteLine(union);
//        SimpleFeature.WriteSVG("union", union);
//    }

//    // Perform intersection operation
//    if (operations.Boolean(Operation.BooleanType.Intersection, features[0], features[1], out var intersection))
//    {
//        Console.WriteLine("Intersection Result:");
//        Console.WriteLine(intersection);
//        SimpleFeature.WriteSVG("intersection", intersection);
//    }

//    // Perform difference operation
//    if (operations.Boolean(Operation.BooleanType.Difference, features[0], features[1], out var difference))
//    {
//        Console.WriteLine("Difference Result:");
//        Console.WriteLine(difference);
//        SimpleFeature.WriteSVG("difference", difference);
//    }

//    // Perform symmetric difference operation
//    if (operations.Boolean(Operation.BooleanType.SymDifference, features[0], features[1], out var symDifference))
//    {
//        Console.WriteLine("SymDifference Result:");
//        Console.WriteLine(symDifference);
//        SimpleFeature.WriteSVG("symDifference", symDifference);
//    }

//}
//else
//{
//    Console.WriteLine("Failed to create operations.");
//}

//// Check MultiLineString
//wkts =
//[
//    "LineSTRING (1 1, 4 2, 1 3, 4 4)",
//    "lineSTRINg   (3 1, 3 2, 2 2, 2 3,4 3.0000 , 2.5 3.5)",
//];
//features = new SimpleFeature[wkts.Length];
//for (int i = 0; i < wkts.Length; i++)
//{
//    if (SimpleFeature.TryParseWkt(wkts[i], out var feature))
//    {
//    }
//    else
//    {
//        Console.WriteLine($"Failed to parse WKT: {wkts[i]}");
//    }
//    features[i] = feature;

//    Console.WriteLine(feature);
//}
//SimpleFeature.WriteSVG("lresult", features);


//if (Operation.Create(out operations, features))
//{
//    // Perform union operation
//    if (operations.Boolean(Operation.BooleanType.Union, features[0], features[1], out var union))
//    {
//        Console.WriteLine("Union Result:");
//        Console.WriteLine(union);
//        SimpleFeature.WriteSVG("lunion", union);
//    }

//    // Perform intersection operation
//    if (operations.Boolean(Operation.BooleanType.Intersection, features[0], features[1], out var intersection))
//    {
//        Console.WriteLine("Intersection Result:");
//        Console.WriteLine(intersection);
//        SimpleFeature.WriteSVG("lintersection", intersection);
//    }

//    // Perform difference operation
//    if (operations.Boolean(Operation.BooleanType.Difference, features[0], features[1], out var difference))
//    {
//        Console.WriteLine("Difference Result:");
//        Console.WriteLine(difference);
//        SimpleFeature.WriteSVG("ldifference", difference);
//    }

//    // Perform symmetric difference operation
//    if (operations.Boolean(Operation.BooleanType.SymDifference, features[0], features[1], out var symDifference))
//    {
//        Console.WriteLine("SymDifference Result:");
//        Console.WriteLine(symDifference);
//        SimpleFeature.WriteSVG("lsymDifference", symDifference);
//    }

//}
//else
//{
//    Console.WriteLine("Failed to create operations.");
//}


//Console.WriteLine("fertig!");
