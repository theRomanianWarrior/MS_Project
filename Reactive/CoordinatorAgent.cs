using ActressMas;
using Message = ActressMas.Message;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Reactive
{
    public class CoordinatorAgent : Agent
    {
        private PlanetForm _formGui;
        public Dictionary<string, string> EvacuationAgentsPositions { get; set; }
        public Dictionary<string, string> ExitsPositions { get; set; } // aka exits position
        private readonly string _basePosition; // position of "special agent"
        private bool _alertWasStarted;
        private const int FieldOfViewAround = 2; // this mean 3x3
        private readonly List<string> _agentsNotifiedAboutAlarm = new();
        private readonly Stopwatch _watch = new ();
        private List<double> EvacuationTimeHistory = new();
        
        public CoordinatorAgent()
        {
            EvacuationAgentsPositions = new Dictionary<string, string>();
            ExitsPositions = new Dictionary<string, string>();
            _basePosition = Utils.Str(Utils.Size / 2, Utils.Size / 2);
            MasEnvSingleton.Instance = new EnvironmentMas();
            var t = new Thread(GuiThread);
            t.Start();
        }

        private void ResetAllResourcesAndUpdateUi()
        {
            EvacuationAgentsPositions.Clear();
            ExitsPositions.Clear();
            _alertWasStarted = false;
            _agentsNotifiedAboutAlarm.Clear();
            _watch.Reset();
            
            _formGui.UpdatePlanetGUI();
        }
        
        private void GuiThread()
        {
            _formGui = new PlanetForm();
            _formGui.SetOwner(this);
            _formGui.ShowDialog();
            Application.Run();
        }

        private void BeginNewRound()
        {
            //Thread.Sleep(100);
            Console.WriteLine($@"Remaining rounds: {Utils.NumberOfRepeatingRounds}");
            ResetAllResourcesAndUpdateUi();
            
            AddEvacuationAgentsAgainForNewRound();

            Setup();
        }

        private void AddEvacuationAgentsAgainForNewRound()
        {
            for (var i = 1; i <= Utils.NoEvacuationAgents; i++)
            {
                var explorerAgent = new EvacuationAgent();
                MasEnvSingleton.Instance.Add(explorerAgent, "evacuation" + i);
            }
        }
        // second build resources
        public override void Setup()
        {
            Console.WriteLine(@"Starting " + Name);

            List<string> resPos = new List<string>();
            string compPos = Utils.Str(Utils.Size / 2, Utils.Size / 2);
            resPos.Add(compPos); // the position of the base

            for (int i = 1; i <= Utils.NoExits; i++)
            {
                while (resPos.Contains(compPos)) // resources do not overlap
                {
                    int x = Utils.RandNoGen.Next(Utils.Size);
                    int y = Utils.RandNoGen.Next(Utils.Size);
                    compPos = Utils.Str(x, y);
                }

                ExitsPositions.Add("res" + i, compPos); // exits position
                resPos.Add(compPos);
            }

            Task.Delay(new TimeSpan(0, 0, Utils.SecondsBeforeAlarm)).ContinueWith(_ => { NotifyAlarmIsStarted(); });
        }

        public override void Act(Message message)
        {
            Console.WriteLine(@"\t[{1} -> {0}]: {2}", this.Name, message.Sender, message.Content);

            Utils.ParseMessage(message.Content, out var action, out string parameters);
            
            switch (action)
            {
                case "position":
                    HandlePosition(message.Sender, parameters); // FIRST CALL
                    break;

                case "change":
                    HandleChange(message.Sender, parameters);
                    break;
                
                case "findExitInMyArea": // here agent makes a step in and tries to search for another agents or an exit 
                    var isBlocker = HandleMoveForFindingExit(message.Sender, parameters); 
                    if (isBlocker) break;
                    FindExitOrAgentInProximity(message.Sender, parameters);
                    break;
                case "nextMoveToExit":
                    MoveToExit(message.Sender, parameters);
                    break;
            }
            _formGui.UpdatePlanetGUI();
            Thread.Sleep(Utils.DelayBetweenTurns);
        }

        private bool HandleMoveForFindingExit(string sender, string position)
        {
            // Check if on given coordinates is not an agent already
            if (position == _basePosition)
            {
                Send(sender, "directionToExitBlocked");
                return true;
            }
            
            foreach (var k in EvacuationAgentsPositions.Keys)// for each evacuation in coordinator's database
            {
                if (k == sender)  // if current evacuation from coordinator's database is same as evacuation that have sent request to change
                    continue;
                if (EvacuationAgentsPositions[k] == position) // if evacuation wants to move on a position that another evacuation hold, send block
                {
                    Send(sender, "directionToExitBlocked");
                    return true;
                }
            }
            
            EvacuationAgentsPositions[sender] = position;
            
            return false;
        }

        private void MoveToExit(string sender, string position)
        {
            try
            {
                if (position == _basePosition)
                {
                    Send(sender, "block");
                    return;
                }

                foreach (string k in EvacuationAgentsPositions.Keys)
                {
                    if (k == sender)
                        continue;
                    if (EvacuationAgentsPositions[k] == position)
                    {
                        Send(sender, "block");
                        return;
                    }
                }
                
                EvacuationAgentsPositions[sender] = position;

                if (ExitsPositions!.Values.Contains(position) && position != _basePosition) // if position is already on exit, remove agent
                {
                    _formGui.UpdatePlanetGUI();
                    Console.WriteLine(@$"Agent {sender} is out from {position}");
                    EvacuationAgentsPositions.Remove(sender);
                    Environment.Remove(sender);
                    if (Environment.NoAgents == 1)
                    {
                        _watch.Stop();
                        if (Utils.NumberOfRepeatingRounds > 0)
                        {
                            _formGui.UpdatePlanetGUI();
                            --Utils.NumberOfRepeatingRounds;
                            var evacuationTime = Math.Round(TimeSpan.FromMilliseconds(_watch.ElapsedMilliseconds).TotalSeconds, 3);
                            Console.WriteLine(@$"Time required to evacuate this round: {evacuationTime}");

                            EvacuationTimeHistory.Add(evacuationTime);
                            BeginNewRound();
                            return;
                        }
                        else
                        {
                            Console.WriteLine(@$"Average Evacuation Time: {EvacuationTimeHistory.Average()} seconds");
                            this.Stop();
                            return;
                        }
                    }
                }

                Send(sender, "moveToExit");
            }
            catch (Exception e)
            {
                Console.WriteLine(@"At MoveToExit " + e.Message);
            }
        }

        private void FindExitOrAgentInProximity(string sender, string position)
        {
            try
            {
                var coord = position.ParseCoordinates();

                var agentsPositionsExceptSender = GetAgentsPositionExceptSender(sender);

                if (EvacuationAgentsPositions.Keys.Contains(sender))
                {
                    for (var radius = 1; radius <= FieldOfViewAround; ++radius)
                    {
                        for (var xAxisCoord = coord.X - radius; xAxisCoord <= coord.X + radius; xAxisCoord++)
                        {
                            if (ExitsPositions!.Values.Contains(
                                    $@"{xAxisCoord} {coord.Y - radius}")) // search for exit on this axis
                            {
                                var exitCoordinates = $@"{xAxisCoord} {coord.Y - radius}";
                                MoveToFoundExit(sender, position, exitCoordinates);
                                return;
                            }

                            if (agentsPositionsExceptSender.Values.Contains(
                                    $@"{xAxisCoord} {coord.Y - radius}")) // search for an agent
                            {
                                var theAgentNameInProximity =
                                    agentsPositionsExceptSender.Single(a =>
                                        a.Value == $@"{xAxisCoord} {coord.Y - radius}");
                                var exit = FindExitInProximity(theAgentNameInProximity.Key,
                                    theAgentNameInProximity.Value); // find exit in the proximity of the proximity agent
                                if (exit != string.Empty)
                                {
                                    Console.WriteLine(@"Found exit in proximity of agent " +
                                                      theAgentNameInProximity.Key);
                                    MoveToFoundExit(sender, position, exit);
                                    return;
                                }
                            }

                            if (ExitsPositions!.Values.Contains($@"{xAxisCoord} {coord.Y + radius}"))
                            {
                                var exitCoordinates = $@"{xAxisCoord} {coord.Y + radius}";
                                MoveToFoundExit(sender, position, exitCoordinates);
                                return;
                            }

                            if (agentsPositionsExceptSender.Values.Contains($@"{xAxisCoord} {coord.Y + radius}"))
                            {
                                var theAgentNameInProximity =
                                    agentsPositionsExceptSender.Single(a =>
                                        a.Value == $@"{xAxisCoord} {coord.Y + radius}");
                                var exit = FindExitInProximity(theAgentNameInProximity.Key,
                                    theAgentNameInProximity.Value); // find exit in the proximity of the proximity agent
                                if (exit != string.Empty)
                                {
                                    Console.WriteLine(@"Found exit in proximity of agent " +
                                                      theAgentNameInProximity.Key);
                                    MoveToFoundExit(sender, position, exit);
                                    return;
                                }
                            }
                        }

                        for (var yAxisCoord = coord.Y - radius + 1; yAxisCoord <= coord.Y + radius - 1; yAxisCoord++)
                        {
                            if (ExitsPositions!.Values.Contains($@"{coord.X - radius} {yAxisCoord}"))
                            {
                                var exitCoordinates = $@"{coord.X - radius} {yAxisCoord}";
                                MoveToFoundExit(sender, position, exitCoordinates);
                                return;
                            }

                            if (agentsPositionsExceptSender.Values.Contains($@"{coord.X - radius} {yAxisCoord}"))
                            {
                                var theAgentNameInProximity =
                                    agentsPositionsExceptSender.Single(a =>
                                        a.Value == $@"{coord.X - radius} {yAxisCoord}");
                                var exit = FindExitInProximity(theAgentNameInProximity.Key,
                                    theAgentNameInProximity.Value); // find exit in the proximity of the proximity agent
                                if (exit != string.Empty)
                                {
                                    Console.WriteLine(@"Found exit in proximity of agent " +
                                                      theAgentNameInProximity.Key);
                                    MoveToFoundExit(sender, position, exit);
                                    return;
                                }
                            }

                            if (ExitsPositions!.Values.Contains($@"{coord.X + radius} {yAxisCoord}"))
                            {
                                var exitCoordinates = $@"{coord.X + radius} {yAxisCoord}";
                                MoveToFoundExit(sender, position, exitCoordinates);
                                return;
                            }

                            if (agentsPositionsExceptSender.Values.Contains($@"{coord.X + radius} {yAxisCoord}"))
                            {
                                var theAgentNameInProximity =
                                    agentsPositionsExceptSender.Single(a =>
                                        a.Value == $@"{coord.X + radius} {yAxisCoord}");
                                var exit = FindExitInProximity(theAgentNameInProximity.Key,
                                    theAgentNameInProximity.Value); // find exit in the proximity of the proximity agent
                                if (exit != string.Empty)
                                {
                                    Console.WriteLine(@"Found exit in proximity of agent " +
                                                      theAgentNameInProximity.Key);
                                    MoveToFoundExit(sender, position, exit);
                                    return;
                                }
                            }
                        }
                    }
                }

                // no exit found, no agent in proximity
                Console.WriteLine(@"Not found any exits or agents in proximity for " + sender);
                HandleChange(sender, position); // just make next move
            }
            catch (Exception e)
            {
               Console.WriteLine(@"At FindExitOrAgentInProximity " + e.Message);
            }
        }

        private string FindExitInProximity(string sender, string position)
        {
            var coord = position.ParseCoordinates();
            if (EvacuationAgentsPositions.Keys.Contains(sender))
            {
                for (var radius = 1; radius <= FieldOfViewAround; ++radius)
                {
                    for (var xAxisCoord = coord.X - radius; xAxisCoord <= coord.X + radius; xAxisCoord++)
                    {
                        if (ExitsPositions!.Values.Contains($@"{coord.Y - radius} {xAxisCoord}"))
                            return $@"{coord.Y - radius} {xAxisCoord}";

                        if (ExitsPositions!.Values.Contains($@"{coord.Y + radius} {xAxisCoord}"))
                            return $@"{coord.Y + radius} {xAxisCoord}";
                    }

                    for (var yAxisCoord = coord.Y - radius + 1; yAxisCoord <= coord.Y + radius - 1; yAxisCoord++)
                    {
                        if (ExitsPositions!.Values.Contains($@"{coord.X - radius} {yAxisCoord}"))
                            return $@"{coord.X - radius} {yAxisCoord}";

                        if (ExitsPositions!.Values.Contains($@"{coord.X + radius} {yAxisCoord}"))
                            return $@"{coord.X + radius} {yAxisCoord}";
                    }
                }
            }

            return string.Empty;
        }
        private void MoveToFoundExit(string sender, string currentPosition, string exitCoordinates)
        {
            Console.WriteLine(@$"Found exit at {exitCoordinates}!");
            EvacuationAgentsPositions[sender] = currentPosition; // update current position
            Send(sender, "moveToExit " + exitCoordinates);
        }

        private Dictionary<string, string> GetAgentsPositionExceptSender(string sender)
        {
            var agentsPositionExceptCurrent = new Dictionary<string, string>(EvacuationAgentsPositions);
            agentsPositionExceptCurrent.Remove(sender);

            return agentsPositionExceptCurrent;
        }

        private void NotifyAlarmIsStarted()
        {
            if (!_alertWasStarted)
            {
                _formGui.UpdatePlanetGUI(true);
                _alertWasStarted = true;
                _watch.Start();
            }
        }

        private void HandlePosition(string sender, string position)
        {
            if (EvacuationAgentsPositions.ContainsValue(position))
            {
                Send(sender, "block");
                return;
            }

            EvacuationAgentsPositions.Add(sender, position);
            Send(sender, "move");
        }

        private void HandleChange(string sender, string position)
        {
            if (position == _basePosition)
            {
                Send(sender, "block");
                return;
            }

            foreach (string k in EvacuationAgentsPositions.Keys)// for each evacuation in coordinator's database
            {
                if (k == sender)  // if current evacuation from coordinator's database is same as evacuation that have sent request to change
                    continue;
                if (EvacuationAgentsPositions[k] == position) // if evacuation wants to move on a position that another evacuation hold, send block
                {
                    Send(sender, "block");
                    return;
                }
            }

            foreach (var resourcePosition in ExitsPositions.Keys) // ignore exits
            {
                if (ExitsPositions[resourcePosition] == position)
                {
                    Send(sender, "block");
                    return;
                }
            }
            
            EvacuationAgentsPositions[sender] = position;

            if (_alertWasStarted && !_agentsNotifiedAboutAlarm.Contains(sender))
            {
                _agentsNotifiedAboutAlarm.Add(sender);
                Send(sender, "start_alert");
            }
            else
            {
                Send(sender, "move");
            }
        }
    }
}