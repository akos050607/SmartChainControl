using SmartChainControl.Shared.Models;

namespace SmartChainControl.Server.Services
{
    public class Pathfinder
    {
        private readonly bool[,] _collisionMap;
        private readonly int _width;
        private readonly int _height;

        // Node class for A* algorithm
        private class Node
        {
            public int X { get; set; }
            public int Y { get; set; }
            public Node? Parent { get; set; }
            public int G { get; set; } // Distance from start
            public int H { get; set; } // Estimated distance to target
            public int F => G + H;     // Cost
        }

        public Pathfinder(MapInfo map)
        {
            _width = map.Width;
            _height = map.Height;
            _collisionMap = new bool[_width, _height];

            foreach (var obs in map.Obstacles)
            {
                if (obs.X >= 0 && obs.X < _width && obs.Y >= 0 && obs.Y < _height)
                {
                    _collisionMap[obs.X, obs.Y] = true;
                }
            }
        }

        public List<Position>? FindPath(int startX, int startY, int targetX, int targetY, HashSet<(int, int)>? dynamicObstacles = null)
        {
            if (IsWall(targetX, targetY, null)) return null;

            var openList = new List<Node>();
            var closedList = new HashSet<(int, int)>();

            openList.Add(new Node { X = startX, Y = startY });

            while (openList.Count > 0)
            {
                var current = openList.OrderBy(n => n.F).First();

                if (current.X == targetX && current.Y == targetY)
                {
                    return ReconstructPath(current);
                }

                openList.Remove(current);
                closedList.Add((current.X, current.Y));

                foreach (var neighbor in GetNeighbors(current))
                {
                    if (closedList.Contains((neighbor.X, neighbor.Y))) continue;
                    
                    if (IsWall(neighbor.X, neighbor.Y, dynamicObstacles)) continue;

                    var existingNode = openList.FirstOrDefault(n => n.X == neighbor.X && n.Y == neighbor.Y);
                    if (existingNode == null)
                    {
                        neighbor.G = current.G + 1;
                        neighbor.H = Math.Abs(neighbor.X - targetX) + Math.Abs(neighbor.Y - targetY);
                        neighbor.Parent = current;
                        openList.Add(neighbor);
                    }
                    else if (current.G + 1 < existingNode.G)
                    {
                        existingNode.G = current.G + 1;
                        existingNode.Parent = current;
                    }
                }
            }

            return null;
        }

        private bool IsWall(int x, int y, HashSet<(int, int)>? dynamicObstacles)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height) return true;
            
            if (_collisionMap[x, y]) return true;

            if (dynamicObstacles != null && dynamicObstacles.Contains((x, y))) return true;

            return false;
        }

        private List<Position> ReconstructPath(Node? node)
        {
            var path = new List<Position>();
            while (node != null)
            {
                path.Add(new Position { X = node.X, Y = node.Y });
                node = node.Parent;
            }
            path.Reverse();
            return path;
        }

        private IEnumerable<Node> GetNeighbors(Node node)
        {
            var neighbors = new List<Node>
            {
                new Node { X = node.X, Y = node.Y - 1 }, // Up
                new Node { X = node.X, Y = node.Y + 1 }, // Down
                new Node { X = node.X - 1, Y = node.Y }, // Left
                new Node { X = node.X + 1, Y = node.Y }  // Right
            };

            return neighbors.Where(n => n.X >= 0 && n.X < _width && n.Y >= 0 && n.Y < _height);
        }
    }
    
}