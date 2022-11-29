using System;
using System.Collections.Generic;

namespace Reactive
{
    public static class Utils
    {
        public static int Size = 11;
        public static int NoExplorers = 50;//119; // No of worker agents
        public static int NoResources = 1; // No of exits

        public static int DelayBetweenTurns = 0;
        public static Random RandNoGen = new();


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
                for (int i = 1; i < t.Length - 1; i++)
                    parameters += t[i] + " ";
                parameters += t[t.Length - 1];
            }
        }

        public static string Str(object p1, object p2)
        {
            return string.Format("{0} {1}", p1, p2);
        }

        public static string Str(object p1, object p2, object p3)
        {
            return string.Format("{0} {1} {2}", p1, p2, p3);
        }
    }
}