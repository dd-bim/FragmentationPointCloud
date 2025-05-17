// See https://aka.ms/new-console-template for more information

using Playground;

var a = new VertexList(true, [
    (1, 2),
    (2, 3),
    (3, 2),
    (2, 1),
]);

var b = new VertexList(true, [
    (2, 2),
    (3, 1),
    (4, 2),
    (3, 3)
]);

var c = new VertexList(false, [
    (0, 0),
    (5, 0),
    (5, 5),
    (0, 5)
]);

var polygonA = new Polygon([a]);
var polygonB = new Polygon([b]);
var polygonC = new Polygon([c]);

var polygon1 = GpcWrapper.AddVertexList(polygonC, a);
var polygon2 = GpcWrapper.AddVertexList(polygon1, b);
var result = GpcWrapper.Clip(GpcOperation.Union,  polygon2, new Polygon([]));

Console.WriteLine("Polygon1: ");
foreach (var contour in polygon1.Contours)
{
    Console.WriteLine($"Contour: {contour.IsHole}");
    foreach (var vertex in contour.Vertices)
    {
        Console.WriteLine($"Vertex: {vertex.x}, {vertex.y}");
    }
}
Console.WriteLine();
Console.WriteLine("Polygon2: ");
foreach (var contour in polygon2.Contours)
{
    Console.WriteLine($"Contour: {contour.IsHole}");
    foreach (var vertex in contour.Vertices)
    {
        Console.WriteLine($"Vertex: {vertex.x}, {vertex.y}");
    }
}
Console.WriteLine(); Console.WriteLine("Result: ");
foreach (var contour in result.Contours)
{
    Console.WriteLine($"Contour: {contour.IsHole}");
    foreach (var vertex in contour.Vertices)
    {
        Console.WriteLine($"Vertex: {vertex.x}, {vertex.y}");
    }
}
Console.WriteLine();

Console.WriteLine("fertig!");
