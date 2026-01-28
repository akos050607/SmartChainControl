using SmartChainControl.Shared.Models;

namespace SmartChainControl.Server.Services;

public class SimulationManager
{
    public List<Robot> Robots { get; private set; } = new();
    public MapInfo Map { get; private set; }

    private Pathfinder _pathfinder;
    private readonly Random _random = new();
    private const int MapWidth = 20;
    private const int MapHeight = 20;

    private class RobotInternalState
    {
        public int ChargingTimer { get; set; } = 0;
        public (int X, int Y) HomePosition { get; set; }
        public double DrainRate { get; set; }
        public bool IsManual { get; set; } = false;
    }
    private Dictionary<int, RobotInternalState> _internalStates = new();

    private readonly string[] _robotColors = new[] {
        "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#00FFFF", "#FF00FF"
    };

    private Dictionary<int, (int X, int Y)> _dropOffZones = new();

    public SimulationManager()
    {
        Map = GenerateWarehouseMap();
        _pathfinder = new Pathfinder(Map);

        // Initialize 5 robots with unique colors, home positions and battery drain rates
        for (int i = 0; i < 5; i++)
        {
            int startY = 2 + (i * 3);
            _dropOffZones.Add(i + 1, (0, startY));
            double randomDrain = 0.01 + (_random.NextDouble() * 0.02);

            _internalStates.Add(i + 1, new RobotInternalState
            {
                HomePosition = (0, startY),
                DrainRate = randomDrain
            });

            Robots.Add(new Robot
            {
                Id = i + 1,
                X = 0,
                Y = startY,
                ColorHex = _robotColors[i % _robotColors.Length],
                State = "Idle",
                BatteryLevel = 100.0,
                CurrentPath = new List<Position>(),
                PatienceThreshold = _random.Next(5, 15),

            });
        }
    }

    // Generate warehouse layout with shelf grid pattern
    private MapInfo GenerateWarehouseMap()
    {
        var map = new MapInfo { Width = MapWidth, Height = MapHeight };
        for (int x = 4; x < MapWidth - 2; x += 3)
        {
            for (int y = 2; y < MapHeight - 2; y++)
            {
                if (y == 10) continue;
                map.Obstacles.Add(new Obstacle { X = x, Y = y, Type = "Shelf" });
            }
        }
        return map;
    }
    private void RecalculatePath(Robot robot)
    {
        var path = _pathfinder.FindPath(
            (int)Math.Round(robot.X), 
            (int)Math.Round(robot.Y), 
            (int)robot.TargetX, 
            (int)robot.TargetY
        );
        robot.CurrentPath = path ?? new List<Position>();
    }

    public void ToggleManualMode(int robotId, bool enable)
    {
        var robot = Robots.FirstOrDefault(r => r.Id == robotId);
        if (robot == null) return;
        var state = _internalStates[robotId];

        if (enable)
        {
            state.IsManual = true;
            robot.State = "Manual";
            robot.CurrentPath = new List<Position>();
        }
        else
        {
            state.IsManual = false;

            if (robot.HasCargo)
            {
                robot.State = "ToExit";
                robot.TargetX = state.HomePosition.X;
                robot.TargetY = state.HomePosition.Y;
                RecalculatePath(robot);
            }
            else if (robot.CurrentTargetNode != null)
            {
                var shelfX = (int)robot.CurrentTargetNode.X;
                var shelfY = (int)robot.CurrentTargetNode.Y;
                var entryPoint = GetWalkableNeighbor(shelfX, shelfY);

                if (entryPoint != null)
                {
                    robot.State = "ToShelf";
                    robot.TargetX = entryPoint.X;
                    robot.TargetY = entryPoint.Y;
                    RecalculatePath(robot);
                }
                else
                {
                    robot.State = "Idle";
                    robot.CurrentTargetNode = null;
                }
            }
            else
            {
                robot.State = "Idle";
            }
        }
    }

    public void SetManualTarget(int robotId, int x, int y)
    {
        var robot = Robots.FirstOrDefault(r => r.Id == robotId);
        if (robot == null) return;
        var state = _internalStates[robotId];

        state.IsManual = true;
        robot.State = "Manual";
        robot.TargetX = x;
        robot.TargetY = y;
        var path = _pathfinder.FindPath((int)Math.Round(robot.X), (int)Math.Round(robot.Y), x, y);
        robot.CurrentPath = path ?? new List<Position>();
    }
    public void Update()
    {
        var occupiedCells = new HashSet<(int, int)>();
        foreach (var r in Robots) 
        {
            occupiedCells.Add(((int)Math.Round(r.X), (int)Math.Round(r.Y)));
        }

        foreach (var robot in Robots)
        {
            var internalState = _internalStates[robot.Id];

            if (internalState.IsManual)
            {
                robot.BatteryLevel -= internalState.DrainRate;
                if (robot.BatteryLevel < 0) robot.BatteryLevel = 0;
                MoveRobot(robot, occupiedCells);
                continue; 
            }

            if (robot.State != "Charging")
            {
                robot.BatteryLevel -= internalState.DrainRate;
                if (robot.BatteryLevel < 0) robot.BatteryLevel = 0;

                if (robot.BatteryLevel < 30 && robot.State != "Returning" && robot.State != "ToExit" && robot.State != "Manual")
                {
                    robot.HasCargo = false; 
                    robot.State = "Returning";
                    robot.CurrentTargetNode = null;
                    robot.TargetX = internalState.HomePosition.X;
                    robot.TargetY = internalState.HomePosition.Y;
                    RecalculatePath(robot);
                }
            }

            if (robot.State == "Charging")
            {
                internalState.ChargingTimer++;
                robot.BatteryLevel = Math.Min(100, 30 + ((double)internalState.ChargingTimer / 1200.0 * 70));

                if (internalState.ChargingTimer >= 1200) 
                {
                    robot.BatteryLevel = 100;
                    robot.State = "Idle";
                    internalState.ChargingTimer = 0;
                }
                continue; 
            }
            
            else if (robot.State == "Returning" && IsPathFinished(robot))
            {
                if (IsAtPosition(robot, internalState.HomePosition.X, internalState.HomePosition.Y))
                {
                    robot.State = "Charging";
                    internalState.ChargingTimer = 0;
                }
                else
                {
                    RecalculatePath(robot);
                }
            }
            
            else if (robot.State == "Idle" && robot.BatteryLevel > 30)
            {
                var shelf = GetFreeRandomShelf(robot.Id);
                if (shelf != null)
                {
                    var entryPoint = GetWalkableNeighbor(shelf.X, shelf.Y);
                    if (entryPoint != null)
                    {
                        robot.CurrentTargetNode = new Position { X = shelf.X, Y = shelf.Y };
                        robot.TargetX = entryPoint.X;
                        robot.TargetY = entryPoint.Y;
                        robot.StuckTicks = 0;
                        robot.PatienceThreshold = _random.Next(5, 20); 
                        
                        robot.State = "ToShelf";
                        RecalculatePath(robot);
                    }
                }
            }
            
            else if (robot.State == "ToShelf" && IsPathFinished(robot))
            {
                if (robot.CurrentTargetNode != null && 
                    IsAtPosition(robot, robot.TargetX, robot.TargetY))
                {
                    robot.State = "Loading";
                    robot.HasCargo = true;
                    
                    robot.TargetX = internalState.HomePosition.X;
                    robot.TargetY = internalState.HomePosition.Y;
                    robot.StuckTicks = 0;

                    robot.State = "ToExit";
                    RecalculatePath(robot);
                }
                else
                {
                    robot.State = "Idle";
                    robot.CurrentTargetNode = null;
                }
            }
            
            else if (robot.State == "ToExit" && IsPathFinished(robot))
            {
                if (IsAtPosition(robot, internalState.HomePosition.X, internalState.HomePosition.Y))
                {
                    robot.HasCargo = false;
                    robot.State = "Idle"; 
                }
                else
                {
                    RecalculatePath(robot);
                }
            }

            MoveRobot(robot, occupiedCells);
        }
    }

    private bool IsAtPosition(Robot robot, float targetX, float targetY)
    {
        return Math.Abs(robot.X - targetX) < 1.0f && Math.Abs(robot.Y - targetY) < 1.0f;
    }

    private void MoveRobot(Robot robot, HashSet<(int, int)> occupiedCells)
    {
        while (robot.CurrentPath != null && robot.CurrentPath.Count > 0)
        {
            var nextNode = robot.CurrentPath[0];
            if ((int)nextNode.X == (int)Math.Round(robot.X) && (int)nextNode.Y == (int)Math.Round(robot.Y))
            {
                robot.CurrentPath.RemoveAt(0);
            }
            else break;
        }

        if (robot.CurrentPath == null || robot.CurrentPath.Count == 0) return;

        var target = robot.CurrentPath[0];
        var targetCell = ((int)target.X, (int)target.Y);

        bool isBlocked = occupiedCells.Contains(targetCell);

        if (isBlocked)
        {
            robot.StuckTicks++;
            // Recalculate path after waiting, treating other robots as obstacles
            if (robot.StuckTicks > robot.PatienceThreshold)
            {
                var otherRobotsAsObstacles = new HashSet<(int, int)>(occupiedCells);
                var newPath = _pathfinder.FindPath((int)Math.Round(robot.X), (int)Math.Round(robot.Y), (int)robot.TargetX, (int)robot.TargetY, otherRobotsAsObstacles);

                if (newPath != null)
                {
                    robot.CurrentPath = newPath;
                    robot.StuckTicks = 0;
                    robot.PatienceThreshold = _random.Next(5, 15);
                }
                else
                {
                    robot.PatienceThreshold += 5;
                }
            }
        }
        else
        {
            robot.StuckTicks = 0;
            float speed = 0.13f;
            float dx = target.X - robot.X;
            float dy = target.Y - robot.Y;

            if (Math.Abs(dx) <= speed && Math.Abs(dy) <= speed)
            {
                robot.X = target.X;
                robot.Y = target.Y;
            }
            else
            {
                robot.X += Math.Sign(dx) * speed;
                robot.Y += Math.Sign(dy) * speed;
            }
            occupiedCells.Add(targetCell);
            occupiedCells.Add(((int)Math.Round(robot.X), (int)Math.Round(robot.Y)));
        }
    }

    private bool IsPathFinished(Robot robot)
    {
        return robot.CurrentPath == null || robot.CurrentPath.Count == 0;
    }

    private Obstacle? GetFreeRandomShelf(int myRobotId)
    {
        var shelves = Map.Obstacles.Where(o => o.Type == "Shelf").ToList();
        shelves = shelves.OrderBy(x => _random.Next()).ToList();

        foreach (var shelf in shelves)
        {
            bool isTaken = Robots.Any(r => r.Id != myRobotId &&
                                         r.CurrentTargetNode != null &&
                                         (int)r.CurrentTargetNode.X == shelf.X &&
                                         (int)r.CurrentTargetNode.Y == shelf.Y);

            if (!isTaken) return shelf;
        }
        return null;
    }

    private Position? GetWalkableNeighbor(int targetX, int targetY)
    {
        var neighbors = new List<(int x, int y)>
        {
            (targetX - 1, targetY), (targetX + 1, targetY)
        };
        neighbors = neighbors.OrderBy(x => _random.Next()).ToList();

        foreach (var n in neighbors)
        {
            if (n.x < 0 || n.x >= MapWidth || n.y < 0 || n.y >= MapHeight) continue;
            bool isObstacle = Map.Obstacles.Any(o => o.X == n.x && o.Y == n.y);
            if (!isObstacle) return new Position { X = n.x, Y = n.y };
        }
        return null;
    }
}