// See https://aka.ms/new-console-template for more information

using Playground;
using Playground.Raum.Geometry;
using Playground.Raum.Topology;

static void WriteCollection((double x, double y)[][][][] collection)
{
    for (int i = 0; i < collection.Length; i++)
    {
        var item = collection[i];
        Console.WriteLine($"Item {i + 1}:");
        for (int j = 0; j < item.Length; j++)
        {
            var subItem = item[j];
            Console.WriteLine($"  SubItem {j + 1}:");
            for (int k = 0; k < subItem.Length; k++)
            {
                var subSubItem = subItem[k];
                Console.WriteLine($"    SubSubItem {k + 1}:");
                for (int l = 0; l < subSubItem.Length; l++)
                {
                    var point = subSubItem[l];
                    Console.WriteLine(FormattableString.Invariant($"      Point {l + 1}: {point}"));
                }
            }
        }
    }
}

var a = new (double x, double y)[]
{
    (1, 2),
    (2, 3),
    (3, 2),
    (2, 1),
    (1, 2),
};

var b = new (double x, double y)[]
{
    (2, 2),
    (3, 1),
    (4, 2),
    (3, 3),
    (2, 2),
};

var c = new (double x, double y)[]
{
    (0, 0),
    (5, 0),
    (5, 5),
    (0, 5),
    (0, 0),
};

var d = new (double x, double y)[]
{
    (5, 0),
    (5, 1),
    (6, 1),
    (6, 0),
    (5, 0),
};

var box = new BoundingBox();
box.Extend(a);
box.Extend(b);
box.Extend(c);
box.Extend(d);

if (!Epsilon.Create(1,box, out var epsilon))
{
    Console.WriteLine("Epsilon could not be created.");
    return;
}

var (_, result) = Partition.CreateFromRegions(epsilon, out var partion, [[c,a,b,d]]);
partion.WriteSvg("result");
Console.WriteLine("Result:");
WriteCollection(result);
Console.WriteLine();


//var (_, polyA) = Partition.CreateFromRegions(epsilon, out var partionA, [[a]]);
//partionA.WriteSvg("A");
//Console.WriteLine("A:");
//WriteCollection(polyA);
//Console.WriteLine();

//var (_, polyB) = Partition.CreateFromRegions(epsilon, out var partionB, [[b]]);
//partionB.WriteSvg("B");
//Console.WriteLine("B:");
//WriteCollection(polyB);
//Console.WriteLine();

//var (_, polyC) = Partition.CreateFromRegions(epsilon, out var partionC, [[c]]);
//partionC.WriteSvg("C");
//Console.WriteLine("C:");
//WriteCollection(polyC);
//Console.WriteLine();

Console.WriteLine("fertig!");
