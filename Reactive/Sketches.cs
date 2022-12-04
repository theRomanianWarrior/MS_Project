using System;

public class Sketches
{
    public record Coordinates
    {
        public int X { get; set; }
        public int Y { get; set; }
    
    }

    private static Coordinates coord = new()
    {
        X = 6,
        Y = 0
    };
    private static int fieldOfViewAround = 2;
    public static void Main1(string[] args)
    {
        for (var radius = 1; radius <= fieldOfViewAround; ++radius)
        {
            for (var xAxisCoord = coord.X - radius; xAxisCoord <= coord.X + radius; xAxisCoord++)
            {
                Console.WriteLine($@"{xAxisCoord} {coord.Y - radius}");
                Console.WriteLine($@"{xAxisCoord} {coord.Y + radius}");
            }

            for (var yAxisCoord = coord.Y - radius + 1; yAxisCoord <= coord.Y + radius - 1; yAxisCoord++)
            {
                Console.WriteLine($@"{coord.X - radius} {yAxisCoord}");
                Console.WriteLine($@"{coord.X + radius} {yAxisCoord}");
            }
            Console.WriteLine();
        }
    }
}