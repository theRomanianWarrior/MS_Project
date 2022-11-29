using System.Threading;

namespace Reactive
{
    public class Program
    {
        private static void Main()
        {
            //EnvironmentMas env = new EnvironmentMas(0, 100);

            var planetAgent = new PlanetAgent();
            MasEnvironmentSingleton.Instance.Add(planetAgent, "planet");
            
            for (int i = 1; i <= Utils.NoExplorers; i++)
            {
                var explorerAgent = new ExplorerAgent();
                MasEnvironmentSingleton.Instance.Add(explorerAgent, "explorer" + i);
            }

            //Thread.Sleep(500);

            MasEnvironmentSingleton.Instance.Start();
        }
    }
}