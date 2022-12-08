namespace Reactive
{
    public static class Program
    {
        private static void Main()
        {
            var planetAgent = new CoordinatorAgent();
            MasEnvSingleton.Instance.Add(planetAgent, "coordinator");
            
            for (var i = 1; i <= Utils.NoEvacuationAgents; i++)
            {
                var explorerAgent = new EvacuationAgent();
                MasEnvSingleton.Instance.Add(explorerAgent, "evacuation" + i);
            }
            
            MasEnvSingleton.Instance.Start();
        }
    }
}