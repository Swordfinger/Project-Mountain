using JailerGame.Characters;
using UnityEngine;

namespace JailerGame.Identity
{
    /// <summary>
    /// 使者的额外能力（思路 D：被动 + 大招 + 剧情触发）。
    /// 第一阶段先放骨架，第二阶段补具体效果。
    /// </summary>
    public class EmissaryAbility
    {
        public PlayerEntity Owner { get; }

        // —— 1. 隐藏被动 —— 不暴露的小加成
        public int HiddenDamageBonus { get; set; } = 1;
        public int HiddenBlockBonus { get; set; } = 1;

        // —— 2. 主动大招 —— 用了会暴露
        public bool ReleaseSealedEmissaryAvailable { get; set; } = true;
        public bool PeekHandAvailable { get; set; } = true; // 偷看一名玩家手牌
        public bool FakeVoteAvailable { get; set; } = true; // 伪装票（使者专属）

        // —— 3. 剧情触发 —— 见到大魔王时的特殊互动
        public bool CanCommuneWithSealed { get; set; } = true;

        public EmissaryAbility(PlayerEntity owner)
        {
            Owner = owner;
        }

        /// <summary>使者主动按按钮，释放本层被封印的使者（即大魔王副本）</summary>
        public bool TryReleaseSealedEmissary()
        {
            if (!ReleaseSealedEmissaryAvailable) return false;
            ReleaseSealedEmissaryAvailable = false;
            Debug.Log($"[Emissary] {Owner.DisplayName} 释放了被封印的使者！（这一动作不会被其他玩家直接发现）");
            // 实际生成 Boss 的逻辑在 World/SealedEmissaryEncounter.cs 处理
            return true;
        }

        /// <summary>偷看一名玩家手牌（使者专属）</summary>
        public bool TryPeekHand(PlayerEntity target)
        {
            if (!PeekHandAvailable || target == null) return false;
            PeekHandAvailable = false;
            Debug.Log($"[Emissary] {Owner.DisplayName} 偷看了 {target.DisplayName} 的手牌：" +
                      string.Join(",", target.Deck.Hand.ConvertAll(c => c.Data.cardName)));
            return true;
        }

        /// <summary>伪装票：投票时显示一个不是使者投的目标，迷惑其他人</summary>
        public bool TryFakeVote()
        {
            if (!FakeVoteAvailable) return false;
            FakeVoteAvailable = false;
            return true;
        }
    }
}
