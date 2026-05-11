using System.Collections.Generic;
using UnityEngine;

namespace JailerGame.World
{
    /// <summary>
    /// 4 层世界管理器（占位）。
    /// 第一阶段只放数据骨架，第三阶段实现完整的入口选择/晋级判定。
    ///
    /// 设计回顾（来自策划讨论）：
    /// - 共 4 层，前 3 层是"自由发育层"，每层约 15 分钟
    /// - 第 4 层是最终 Boss 层
    /// - 每层有：守门人（必打 / 副本）、若干入口（精英怪/金币/赐福/大厅）
    /// - 大魔王副本（可选）：使者按按钮释放，全员强制合作
    /// </summary>
    public class LayerManager : MonoBehaviour
    {
        [Header("4 层世界配置")]
        public List<LayerData> layers = new();

        public LayerData CurrentLayer { get; private set; }
        public int CurrentIndex { get; private set; }

        public void EnterLayer(int idx)
        {
            if (idx < 0 || idx >= layers.Count) return;
            CurrentIndex = idx;
            CurrentLayer = layers[idx];
            Debug.Log($"[Layer] 进入第 {idx + 1} 层：{CurrentLayer.layerName}");
        }
    }

    [System.Serializable]
    public class LayerData
    {
        public string layerName;
        [TextArea(2, 4)] public string description;
        public int requiredEliteKills = 1;
        public int requiredGold = 50;
        public List<NodeData> nodes = new();
        public Combat.EnemyData gatekeeper;
        public Combat.EnemyData sealedEmissary; // 大魔王（可选副本）
    }

    [System.Serializable]
    public class NodeData
    {
        public NodeType type;
        public string nodeName;
        public Combat.EnemyData enemyOverride; // 精英怪节点用
        public int goldReward = 10;
    }

    public enum NodeType
    {
        EliteEnemy,
        GoldFarm,
        Blessing,
        Hall,           // 商店、悬赏
        SecretPath,     // 通过天才走秘密通道（你提到的两种晋级方式之一）
    }
}
