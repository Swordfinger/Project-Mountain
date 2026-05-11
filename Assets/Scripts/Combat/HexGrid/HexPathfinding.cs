using System.Collections.Generic;

namespace JailerGame.Combat.HexGrid
{
    /// <summary>
    /// 蜂窝格 A* 寻路。返回从 start 到 end 的路径（含起点终点）。
    /// 不可达返回 null。
    /// </summary>
    public static class HexPathfinding
    {
        public static List<HexCell> FindPath(HexGridMap map, HexCoordinates start, HexCoordinates end, int maxSteps = 99)
        {
            if (!map.Contains(start) || !map.Contains(end)) return null;
            if (start == end) return new List<HexCell> { map.GetCell(start) };

            var openSet = new SimplePriorityQueue<HexCoordinates>();
            openSet.Enqueue(start, 0);

            var cameFrom = new Dictionary<HexCoordinates, HexCoordinates>();
            var gScore = new Dictionary<HexCoordinates, int> { [start] = 0 };

            while (openSet.Count > 0)
            {
                var current = openSet.Dequeue();
                if (current == end)
                    return Reconstruct(cameFrom, current, map);

                if (gScore[current] >= maxSteps) continue;

                foreach (var neighbor in map.GetNeighbors(current))
                {
                    if (neighbor == null) continue;
                    if (neighbor.Coordinates != end && !neighbor.IsWalkable) continue;

                    int cost = neighbor.Terrain == TerrainType.Rough ? 2 : 1;
                    int tentative = gScore[current] + cost;

                    if (!gScore.TryGetValue(neighbor.Coordinates, out var existing) || tentative < existing)
                    {
                        cameFrom[neighbor.Coordinates] = current;
                        gScore[neighbor.Coordinates] = tentative;
                        int f = tentative + HexCoordinates.Distance(neighbor.Coordinates, end);
                        openSet.Enqueue(neighbor.Coordinates, f);
                    }
                }
            }

            return null;
        }

        private static List<HexCell> Reconstruct(Dictionary<HexCoordinates, HexCoordinates> cameFrom,
            HexCoordinates current, HexGridMap map)
        {
            var path = new List<HexCell> { map.GetCell(current) };
            while (cameFrom.TryGetValue(current, out var prev))
            {
                current = prev;
                path.Add(map.GetCell(current));
            }
            path.Reverse();
            return path;
        }

        // 极简优先队列（避免依赖第三方库）
        private class SimplePriorityQueue<T>
        {
            private readonly List<(T item, int priority)> _list = new();
            public int Count => _list.Count;
            public void Enqueue(T item, int priority)
            {
                _list.Add((item, priority));
                _list.Sort((a, b) => a.priority.CompareTo(b.priority));
            }
            public T Dequeue()
            {
                var top = _list[0];
                _list.RemoveAt(0);
                return top.item;
            }
        }
    }
}
