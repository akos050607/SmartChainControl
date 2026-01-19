using SmartChainControl.Shared.Models;
using System.Collections.Concurrent;

namespace SmartChainControl.Server.Services;

public class SimulationManager
{
    public List<Robot> Robots { get; private set; } = new();
    
    private readonly Random _random = new();
    private const int MapSize = 20;

    public SimulationManager()
    {
        for (int i = 1; i <= 5; i++)
        {
            Robots.Add(new Robot
            {
                Id = i,
                X = _random.Next(0, MapSize),
                Y = _random.Next(0, MapSize),
                TargetX = _random.Next(0, MapSize),
                TargetY = _random.Next(0, MapSize),
                ColorHex = GetRandomNeonColor(),
                State = "Idle"
            });
        }
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
                robot.TargetX = _random.Next(0, MapSize);
                robot.TargetY = _random.Next(0, MapSize);
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