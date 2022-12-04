using ActressMas;

namespace Reactive
{
    public static class Program
    {
        private static void Main()
        {
            var env = new EnvironmentMas(0, 100);

            var planetAgent = new CoordinatorAgent();
            env.Add(planetAgent, "coordinator");
            
            for (var i = 1; i <= Utils.NoExplorers; i++)
            {
                var explorerAgent = new EvacuationAgent();
                env.Add(explorerAgent, "evacuation" + i);
            }
            
            env.Start();
        }
    }
}