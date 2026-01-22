namespace SmartChainControl.Shared.Models
{
    public class MapInfo
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public List<Obstacle> Obstacles { get; set; } = new();
    }

    public class Obstacle
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string Type { get; set; } = "Shelf";
    }
}