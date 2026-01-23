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

    private readonly string[] _robotColors = new[] { 
        "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#00FFFF", "#FF00FF" 
    };

    public SimulationManager()
    {
        Map = GenerateWarehouseMap();
        _pathfinder = new Pathfinder(Map);

        for (int i = 0; i < 5; i++)
        {
            Robots.Add(new Robot
            {
                Id = i + 1,
                X = 0, 
                Y = (i + 1) * 2,
                ColorHex = _robotColors[i % _robotColors.Length],
                State = "Idle",
                CurrentPath = new List<Position>(),
                PatienceThreshold = _random.Next(5, 15)
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

    // Main simulation update tick - handles robot state machines and movement
    public void Update()
    {
        // Collect current robot positions for collision detection
        var occupiedCells = new HashSet<(int, int)>();
        foreach (var r in Robots) 
        {
            occupiedCells.Add(((int)Math.Round(r.X), (int)Math.Round(r.Y)));
        }

        foreach (var robot in Robots)
        {
            // Robot state machine: Idle -> ToShelf -> Loading -> ToExit
            if (robot.State == "Idle")
            {
                var shelf = GetRandomShelf();
                var entryPoint = GetWalkableNeighbor(shelf.X, shelf.Y);

                if (entryPoint != null)
                {
                    robot.CurrentTargetNode = new Position { X = shelf.X, Y = shelf.Y };
                    robot.TargetX = entryPoint.X;
                    robot.TargetY = entryPoint.Y;
                    robot.StuckTicks = 0;
                    
                    robot.PatienceThreshold = _random.Next(5, 20); 

                    var path = _pathfinder.FindPath((int)Math.Round(robot.X), (int)Math.Round(robot.Y), (int)robot.TargetX, (int)robot.TargetY);
                    
                    if (path != null && path.Count > 0)
                    {
                        robot.CurrentPath = path;
                        robot.State = "ToShelf";
                    }
                }
            }
            else if (robot.State == "ToShelf" && IsPathFinished(robot))
            {
                robot.State = "Loading";
                robot.HasCargo = true;
                robot.TargetX = 0;
                robot.TargetY = robot.Id * 2;
                robot.CurrentTargetNode = null;
                robot.StuckTicks = 0;

                var path = _pathfinder.FindPath((int)Math.Round(robot.X), (int)Math.Round(robot.Y), (int)robot.TargetX, (int)robot.TargetY);
                robot.CurrentPath = path ?? new List<Position>();
                robot.State = "ToExit";
            }
            else if (robot.State == "ToExit" && IsPathFinished(robot))
            {
                robot.HasCargo = false;
                robot.State = "Idle";
            }

            // Move robot along path with collision avoidance
            MoveRobot(robot, occupiedCells);
        }
    }

    private void MoveRobot(Robot robot, HashSet<(int, int)> occupiedCells)
    {
        // Remove already-reached waypoints from path
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
        
        bool isBlocked = occupiedCells.Contains(((int)target.X, (int)target.Y));

        if (isBlocked)
        {
            robot.StuckTicks++;

            // Replan path after patience threshold exceeded
            if (robot.StuckTicks > robot.PatienceThreshold)
            {
                var otherRobotsAsObstacles = new HashSet<(int, int)>(occupiedCells);
                
                var newPath = _pathfinder.FindPath(
                    (int)Math.Round(robot.X), 
                    (int)Math.Round(robot.Y), 
                    (int)robot.TargetX, 
                    (int)robot.TargetY, 
                    otherRobotsAsObstacles
                );

                if (newPath != null)
                {
                    robot.CurrentPath = newPath;
                    robot.StuckTicks = 0;
                    robot.PatienceThreshold = _random.Next(5, 15);
                }
                else
                {
                    // No path found, increase patience and wait
                    robot.PatienceThreshold += 5;
                }
            }
        }
        else
        {
            // Path is clear, move robot toward next waypoint
            robot.StuckTicks = 0;

            occupiedCells.Remove(((int)Math.Round(robot.X), (int)Math.Round(robot.Y)));

            float speed = 0.2f;
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

            occupiedCells.Add(((int)Math.Round(robot.X), (int)Math.Round(robot.Y)));
        }
    }

    private bool IsPathFinished(Robot robot)
    {
        return robot.CurrentPath == null || robot.CurrentPath.Count == 0;
    }

    private Obstacle GetRandomShelf()
    {
        var shelves = Map.Obstacles.Where(o => o.Type == "Shelf").ToList();
        if (shelves.Count == 0) return new Obstacle { X = 5, Y = 5 };
        return shelves[_random.Next(shelves.Count)];
    }

    private Position? GetWalkableNeighbor(int targetX, int targetY)
    {
        var neighbors = new List<(int x, int y)> 
        { 
            (targetX - 1, targetY), 
            (targetX + 1, targetY) 
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