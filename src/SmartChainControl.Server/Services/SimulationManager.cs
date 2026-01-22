using SmartChainControl.Shared.Models;
using System.Collections.Concurrent;

namespace SmartChainControl.Server.Services;

public class SimulationManager
{
    public List<Robot> Robots { get; private set; } = new();
    public MapInfo Map { get; private set; }

    private Pathfinder _pathfinder;
    private readonly Random _random = new();
    private const int MapWidth = 20;
    private const int MapHeight = 20;

    public SimulationManager()
    {
        Map = GenerateWarehouseMap();
        _pathfinder = new Pathfinder(Map);

        for (int i = 1; i <= 5; i++)
        {
            Robots.Add(new Robot
            {
                Id = i,
                X = 0, Y = i * 2,
                TargetX = _random.Next(0, MapWidth),
                TargetY = _random.Next(0, MapHeight),
                ColorHex = GetRandomNeonColor(),
                State = "Idle"
            });
        }
    }

    private MapInfo GenerateWarehouseMap()
    {
        var map = new MapInfo { Width = MapWidth, Height = MapHeight };

        for (int x = 2; x < MapWidth - 2; x += 3)
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
    foreach (var r in Robots) occupiedCells.Add(((int)r.X, (int)r.Y));

    foreach (var robot in Robots)
    {
        if (robot.State == "Idle")
        {
            var shelf = GetRandomShelf();
            robot.TargetX = shelf.X;
            robot.TargetY = shelf.Y;
            robot.CurrentTargetNode = new Position { X = shelf.X, Y = shelf.Y };
            
            var path = _pathfinder.FindPath((int)robot.X, (int)robot.Y, (int)robot.TargetX, (int)robot.TargetY);
            if (path != null && path.Count > 0)
            {
                robot.CurrentPath = path;
                robot.State = "ToShelf";
            }
        }
        
        else if (robot.State == "ToShelf" && robot.CurrentPath.Count == 0)
        {
            robot.State = "Loading";
            robot.HasCargo = true;
            robot.TargetX = 0;
            robot.TargetY = 5;
            robot.CurrentTargetNode = null;
            
            var path = _pathfinder.FindPath((int)robot.X, (int)robot.Y, (int)robot.TargetX, (int)robot.TargetY);
            robot.CurrentPath = path ?? new List<Position>();
            robot.State = "ToExit";
        }

        else if (robot.State == "ToExit" && robot.CurrentPath.Count == 0)
        {
            robot.HasCargo = false;
            robot.State = "Idle"; 
        }

        // --- MOVEMENT AND COLLISION AVOIDANCE ---
        
        if (robot.CurrentPath != null && robot.CurrentPath.Count > 0)
        {
            var nextStep = robot.CurrentPath[0];
            bool isBlocked = occupiedCells.Contains(((int)nextStep.X, (int)nextStep.Y));
            
            if (!isBlocked)
            {
                occupiedCells.Remove(((int)robot.X, (int)robot.Y));
                float speed = 0.2f;
                float dx = nextStep.X - robot.X;
                float dy = nextStep.Y - robot.Y;

                if (Math.Abs(dx) < speed && Math.Abs(dy) < speed)
                {
                    robot.X = nextStep.X;
                    robot.Y = nextStep.Y;
                    robot.CurrentPath.RemoveAt(0);
                    occupiedCells.Add(((int)robot.X, (int)robot.Y));
                }
                else
                {
                    robot.X += Math.Sign(dx) * speed;
                    robot.Y += Math.Sign(dy) * speed;
                }
            }
        }
    }
}

    private Obstacle GetRandomShelf()
    {
        var shelves = Map.Obstacles.Where(o => o.Type == "Shelf").ToList();
        if (shelves.Count == 0) return new Obstacle { X = 5, Y = 5 };
        return shelves[_random.Next(shelves.Count)];
    }

    private bool IsShelf(int x, int y)
    {
        return Map.Obstacles.Any(o => o.X == x && o.Y == y);
    }
    private string GetRandomNeonColor()
    {
        var colors = new[] { "#00FF00", "#00FFFF", "#FF00FF", "#FFFF00", "#FF4500" };
        return colors[_random.Next(colors.Length)];
    }
}