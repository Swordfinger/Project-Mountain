using System;
using UnityEngine;

namespace JailerGame.Combat.HexGrid
{
    /// <summary>
    /// 蜂窝格立方坐标 (Cube Coordinates)，满足 x + y + z = 0
    /// 参考 https://www.redblobgames.com/grids/hexagons/
    /// 这是纯数据结构，不依赖 MonoBehaviour，方便序列化和联机同步。
    /// </summary>
    [Serializable]
    public struct HexCoordinates : IEquatable<HexCoordinates>
    {
        public int x;
        public int y;
        public int z;

        public HexCoordinates(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            // 在 Editor 模式下校验 x+y+z=0
            #if UNITY_EDITOR
            if (x + y + z != 0)
                Debug.LogError($"HexCoordinates 非法：({x},{y},{z}) 不满足 x+y+z=0");
            #endif
        }

        /// <summary>
        /// 6 个方向向量，索引 0~5 顺时针：东、东北、西北、西、西南、东南
        /// 索引就是"破绽方向"的标识，BreakPoint 系统会用到
        /// </summary>
        public static readonly HexCoordinates[] Directions =
        {
            new HexCoordinates( 1, -1,  0),  // 0 East
            new HexCoordinates( 1,  0, -1),  // 1 NorthEast
            new HexCoordinates( 0,  1, -1),  // 2 NorthWest
            new HexCoordinates(-1,  1,  0),  // 3 West
            new HexCoordinates(-1,  0,  1),  // 4 SouthWest
            new HexCoordinates( 0, -1,  1),  // 5 SouthEast
        };

        public HexCoordinates Neighbor(int directionIndex)
        {
            var d = Directions[((directionIndex % 6) + 6) % 6];
            return new HexCoordinates(x + d.x, y + d.y, z + d.z);
        }

        /// <summary>
        /// 蜂窝格曼哈顿距离
        /// </summary>
        public static int Distance(HexCoordinates a, HexCoordinates b)
        {
            return (Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y) + Math.Abs(a.z - b.z)) / 2;
        }

        /// <summary>
        /// 把立方坐标转成 Unity 世界坐标（Y 朝上的 3D 平面，蜂窝格平铺在 XZ 平面）
        /// 适用于"flat-top"蜂窝（顶部是平的）。如果你买的 Hex Game Studio 用 pointy-top，把宽高对调即可。
        /// </summary>
        public Vector3 ToWorld(float cellSize)
        {
            float worldX = cellSize * (3f / 2f * x);
            float worldZ = cellSize * (Mathf.Sqrt(3f) / 2f * x + Mathf.Sqrt(3f) * (-z));
            return new Vector3(worldX, 0, worldZ);
        }

        public static HexCoordinates FromOffsetCoordinates(int col, int row)
        {
            // odd-q offset 转 cube
            int x = col;
            int z = row - (col - (col & 1)) / 2;
            int y = -x - z;
            return new HexCoordinates(x, y, z);
        }

        /// <summary>
        /// 给定从我指向目标的方向，返回方向索引 0~5；如果不是相邻或不在六个轴向上，返回 -1
        /// </summary>
        public static int DirectionTo(HexCoordinates from, HexCoordinates to)
        {
            int dx = to.x - from.x;
            int dy = to.y - from.y;
            int dz = to.z - from.z;
            for (int i = 0; i < 6; i++)
            {
                var d = Directions[i];
                if (d.x == dx && d.y == dy && d.z == dz) return i;
            }
            return -1;
        }

        public bool Equals(HexCoordinates other) => x == other.x && y == other.y && z == other.z;
        public override bool Equals(object obj) => obj is HexCoordinates other && Equals(other);
        public override int GetHashCode() => x * 73856093 ^ y * 19349663 ^ z * 83492791;
        public override string ToString() => $"({x},{y},{z})";

        public static bool operator ==(HexCoordinates a, HexCoordinates b) => a.Equals(b);
        public static bool operator !=(HexCoordinates a, HexCoordinates b) => !a.Equals(b);
    }
}
