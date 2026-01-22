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
        foreach (var robot in Robots)
        {
            if (robot.CurrentPath == null || robot.CurrentPath.Count == 0)
            {
                int tx, ty;
                do
                {
                    tx = _random.Next(0, Map.Width);
                    ty = _random.Next(0, Map.Height);
                } while (IsShelf(tx, ty));

                robot.TargetX = tx;
                robot.TargetY = ty;
                
                var path = _pathfinder.FindPath((int)robot.X, (int)robot.Y, tx, ty);
                if (path != null && path.Count > 1) 
                {
                    path.RemoveAt(0);
                    robot.CurrentPath = path;
                    robot.State = "Moving";
                }
            }

            if (robot.CurrentPath != null && robot.CurrentPath.Count > 0)
            {
                var nextStep = robot.CurrentPath[0];
                float speed = 0.2f;

                float dx = nextStep.X - robot.X;
                float dy = nextStep.Y - robot.Y;
                
                if (Math.Abs(dx) < speed && Math.Abs(dy) < speed)
                {
                    robot.X = nextStep.X;
                    robot.Y = nextStep.Y;
                    robot.CurrentPath.RemoveAt(0);
                }
                else
                {
                    robot.X += Math.Sign(dx) * speed;
                    robot.Y += Math.Sign(dy) * speed;
                }
            }
        }
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