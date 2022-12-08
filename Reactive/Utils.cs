using System;
using System.Collections.Generic;

namespace Reactive
{
    public static class Utils
    {
        public static readonly int Size = 10;
        public static readonly int NoEvacuationAgents = 20; // No of worker agents
        public static readonly int NoExits = 3; // No of exits
        public static readonly int SecondsBeforeAlarm = 1;
        
        public static readonly int DelayBetweenTurns = 0;
        public static readonly Random RandNoGen = new();
        public static int NumberOfRepeatingRounds = 100;


        public static Coordinates ParseCoordinates(this string coordinates)
        {
            var split = coordinates.Split();
            return new Coordinates
            {
                X = int.Parse(split[0]),
                Y = int.Parse(split[1])
            };
        }
        public static void ParseMessage(string content, out string action, out List<string> parameters)
        {
            string[] t = content.Split();

            action = t[0];

            parameters = new List<string>();
            for (int i = 1; i < t.Length; i++)
                parameters.Add(t[i]);
        }

        public static void ParseMessage(string content, out string action, out string parameters)
        {
            string[] t = content.Split();

            action = t[0];

            parameters = "";

            if (t.Length > 1)
            {
                for (var i = 1; i < t.Length - 1; i++)
                    parameters += t[i] + " ";
                parameters += t[t.Length - 1];
            }
        }

        public static string Str(object p1, object p2)
        {
            return $"{p1} {p2}";
        }

        public static string Str(object p1, object p2, object p3)
        {
            return $"{p1} {p2} {p3}";
        }
    }
}