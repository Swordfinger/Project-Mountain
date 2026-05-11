using System.Collections.Generic;
using UnityEngine;

namespace JailerGame.Combat
{
    /// <summary>
    /// 敌人/Boss 数据资产。
    /// </summary>
    [CreateAssetMenu(fileName = "Enemy_New", menuName = "JailerGame/Enemy Data", order = 3)]
    public class EnemyData : ScriptableObject
    {
        [Header("身份")]
        public string enemyId;
        public string displayName;
        public EnemyType type = EnemyType.Minion;
        [TextArea(2, 5)] public string description;
        public Sprite portrait;

        [Header("基础数值（基于 1 名玩家）")]
        public int baseMaxHp = 100;
        public int baseDamage = 8;
        public int baseSpeed = 5;
        public int moveRange = 1;

        [Header("人数缩放（人少时按比例减弱，但仍偏难）")]
        [Tooltip("HP 按公式 baseMaxHp * (1 + (playerCount-1) * scale)")]
        public float hpScalePerPlayer = 0.6f;
        [Tooltip("伤害按公式 baseDamage * (1 + (playerCount-1) * scale)")]
        public float damageScalePerPlayer = 0.2f;

        [Header("AI 行为模式")]
        public List<EnemyActionPattern> actionPatterns = new();
    }

    public enum EnemyType
    {
        Minion,        // 普通怪
        Elite,         // 精英怪
        Gatekeeper,    // 守门人
        SealedEmissary,// 被封印的使者（大魔王）
        FinalBoss,     // 狱卒使
    }

    [System.Serializable]
    public class EnemyActionPattern
    {
        public string actionName;
        public EnemyActionType actionType;
        public int value;       // 伤害/格挡/治疗值
        public int weight = 1;  // AI 选择权重
        [Tooltip("使用此招后是否转身（影响破绽）")]
        public bool turnAfterAction = true;
    }

    public enum EnemyActionType
    {
        Attack,
        AOEAttack,
        Defend,
        Heal,
        SummonMinion,
        Buff,
        Debuff,
    }
}
