using ActressMas;
using System;
using System.Collections.Generic;

namespace Reactive
{
    public class ExplorerAgent : Agent
    {
        private readonly Coordinates _coordinates = new();
        private State _state;
        private readonly Coordinates[] _lastTwoSteps = InitializeArray<Coordinates>(2); // [0] IS OLDEST, [1] IS THE FRESHEST
        private bool _firstIteration = true;
        private Coordinates _foundExitPosition;
            
        private enum State { EmergencyOff, EmergencyOn };

        // first build explorer agents
        public override void Setup()
        {
            Console.WriteLine(@"Starting " + Name);

            _coordinates.X = Utils.Size / 2;
            _coordinates.Y = Utils.Size / 2;
            _state = State.EmergencyOff;

            
            while (IsAtBase())
            {
                _coordinates.X = Utils.RandNoGen.Next(Utils.Size);
                _coordinates.Y = Utils.RandNoGen.Next(Utils.Size);
            }

            _lastTwoSteps[0].X = _coordinates.X;
            _lastTwoSteps[0].Y = _coordinates.Y;
            
            Send("planet", Utils.Str("position", _coordinates.X, _coordinates.Y));
        }

        private bool IsAtBase()
        {
            return (_coordinates.X == Utils.Size / 2 && _coordinates.Y == Utils.Size / 2); // the position of the base
        }

        public override void Act(Message message)
        {
            Console.WriteLine(@"\t[{1} -> {0}]: {2}", this.Name, message.Sender, message.Content);

            Utils.ParseMessage(message.Content, out var action, out List<string> parameters);

            //the agents, upon being notified of the emergency, will start moving in a random constant direction
            if (action == "start_alert")
            {
                _state = State.EmergencyOn;

                MoveRandomConstant();
                Send("planet", Utils.Str("findExitInMyArea", _coordinates.X, _coordinates.Y));
            }
            else if (action == "block")
            {
                CancelLastStepInHistory();
                // R1. If you detect an obstacle, then change direction
                MoveRandomly();
                Send("planet", Utils.Str("change", _coordinates.X, _coordinates.Y));
            }
            else if (action == "move")
            {
                if (_state == State.EmergencyOn)
                {
                    Console.WriteLine(@$"agent {this.Name} change command to move constant");
                    MoveRandomConstant();
                    Send("planet", Utils.Str("findExitInMyArea", _coordinates.X, _coordinates.Y));
                }
                else
                {
                    Console.WriteLine(@$"agent {this.Name} change command to move random");
                    MoveRandomly(); // move randomly ignoring exits
                    Send("planet", Utils.Str("change", _coordinates.X, _coordinates.Y));
                }
            }
            else if(action == "moveToExit")
            {
                MoveToExit(parameters.Count < 2
                    ? $"{_foundExitPosition.X} {_foundExitPosition.Y}"
                    : $"{parameters[0]} {parameters[1]}");
            }
            else if (action == "directionToExitBlocked")
            {
                MoveRandomlyBasedOnHistoryStep();
                Send("planet", Utils.Str("findExitInMyArea", _coordinates.X, _coordinates.Y));
            }
        }

        private void CancelLastStepInHistory()
        {
            _coordinates.X = _lastTwoSteps[0].X;
            _coordinates.Y = _lastTwoSteps[0].X;

            _lastTwoSteps[1].X = _coordinates.X;
            _lastTwoSteps[1].Y = _coordinates.Y;
        }

        private void UpdateStepsCoordinatesForX()
        {
            if (_firstIteration)
            {
                _lastTwoSteps[1].X = _coordinates.X;
                _lastTwoSteps[1].Y = _coordinates.Y;
                _firstIteration = false;
            }
            else
            {
                // old first step is removed as it's irrelevant
                _lastTwoSteps[0].X = _lastTwoSteps[1].X;
                _lastTwoSteps[0].Y = _lastTwoSteps[1].Y;

                _lastTwoSteps[1].X = _coordinates.X;                
            }
        }
        
        private void MoveToExit(string exitCoordinates) // this should be resolved!!!!!!!!!!!!!!!!!!!!!!!! it moves just forward, but it does not take into account the obstacles
        {
            var exitPosition = exitCoordinates.ParseCoordinates();
            _foundExitPosition = exitPosition;
            if (_coordinates.X > exitPosition.X)
                --_coordinates.X;
            else if(_coordinates.X < exitPosition.X)
                ++_coordinates.X;
            
            UpdateStepsCoordinatesForX();
            
            
            if (_coordinates.Y > exitPosition.Y)
                --_coordinates.Y;
            else if(_coordinates.Y < exitPosition.Y)
                ++_coordinates.Y;
            
            UpdateStepsCoordinatesForY();
            Send("planet", Utils.Str("nextMoveToExit", _coordinates.X, _coordinates.Y));  
        }
        
        private void UpdateStepsCoordinatesForY()
        {
            if (_firstIteration)
            {
                _lastTwoSteps[1].X = _coordinates.X;
                _lastTwoSteps[1].Y = _coordinates.Y;
                _firstIteration = false;
            }
            else
            {
                // old first step is removed as it's irrelevant
                _lastTwoSteps[0].X = _lastTwoSteps[1].X;
                _lastTwoSteps[0].Y = _lastTwoSteps[1].Y;

                _lastTwoSteps[1].Y = _coordinates.Y;                
            }
        }
        
        private void MoveRandomly()
        {
            int d = Utils.RandNoGen.Next(4);
            switch (d)
            {
                case 0:
                    if (_coordinates.X > 0)
                    {
                        _coordinates.X--;
                        UpdateStepsCoordinatesForX();
                    }

                    break;
                case 1:
                    if (_coordinates.X < Utils.Size - 1)
                    {
                        _coordinates.X++;
                        UpdateStepsCoordinatesForX();
                    }

                    break;
                case 2:
                    if (_coordinates.Y > 0)
                    {
                        _coordinates.Y--;
                        UpdateStepsCoordinatesForY();
                    }

                    break;
                case 3:
                    if (_coordinates.Y < Utils.Size - 1)
                    {
                        _coordinates.Y++;
                        UpdateStepsCoordinatesForY();
                    }

                    break;
            }
        }

        private void MoveRandomlyBasedOnHistoryStep() // on findExitInMyArea if is blocked
        {
            if (_firstIteration) // less probable to happen
            {
                int d = Utils.RandNoGen.Next(4);
                switch (d)
                {
                    case 0:
                        if (_lastTwoSteps[1].X > 0)
                        {
                            _coordinates.X--;
                            _lastTwoSteps[1].X = _coordinates.X;
                        }
                        break;
                    case 1:
                        if (_lastTwoSteps[1].X < Utils.Size - 1)
                        {
                            _coordinates.X++; 
                            _lastTwoSteps[1].X = _coordinates.X;
                        }

                        break;
                    case 2:
                        if (_lastTwoSteps[1].Y > 0)
                        {
                            _coordinates.Y--; 
                            _lastTwoSteps[1].Y = _coordinates.Y;
                        }
                        break;
                    case 3:
                        if (_lastTwoSteps[1].Y < Utils.Size - 1)
                        {
                            _coordinates.Y++;
                            _lastTwoSteps[1].Y = _coordinates.Y;
                        }

                        break;
                }
            }
            else // last step was wrong, try change direction and try again
            {
                _coordinates.X = _lastTwoSteps[0].X;
                _coordinates.Y = _lastTwoSteps[0].Y;
                
                var d = Utils.RandNoGen.Next(4);
                switch (d)
                {
                    // Note: old step [0] will remain as it was. update new one [1]
                    case 0:
                        if (_lastTwoSteps[0].X > 0)
                        {
                            _coordinates.X--;
                            _lastTwoSteps[1].X = _coordinates.X;
                        }

                        break;
                    case 1:
                        if (_lastTwoSteps[0].X < Utils.Size - 1)
                        {
                            _coordinates.X++; 
                            _lastTwoSteps[1].X = _coordinates.X;
                        }
                        break;
                    case 2:
                        if (_lastTwoSteps[0].Y > 0)
                        {
                            _coordinates.Y--; 
                            _lastTwoSteps[1].Y = _coordinates.Y;
                        } 
                        break;
                    case 3:
                        if (_lastTwoSteps[0].Y < Utils.Size - 1)
                        {
                            _coordinates.Y++; 
                            _lastTwoSteps[1].Y = _coordinates.Y;
                        } 
                        break;
                }
            }
        }
        
        private void MoveOnAxis(bool axis) // true = X, false = Y
        {
            if (axis)
            {
                if (_coordinates.X > 0)
                {
                    _coordinates.X--;
                    UpdateStepsCoordinatesForX();
                    return;
                }

                if (_coordinates.X < Utils.Size - 1)
                {
                    _coordinates.X++;
                    UpdateStepsCoordinatesForX();
                    return;
                }
            }
            else
            {
                if (_coordinates.Y > 0)
                {
                    _coordinates.Y--;
                    UpdateStepsCoordinatesForY();
                    return;
                }

                if (_coordinates.Y < Utils.Size - 1)
                {
                    _coordinates.Y++;
                    UpdateStepsCoordinatesForY();
                    return;
                }
            }
        }

        private void MoveRandomConstant()
        {
            // get two last points and generate third
            if (_lastTwoSteps[0].X == _lastTwoSteps[1].X) // moving on Y only
            {
                if (_lastTwoSteps[1].Y > _lastTwoSteps[0].Y) // if sense is from up to down on the greed
                {
                    if (_coordinates.Y < Utils.Size - 1) // if is not on the margin of greed
                    {
                        _coordinates.Y++;
                        UpdateStepsCoordinatesForY();
                    }
                    else
                    {
                        MoveOnAxis(true); //change direction random except one you came from
                    }
                    return;
                }
                else if(_lastTwoSteps[1].Y < _lastTwoSteps[0].Y)//if sense is from down to up on the greed
                {
                    if (_coordinates.Y > 0)
                    {
                        _coordinates.Y--;
                        UpdateStepsCoordinatesForY();
                    }
                    else // you're on greed's margin. change direction
                    {
                        MoveOnAxis(true); //change direction random except one you came from
                    }
                    return;
                }
            }
            else if (_lastTwoSteps[0].Y == _lastTwoSteps[1].Y) // moving on X only
            {
                if (_lastTwoSteps[1].X > _lastTwoSteps[0].X) // if sense is from left to the right
                {
                    if (_coordinates.X < Utils.Size - 1) // if is not on the margin of greed
                    {
                        _coordinates.X++;
                        UpdateStepsCoordinatesForX();
                    }
                    else
                    {
                        MoveOnAxis(false); //change direction random except one you came from
                    }
                    
                    return;
                }
                else if (_lastTwoSteps[1].X < _lastTwoSteps[0].X) // if sense is from right to the left
                {
                    if (_coordinates.X > 0) // that means LastTwoSteps[1].X ?
                    {
                        _coordinates.X--;
                        UpdateStepsCoordinatesForX();
                    }
                    else // you're on greed's margin. change direction
                    {
                        MoveOnAxis(false); //change direction random except one you came from
                    }
                    
                    return;
                }
            }
            else if (_lastTwoSteps[0].X == _lastTwoSteps[0].Y && _lastTwoSteps[1].X == _lastTwoSteps[1].Y) // diagonal movement
            {
            }
            
            if (_lastTwoSteps[0].X == _lastTwoSteps[1].X && _lastTwoSteps[0].Y == _lastTwoSteps[1].Y) // Handle the blocker
            {
            }
            
            MoveRandomly();
        }
        
        private static T[] InitializeArray<T>(int length) where T : new()
        {
            var array = new T[length];
            for (var i = 0; i < length; ++i)
            {
                array[i] = new T();
            }

            return array;
        }
    }
}