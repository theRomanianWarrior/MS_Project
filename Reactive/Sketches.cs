namespace Reactive;

using System;
					
public static class Sketches
{
    private static void FindExitInProximity()
    {
        const int fieldOfViewAround = 1;
        const int x = 6;
        const int y = 3;

        for (var radius = 1; radius <= fieldOfViewAround; ++radius)
        {
            for (var xAxisCoord = x - radius; xAxisCoord <= x + radius; xAxisCoord++)
            {
                Console.WriteLine(@$"{y - radius} {xAxisCoord}");
                Console.WriteLine(@$"{y + radius} {xAxisCoord}");
            }

            for (var yAxisCoord = y - radius + 1; yAxisCoord <= y + radius - 1; yAxisCoord++)
            {
                Console.WriteLine(@$"{x - radius} {yAxisCoord}");
                Console.WriteLine(@$"{x + radius} {yAxisCoord}");
            }
        }
    }
}