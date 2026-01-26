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
    }
    
    private Dictionary<int, RobotInternalState> _internalStates = new();

    private readonly string[] _robotColors = new[] { 
        "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#00FFFF", "#FF00FF" 
    };

    public SimulationManager()
    {
        Map = GenerateWarehouseMap();
        _pathfinder = new Pathfinder(Map);

        for (int i = 0; i < 5; i++)
        {
            int startY = 2 + (i * 3);
            _internalStates.Add(i + 1, new RobotInternalState { HomePosition = (0, startY) });

            Robots.Add(new Robot
            {
                Id = i + 1,
                X = 0, 
                Y = startY, 
                ColorHex = _robotColors[i % _robotColors.Length],
                State = "Idle",
                BatteryLevel = 100.0,
                CurrentPath = new List<Position>(),
                PatienceThreshold = 10
            });
        }
    }

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
            if (robot.State != "Charging")
            {
                robot.BatteryLevel -= 0.03;
                if (robot.BatteryLevel < 0) robot.BatteryLevel = 0;
                if (robot.BatteryLevel < 30 && robot.State != "Returning")
                {
                    robot.HasCargo = false; 
                    robot.State = "Returning";
                    robot.CurrentTargetNode = null;
                    
                    robot.TargetX = internalState.HomePosition.X;
                    robot.TargetY = internalState.HomePosition.Y;
                    var path = _pathfinder.FindPath((int)Math.Round(robot.X), (int)Math.Round(robot.Y), (int)robot.TargetX, (int)robot.TargetY);
                    robot.CurrentPath = path ?? new List<Position>();
                }
            }

            if (robot.State == "Charging")
            {
                internalState.ChargingTimer++;
                
                double chargeProgress = (double)internalState.ChargingTimer / 1200.0;
                robot.BatteryLevel = Math.Min(100, 30 + (chargeProgress * 70));

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
                robot.State = "Charging";
                internalState.ChargingTimer = 0;
            }
            else if (robot.State == "Idle")
            {
                if (robot.BatteryLevel > 30) 
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
                            
                            var path = _pathfinder.FindPath((int)Math.Round(robot.X), (int)Math.Round(robot.Y), (int)robot.TargetX, (int)robot.TargetY);
                            if (path != null && path.Count > 0)
                            {
                                robot.CurrentPath = path;
                                robot.State = "ToShelf";
                            }
                        }
                    }
                }
            }
            else if (robot.State == "ToShelf" && IsPathFinished(robot))
            {
                robot.State = "Loading";
                robot.HasCargo = true;
                
                robot.TargetX = internalState.HomePosition.X;
                robot.TargetY = internalState.HomePosition.Y;
                
                robot.CurrentTargetNode = null; 
                var path = _pathfinder.FindPath((int)Math.Round(robot.X), (int)Math.Round(robot.Y), (int)robot.TargetX, (int)robot.TargetY);
                robot.CurrentPath = path ?? new List<Position>();
                robot.State = "ToExit";
            }
            else if (robot.State == "ToExit" && IsPathFinished(robot))
            {
                robot.HasCargo = false;
                robot.State = "Idle"; 
            }

            MoveRobot(robot, occupiedCells);
        }
    }

    private void MoveRobot(Robot robot, HashSet<(int, int)> occupiedCells)
    {
        if (robot.CurrentPath == null || robot.CurrentPath.Count == 0) return;

        while (robot.CurrentPath.Count > 0)
        {
            var nextNode = robot.CurrentPath[0];
            if ((int)nextNode.X == (int)Math.Round(robot.X) && (int)nextNode.Y == (int)Math.Round(robot.Y))
            {
                robot.CurrentPath.RemoveAt(0);
            }
            else break;
        }

        if (robot.CurrentPath.Count == 0) return;

        var target = robot.CurrentPath[0];
        var targetCell = ((int)target.X, (int)target.Y);
        
        bool isBlocked = occupiedCells.Contains(targetCell);

        if (!isBlocked)
        {
            float speed = 0.08f;
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

    private bool IsPathFinished(Robot robot) => robot.CurrentPath == null || robot.CurrentPath.Count == 0;

    private Obstacle? GetFreeRandomShelf(int myRobotId)
    {
        var shelves = Map.Obstacles.Where(o => o.Type == "Shelf").ToList();
        shelves = shelves.OrderBy(x => _random.Next()).ToList();
        foreach (var shelf in shelves)
        {
            bool isTaken = Robots.Any(r => r.Id != myRobotId && r.CurrentTargetNode != null && (int)r.CurrentTargetNode.X == shelf.X && (int)r.CurrentTargetNode.Y == shelf.Y);
            if (!isTaken) return shelf;
        }
        return null;
    }

    private Position? GetWalkableNeighbor(int targetX, int targetY)
    {
        var neighbors = new List<(int x, int y)> { (targetX - 1, targetY), (targetX + 1, targetY) };
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