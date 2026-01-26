using System.Collections.Generic;

namespace SmartChainControl.Shared.Models
{
    public class Robot
    {
        public int Id { get; set; }
        
        public float X { get; set; }
        public float Y { get; set; }

        public float TargetX { get; set; }
        public float TargetY { get; set; }

        public List<Position> CurrentPath { get; set; } = new();

        public Position? CurrentTargetNode { get; set; }

        public string State { get; set; } = "Idle";

        public string ColorHex { get; set; } = "#00FF00";

        public bool HasCargo { get; set; }

        public int StuckTicks { get; set; } = 0;

        public int PatienceThreshold { get; set; } = 10;

        public double BatteryLevel { get; set; } = 100.0;
    }
}