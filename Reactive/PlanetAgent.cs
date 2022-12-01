using ActressMas;
using Message = ActressMas.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Reactive
{
    public class PlanetAgent : Agent
    {
        private PlanetForm _formGui;
        public Dictionary<string, string> ExplorerPositions { get; set; }
        public Dictionary<string, string> ResourcePositions { get; set; } // aka exits position
        private readonly string _basePosition; // position of "special agent"
        private bool _alertWasStarted;
        private const int FieldOfViewAround = 2; // this mean 3x3
        private readonly List<string> _agentsNotifiedAboutAlarm = new();

        public PlanetAgent()
        {
            ExplorerPositions = new Dictionary<string, string>();
            ResourcePositions = new Dictionary<string, string>();
            _basePosition = Utils.Str(Utils.Size / 2, Utils.Size / 2);

            var t = new Thread(GuiThread);
            t.Start();
        }

        private void GuiThread()
        {
            _formGui = new PlanetForm();
            _formGui.SetOwner(this);
            _formGui.ShowDialog();
            Application.Run();
        }

        // second build resources
        public override void Setup()
        {
            Console.WriteLine(@"Starting " + Name);

            List<string> resPos = new List<string>();
            string compPos = Utils.Str(Utils.Size / 2, Utils.Size / 2);
            resPos.Add(compPos); // the position of the base

            for (int i = 1; i <= Utils.NoResources; i++)
            {
                while (resPos.Contains(compPos)) // resources do not overlap
                {
                    int x = Utils.RandNoGen.Next(Utils.Size);
                    int y = Utils.RandNoGen.Next(Utils.Size);
                    compPos = Utils.Str(x, y);
                }

                ResourcePositions.Add("res" + i, compPos); // exits position
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
            
            foreach (var k in ExplorerPositions.Keys)// for each explorer in planet's database
            {
                if (k == sender)  // if current explorer from planet's database is same as explorer that have sent request to change
                    continue;
                if (ExplorerPositions[k] == position) // if explorer wants to move on a position that another explorer hold, send block
                {
                    Send(sender, "directionToExitBlocked");
                    return true;
                }
            }
            
            ExplorerPositions[sender] = position;
            
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

                foreach (string k in ExplorerPositions.Keys)
                {
                    if (k == sender)
                        continue;
                    if (ExplorerPositions[k] == position)
                    {
                        Send(sender, "block");
                        return;
                    }
                }
                
                ExplorerPositions[sender] = position;

                if (ResourcePositions!.Values.Contains(position) && position != _basePosition) // if position is already on exit, remove agent
                {
                    _formGui.UpdatePlanetGUI();
                    Console.WriteLine(@$"Agent {sender} is out from {position}");
                    ExplorerPositions.Remove(sender);
                    Environment.Remove(sender);
                    if (Environment.NoAgents == 1)
                    {
                        this.Stop();
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

                if (ExplorerPositions.Keys.Contains(sender))
                {
                    for (var radius = 1; radius <= FieldOfViewAround; ++radius)
                    {
                        for (var xAxisCoord = coord.X - radius; xAxisCoord <= coord.X + radius; xAxisCoord++)
                        {
                            if (ResourcePositions!.Values.Contains(
                                    $@"{xAxisCoord} {coord.Y - radius}")) // search for exit on this axis
                            {
                                var exitCoordinates = $@"{xAxisCoord} {coord.Y - radius}";
                                FoundExitAndMoveTo(sender, position, exitCoordinates);
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
                                    FoundExitAndMoveTo(sender, position, exit);
                                    return;
                                }
                            }

                            if (ResourcePositions!.Values.Contains($@"{xAxisCoord} {coord.Y + radius}"))
                            {
                                var exitCoordinates = $@"{xAxisCoord} {coord.Y + radius}";
                                FoundExitAndMoveTo(sender, position, exitCoordinates);
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
                                    FoundExitAndMoveTo(sender, position, exit);
                                    return;
                                }
                            }
                        }

                        for (var yAxisCoord = coord.Y - radius + 1; yAxisCoord <= coord.Y + radius - 1; yAxisCoord++)
                        {
                            if (ResourcePositions!.Values.Contains($@"{coord.X - radius} {yAxisCoord}"))
                            {
                                var exitCoordinates = $@"{coord.X - radius} {yAxisCoord}";
                                FoundExitAndMoveTo(sender, position, exitCoordinates);
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
                                    FoundExitAndMoveTo(sender, position, exit);
                                    return;
                                }
                            }

                            if (ResourcePositions!.Values.Contains($@"{coord.X + radius} {yAxisCoord}"))
                            {
                                var exitCoordinates = $@"{coord.X + radius} {yAxisCoord}";
                                FoundExitAndMoveTo(sender, position, exitCoordinates);
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
                                    FoundExitAndMoveTo(sender, position, exit);
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
            if (ExplorerPositions.Keys.Contains(sender))
            {
                for (var radius = 1; radius <= FieldOfViewAround; ++radius)
                {
                    for (var xAxisCoord = coord.X - radius; xAxisCoord <= coord.X + radius; xAxisCoord++)
                    {
                        if (ResourcePositions!.Values.Contains($@"{coord.Y - radius} {xAxisCoord}"))
                            return $@"{coord.Y - radius} {xAxisCoord}";

                        if (ResourcePositions!.Values.Contains($@"{coord.Y + radius} {xAxisCoord}"))
                            return $@"{coord.Y + radius} {xAxisCoord}";
                    }

                    for (var yAxisCoord = coord.Y - radius + 1; yAxisCoord <= coord.Y + radius - 1; yAxisCoord++)
                    {
                        if (ResourcePositions!.Values.Contains($@"{coord.X - radius} {yAxisCoord}"))
                            return $@"{coord.X - radius} {yAxisCoord}";

                        if (ResourcePositions!.Values.Contains($@"{coord.X + radius} {yAxisCoord}"))
                            return $@"{coord.X + radius} {yAxisCoord}";
                    }
                }
            }

            return string.Empty;
        }
        private void FoundExitAndMoveTo(string sender, string currentPosition, string exitCoordinates)
        {
            Console.WriteLine(@$"Found exit at {exitCoordinates}!");
            ExplorerPositions[sender] = currentPosition; // update current position
            Send(sender, "moveToExit " + exitCoordinates);
        }

        private Dictionary<string, string> GetAgentsPositionExceptSender(string sender)
        {
            var agentsPositionExceptCurrent = new Dictionary<string, string>(ExplorerPositions);
            agentsPositionExceptCurrent.Remove(sender);

            return agentsPositionExceptCurrent;
        }

        private void NotifyAlarmIsStarted()
        {
            if (!_alertWasStarted)
            {
                _formGui.UpdatePlanetGUI(true);
                _alertWasStarted = true;
            }
        }

        private void HandlePosition(string sender, string position)
        {
            if (ExplorerPositions.ContainsValue(position))
            {
                Send(sender, "block");
                return;
            }

            ExplorerPositions.Add(sender, position);
            Send(sender, "move");
        }

        private void HandleChange(string sender, string position)
        {

            if (position == _basePosition)
            {
                Send(sender, "block");
                return;
            }

            foreach (string k in ExplorerPositions.Keys)// for each explorer in planet's database
            {
                if (k == sender)  // if current explorer from planet's database is same as explorer that have sent request to change
                    continue;
                if (ExplorerPositions[k] == position) // if explorer wants to move on a position that another explorer hold, send block
                {
                    Send(sender, "block");
                    return;
                }
            }

            foreach (var resourcePosition in ResourcePositions.Keys) // ignore exits
            {
                if (ResourcePositions[resourcePosition] == position)
                {
                    Send(sender, "block");
                    return;
                }
            }
            
            ExplorerPositions[sender] = position;

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