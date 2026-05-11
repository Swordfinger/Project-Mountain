using System.Collections.Generic;
using UnityEngine;

namespace JailerGame.Cards
{
    /// <summary>
    /// 一张卡的数据资产（ScriptableObject）。
    /// 在 Unity 中右键 Create → JailerGame → Card Data 创建。
    /// </summary>
    [CreateAssetMenu(fileName = "Card_New", menuName = "JailerGame/Card Data", order = 1)]
    public class CardData : ScriptableObject
    {
        [Header("基本信息")]
        public string cardId;
        public string cardName;
        [TextArea(2, 5)] public string description;
        public Sprite artwork;

        [Header("分类")]
        public CardRarity rarity = CardRarity.Common;
        public CardCategory category = CardCategory.Attack;
        public CardTiming timing = CardTiming.Normal;

        [Header("使用条件")]
        public int energyCost = 1;
        public CardTargetType targetType = CardTargetType.SingleEnemy;
        [Tooltip("攻击距离（蜂窝格步数），0=无限制")]
        public int range = 1;

        [Header("速度修正（速度结算阶段叠加到角色 BaseSpeed）")]
        public int speedModifier = 0;

        [Header("效果列表（按顺序执行）")]
        [SerializeReference] public List<CardEffect> effects = new();

        [Header("使用后冷却（回合数，0=无冷却）")]
        public int cooldownTurns = 0;

        [Header("使用后是否消耗（一次性卡）")]
        public bool exhaustAfterUse = false;
    }
}
