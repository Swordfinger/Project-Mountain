using System.Collections.Generic;
using UnityEngine;

namespace JailerGame.Combat.HexGrid
{
    /// <summary>
    /// 战斗地图的逻辑表示。纯 C#，存所有 HexCell。
    /// HexGridManager (MonoBehaviour) 负责生成它和驱动渲染。
    /// </summary>
    public class HexGridMap
    {
        private readonly Dictionary<HexCoordinates, HexCell> _cells = new();
        public IReadOnlyDictionary<HexCoordinates, HexCell> Cells => _cells;

        public int Width { get; }
        public int Height { get; }
        public float CellSize { get; }

        public HexGridMap(int width, int height, float cellSize)
        {
            Width = width;
            Height = height;
            CellSize = cellSize;
            BuildRectangleShape();
        }

        /// <summary>
        /// 用 odd-q offset 方式生成一个矩形蜂窝地图
        /// </summary>
        private void BuildRectangleShape()
        {
            for (int col = 0; col < Width; col++)
            {
                for (int row = 0; row < Height; row++)
                {
                    var coord = HexCoordinates.FromOffsetCoordinates(col, row);
                    _cells[coord] = new HexCell(coord);
                }
            }
        }

        public HexCell GetCell(HexCoordinates coord) =>
            _cells.TryGetValue(coord, out var cell) ? cell : null;

        public bool Contains(HexCoordinates coord) => _cells.ContainsKey(coord);

        /// <summary>
        /// 取一个格的 6 个邻居（不存在的方向返回 null，但列表索引仍是 0~5）
        /// </summary>
        public HexCell[] GetNeighbors(HexCoordinates coord)
        {
            var result = new HexCell[6];
            for (int i = 0; i < 6; i++)
            {
                var neighborCoord = coord.Neighbor(i);
                _cells.TryGetValue(neighborCoord, out result[i]);
            }
            return result;
        }

        /// <summary>
        /// 取一个格在某方向的邻居（如果你只关心某个方向）
        /// </summary>
        public HexCell GetNeighbor(HexCoordinates coord, int directionIndex)
        {
            var nc = coord.Neighbor(directionIndex);
            return GetCell(nc);
        }

        /// <summary>
        /// 取一定范围内的所有格（不含中心）
        /// </summary>
        public List<HexCell> GetCellsInRange(HexCoordinates center, int range)
        {
            var result = new List<HexCell>();
            foreach (var kv in _cells)
            {
                if (kv.Key == center) continue;
                if (HexCoordinates.Distance(center, kv.Key) <= range)
                    result.Add(kv.Value);
            }
            return result;
        }
    }
}
