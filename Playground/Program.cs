// See https://aka.ms/new-console-template for more information

using Raum2D;
using Raum2D.Features;
using Raum2D.Geometry;
using Raum2D.Topology;
using Microsoft.Extensions.Logging;


// Update the logger initialization to use the correct method
Raum2D.Common.Logger = LoggerFactory
    .Create(static builder => builder.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    }))
    .CreateLogger("Raum2D");

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
var features = new SimpleFeature[wkts.Length];
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

//if(Operation.Create(out var operations, features))
//{
//    // Perform union operation
//    if (operations.Boolean(Operation.BooleanType.Union, 0, 1, out var union))
//    {
//        Console.WriteLine("Union Result:");
//        Console.WriteLine(union);
//        SimpleFeature.WriteSVG("union", union);
//    }

//    // Perform intersection operation
//    if (operations.Boolean(Operation.BooleanType.Intersection, 0, 1, out var intersection))
//    {
//        Console.WriteLine("Intersection Result:");
//        Console.WriteLine(intersection);
//        SimpleFeature.WriteSVG("intersection", intersection);
//    }

//    // Perform difference operation
//    if (operations.Boolean(Operation.BooleanType.Difference, 0, 1, out var difference))
//    {
//        Console.WriteLine("Difference Result:");
//        Console.WriteLine(difference);
//        SimpleFeature.WriteSVG("difference", difference);
//    }

//    // Perform symmetric difference operation
//    if (operations.Boolean(Operation.BooleanType.SymDifference, 0, 1, out var symDifference))
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

// Check MultiLineString
wkts =
[
    "LineSTRING (1 1, 4 2, 1 3, 4 4)",
    "lineSTRINg   (3 1, 3 2, 2 2, 2 3,4 3 , 2.5 3.5)",
];
features = new SimpleFeature[wkts.Length];
for (int i = 0; i < wkts.Length; i++)
{
    if (SimpleFeature.TryParseWkt(wkts[i], out var feature))
    {
    }
    else
    {
        Console.WriteLine($"Failed to parse WKT: {wkts[i]}");
    }
    features[i] = feature;

    Console.WriteLine(feature);
}
SimpleFeature.WriteSVG("lresult", features);


if (Operation.Create(out var operations, features))
{
    // Perform union operation
    if (operations.Boolean(Operation.BooleanType.Union, 0, 1, out var union))
    {
        Console.WriteLine("Union Result:");
        Console.WriteLine(union);
        SimpleFeature.WriteSVG("lunion", union);
    }

    // Perform intersection operation
    if (operations.Boolean(Operation.BooleanType.Intersection, 0, 1, out var intersection))
    {
        Console.WriteLine("Intersection Result:");
        Console.WriteLine(intersection);
        SimpleFeature.WriteSVG("lintersection", intersection);
    }

    // Perform difference operation
    if (operations.Boolean(Operation.BooleanType.Difference, 0, 1, out var difference))
    {
        Console.WriteLine("Difference Result:");
        Console.WriteLine(difference);
        SimpleFeature.WriteSVG("ldifference", difference);
    }

    // Perform symmetric difference operation
    if (operations.Boolean(Operation.BooleanType.SymDifference, 0, 1, out var symDifference))
    {
        Console.WriteLine("SymDifference Result:");
        Console.WriteLine(symDifference);
        SimpleFeature.WriteSVG("lsymDifference", symDifference);
    }

}
else
{
    Console.WriteLine("Failed to create operations.");
}


Console.WriteLine("fertig!");
