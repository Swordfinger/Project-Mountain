using System;
using JailerGame.Characters;
using JailerGame.Core;
using UnityEngine;

namespace JailerGame.Cards
{
    /// <summary>
    /// 刺客职业的特化卡牌效果。
    /// 普通的伤害/格挡/移动直接复用 CardEffect.cs 里的通用效果；
    /// 这里只放刺客独有的行为。
    /// </summary>

    /// <summary>殊死一搏：将所有手牌当作刺杀使用，破绽伤害 x1.5（而不是 x2）</summary>
    [Serializable]
    public class DesperateStrikeEffect : CardEffect
    {
        public int damagePerCard = 5;
        public float critMultiplier = 1.5f;

        public override bool Execute(CardCastContext ctx)
        {
            if (ctx.Caster is not PlayerEntity p) return false;
            if (ctx.Targets == null || ctx.Targets.Count == 0) return false;
            int handCount = p.Deck.Hand.Count;
            int totalDmg = damagePerCard * handCount;
            foreach (var t in ctx.Targets)
                t.TakeDamage(totalDmg, Combat.BreakPoint.BreakPointSystem.GetHitDirection(ctx.AttackDirection), ctx.CasterClass);
            // 这一回合不再出别的牌
            // （实际通过 TurnManager 的"已出牌"队列阻止追加；第一阶段先打日志）
            Debug.Log($"[Card] {p.DisplayName} 殊死一搏：用 {handCount} 张手牌共造成 {totalDmg} 伤害");
            return true;
        }
    }

    /// <summary>投毒：未来 2 回合内目标使用回血技能则回血减半，且持续 5 回合中毒</summary>
    [Serializable]
    public class PoisonEffect : CardEffect
    {
        public int duration = 5;
        public override bool Execute(CardCastContext ctx)
        {
            foreach (var t in ctx.Targets)
            {
                t.StatusEffects.Add(new BleedStatus(2, duration)); // 占位用流血代替投毒
                Debug.Log($"[Card] 投毒：{t.DisplayName} 中毒 {duration} 回合");
            }
            return true;
        }
    }

    /// <summary>按兵不动：本回合不行动，下回合敌人破绽出现在玩家面前</summary>
    [Serializable]
    public class HoldPositionEffect : CardEffect
    {
        public override bool Execute(CardCastContext ctx)
        {
            // 给敌人一个保证出现破绽的标记（第二阶段实现 Status）
            foreach (var t in ctx.Targets)
                Debug.Log($"[Card] 按兵不动：{ctx.Caster.DisplayName} 等待 {t.DisplayName} 露出破绽");
            return true;
        }
    }

    /// <summary>难以治愈：刺杀伤害降为 x0.75，但目标流血</summary>
    [Serializable]
    public class RustyBladeEffect : CardEffect
    {
        public int damage = 4;
        public int bleedStacks = 2;
        public int bleedTurns = 3;

        public override bool Execute(CardCastContext ctx)
        {
            foreach (var t in ctx.Targets)
            {
                t.TakeDamage(damage, Combat.BreakPoint.BreakPointSystem.GetHitDirection(ctx.AttackDirection), ctx.CasterClass);
                t.StatusEffects.Add(new BleedStatus(bleedStacks, bleedTurns));
            }
            return true;
        }
    }
}
