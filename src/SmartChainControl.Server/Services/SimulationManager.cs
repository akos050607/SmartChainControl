using SmartChainControl.Shared.Models;
using System.Collections.Concurrent;

namespace SmartChainControl.Server.Services;

public class SimulationManager
{
    public List<Robot> Robots { get; private set; } = new();
    public MapInfo Map { get; private set; }

    private readonly Random _random = new();
    private const int MapWidth = 20;
    private const int MapHeight = 20;

    public SimulationManager()
    {
        Map = GenerateWarehouseMap();

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
            float speed = 0.1f;
            
            if (Math.Abs(robot.X - robot.TargetX) > 0.1)
            {
                robot.X += robot.TargetX > robot.X ? speed : -speed;
                robot.State = "Moving";
            }
            else if (Math.Abs(robot.Y - robot.TargetY) > 0.1)
            {
                robot.Y += robot.TargetY > robot.Y ? speed : -speed;
                robot.State = "Moving";
            }
            else
            {
                robot.TargetX = _random.Next(0, MapWidth);
                robot.TargetY = _random.Next(0, MapWidth);
                robot.State = "Idle";
            }
        }
    }

    private string GetRandomNeonColor()
    {
        var colors = new[] { "#00FF00", "#00FFFF", "#FF00FF", "#FFFF00", "#FF4500" };
        return colors[_random.Next(colors.Length)];
    }
}