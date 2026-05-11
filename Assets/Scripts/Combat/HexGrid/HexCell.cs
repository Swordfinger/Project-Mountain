using System.Collections.Generic;
using JailerGame.Core;

namespace JailerGame.Combat.HexGrid
{
    /// <summary>
    /// 蜂窝格的逻辑单元（不挂 MonoBehaviour，便于联机序列化）
    /// 视觉部分由 HexCellView 负责。
    /// </summary>
    public class HexCell
    {
        public HexCoordinates Coordinates { get; }
        public TerrainType Terrain { get; set; } = TerrainType.Plain;

        // 当前占用此格的实体（玩家/敌人），null 表示空
        public CombatEntity Occupant { get; set; }

        public bool IsWalkable => Terrain != TerrainType.Wall && Occupant == null;

        public HexCell(HexCoordinates coords)
        {
            Coordinates = coords;
        }

        public override string ToString() => $"HexCell {Coordinates} [{Terrain}]" +
                                             (Occupant != null ? $" by {Occupant.DisplayName}" : "");
    }

    public enum TerrainType
    {
        Plain,    // 普通地形
        Rough,    // 粗糙地形（消耗双倍移动）
        Wall,     // 不可通行
        Pit,      // 陷阱（进入受伤）
        Healing,  // 治疗格（进入回血）
    }
}
