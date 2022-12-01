using ActressMas;

namespace Reactive
{
    public static class Program
    {
        private static void Main()
        {
            var env = new EnvironmentMas(0, 100);

            var planetAgent = new PlanetAgent();
            env.Add(planetAgent, "planet");
            
            for (var i = 1; i <= Utils.NoExplorers; i++)
            {
                var explorerAgent = new ExplorerAgent();
                env.Add(explorerAgent, "explorer" + i);
            }
            
            env.Start();
        }
    }
}