namespace Reactive;

using System;
					
public static class Sketches
{
    private static void FindExitInProximity()
    {
        const int fieldOfViewAround = 1;
        const int X = 6;
        const int Y = 3;

        for (var radius = 1; radius <= fieldOfViewAround; ++radius)
        {
            for (var xAxisCoord = X - radius; xAxisCoord <= X + radius; xAxisCoord++)
            {
                Console.WriteLine($"{Y - radius} {xAxisCoord}");
                Console.WriteLine($"{Y + radius} {xAxisCoord}");
            }

            for (var yAxisCoord = Y - radius + 1; yAxisCoord <= Y + radius - 1; yAxisCoord++)
            {
                Console.WriteLine($"{X - radius} {yAxisCoord}");
                Console.WriteLine($"{X + radius} {yAxisCoord}");
            }
        }
    }
}