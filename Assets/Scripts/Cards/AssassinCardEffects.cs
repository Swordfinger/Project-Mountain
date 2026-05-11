using System;
using JailerGame.Characters;
using JailerGame.Core;
using JailerGame.Combat.BreakPoint;
using UnityEngine;

namespace JailerGame.Cards
{
    /// <summary>
    /// 刺客职业的特化卡牌效果。
    ///
    /// 测试阶段约定（用户已确认）：
    /// - 所有"伤害"统一 5 点
    /// - 破绽命中：刺杀 x2、殊死一搏 x1.5、难以治愈 x0.75
    /// - 反客为主：使用后 4 回合内不可再次使用（同名所有副本共享冷却）
    /// - 投毒：5 回合中毒，期间敌人回血减半
    ///
    /// 普通的伤害/格挡/移动直接复用 CardEffect.cs 里的 DamageEffect / BlockEffect / MoveEffect；
    /// 这里只放刺客独有的行为。
    /// </summary>

    // ============= 1. 刺杀 =============
    // 直接用 DamageEffect { damage = 5, critMultiplier = 2.0f }，无需新类。

    // ============= 2. 防御 =============
    // 用 BlockEffect { blockAmount = 5, targetSelf = true }。

    // ============= 3. 移动 =============
    // 用 MoveEffect { maxSteps = 1 }。

    // ============= 4. 暗中前行 =============
    /// <summary>
    /// 暗中前行：必须本回合优先使用；使用后结束本回合；
    /// 移动距离 = 当前手牌中能量消耗最大值。
    /// 若本回合未优先打出则锁定保留到下回合（逻辑由 TurnManager 处理 IsLocked）。
    /// </summary>
    [Serializable]
    public class StealthAdvanceEffect : CardEffect
    {
        public override bool Execute(CardCastContext ctx)
        {
            if (ctx.Caster is not PlayerEntity p) return false;
            if (!ctx.TargetCell.HasValue)
            {
                Debug.Log("[Card] 暗中前行：未指定目标格，跳过");
                return false;
            }

            // 计算移动距离 = 手牌中最高能量消耗
            int maxCost = 0;
            foreach (var c in p.Deck.Hand)
                if (c.Data.energyCost > maxCost) maxCost = c.Data.energyCost;
            if (maxCost <= 0) maxCost = 1;

            var move = new MoveEffect { maxSteps = maxCost };
            bool moved = move.Execute(ctx);

            // 暗中前行用后立即结束本回合
            ctx.EndTurnAfter = true;
            Debug.Log($"[Card] {p.DisplayName} 暗中前行：移动 {maxCost} 格，结束本回合");
            return moved;
        }
    }

    // ============= 5. 殊死一搏 =============
    /// <summary>
    /// 殊死一搏：将所有手牌当作刺杀使用，每张造成 5 伤害；
    /// 命中破绽时仅 x1.5 倍率（而不是普通刺杀的 x2）。
    /// </summary>
    [Serializable]
    public class DesperateStrikeEffect : CardEffect
    {
        public int damagePerCard = 5;
        public float critMultiplier = 1.5f;

        public override bool Execute(CardCastContext ctx)
        {
            if (ctx.Caster is not PlayerEntity p) return false;
            if (ctx.Targets == null || ctx.Targets.Count == 0) return false;

            // 把"殊死一搏"自身排除（它在被打出时还在 Hand 里，结算后才被弃牌）
            int handCount = Mathf.Max(0, p.Deck.Hand.Count - 1);
            if (handCount == 0)
            {
                Debug.Log($"[Card] {p.DisplayName} 殊死一搏：手牌只剩自己，无追加伤害");
                return true;
            }

            int total = 0;
            foreach (var t in ctx.Targets)
            {
                int hitDir = BreakPointSystem.GetHitDirection(ctx.AttackDirection);
                for (int i = 0; i < handCount; i++)
                {
                    // 每张手牌单独走一次破绽判定（命中 x1.5）
                    int dmg = damagePerCard;
                    var slot = t.BreakPoints.GetSlot(hitDir);
                    if (slot.IsActive && slot.Type == ctx.CasterClass)
                    {
                        dmg = Mathf.CeilToInt(damagePerCard * critMultiplier);
                    }
                    t.TakeDamage(dmg, hitDir, ctx.CasterClass);
                    total += dmg;
                }
            }

            // 把手牌全部弃掉（除了"殊死一搏"自身，由 TurnManager 在打出后弃牌）
            ctx.DiscardEntireHandAfter = true;
            Debug.Log($"[Card] {p.DisplayName} 殊死一搏：合计造成 {total} 伤害");
            return true;
        }
    }

    // ============= 6. 难以治愈 =============
    /// <summary>
    /// 难以治愈（武器生锈）：
    /// 本次"刺杀"伤害 x0.75（5 → 4 向下取整 → 4），但目标进入持续流血。
    /// 测试阶段：直接造成 4 点伤害 + 流血 2 层 / 3 回合
    /// </summary>
    [Serializable]
    public class RustyBladeEffect : CardEffect
    {
        public int damage = 4;
        public int bleedStacks = 2;
        public int bleedTurns = 3;

        public override bool Execute(CardCastContext ctx)
        {
            if (ctx.Targets == null || ctx.Targets.Count == 0) return false;
            int hitDir = BreakPointSystem.GetHitDirection(ctx.AttackDirection);
            foreach (var t in ctx.Targets)
            {
                t.TakeDamage(damage, hitDir, ctx.CasterClass);
                t.StatusEffects.Add(new BleedStatus(bleedStacks, bleedTurns));
                Debug.Log($"[Card] 难以治愈：{t.DisplayName} 受 {damage} 伤害，流血 {bleedStacks}层/{bleedTurns}回合");
            }
            return true;
        }
    }

    // ============= 7. 按兵不动 =============
    /// <summary>
    /// 按兵不动：本回合放弃出牌，下回合敌人破绽必出现在玩家面前（攻击方向）。
    /// 实现：给目标添加一个 GuaranteedBreakPointStatus，下回合开始时强制刷新。
    /// </summary>
    [Serializable]
    public class HoldPositionEffect : CardEffect
    {
        public override bool Execute(CardCastContext ctx)
        {
            if (ctx.Targets == null || ctx.Targets.Count == 0) return false;
            foreach (var t in ctx.Targets)
            {
                t.StatusEffects.Add(new GuaranteedBreakPointStatus(ctx.CasterClass, 1));
                Debug.Log($"[Card] 按兵不动：下回合 {t.DisplayName} 必出现 {ctx.CasterClass} 破绽");
            }
            ctx.EndTurnAfter = true;
            return true;
        }
    }

    // ============= 8. 投毒 =============
    /// <summary>
    /// 投毒：目标中毒 5 回合；中毒期间，目标使用回血技能仅恢复一半。
    /// </summary>
    [Serializable]
    public class PoisonEffect : CardEffect
    {
        public int duration = 5;
        public float healMultiplier = 0.5f;

        public override bool Execute(CardCastContext ctx)
        {
            foreach (var t in ctx.Targets)
            {
                t.StatusEffects.Add(new PoisonStatus(duration, healMultiplier));
                Debug.Log($"[Card] 投毒：{t.DisplayName} 中毒 {duration} 回合，回血 x{healMultiplier}");
            }
            return true;
        }
    }

    // ============= 9. 反客为主 =============
    /// <summary>
    /// 反客为主：后手反应技能。下回合若敌人对你使用攻击，你将闪避；
    /// 同时从牌堆里随机打出一张手牌。
    /// 使用后此卡 4 回合内不可再次使用（同名卡共享冷却）。
    /// </summary>
    [Serializable]
    public class CounterAttackEffect : CardEffect
    {
        public int cooldownTurns = 4;

        public override bool Execute(CardCastContext ctx)
        {
            if (ctx.Caster is not PlayerEntity p) return false;
            p.StatusEffects.Add(new CounterStanceStatus(1)); // 下回合生效
            // 同名卡冷却（在所有副本上加 cooldown）
            foreach (var c in p.Deck.Hand)
                if (c.Data != null && c.Data.cardId == "ASS_009") c.CurrentCooldown = cooldownTurns;
            foreach (var c in p.Deck.DrawPile)
                if (c.Data != null && c.Data.cardId == "ASS_009") c.CurrentCooldown = cooldownTurns;
            foreach (var c in p.Deck.DiscardPile)
                if (c.Data != null && c.Data.cardId == "ASS_009") c.CurrentCooldown = cooldownTurns;

            Debug.Log($"[Card] {p.DisplayName} 反客为主：进入后手反击姿态，{cooldownTurns} 回合冷却");
            return true;
        }
    }

    // ============= 10. 收刀 =============
    /// <summary>
    /// 收刀：敌人接下来 2 回合无法命中你的破绽（你的破绽对敌人无效）；
    /// 但你 2 回合内不可使用"刺杀"。
    /// </summary>
    [Serializable]
    public class SheatheEffect : CardEffect
    {
        public int duration = 2;

        public override bool Execute(CardCastContext ctx)
        {
            if (ctx.Caster is not PlayerEntity p) return false;
            p.StatusEffects.Add(new SheatheStatus(duration));
            // 锁住所有"刺杀"卡（cardId = ASS_001）
            foreach (var c in p.Deck.Hand)
                if (c.Data != null && c.Data.cardId == "ASS_001") c.CurrentCooldown = duration;
            foreach (var c in p.Deck.DrawPile)
                if (c.Data != null && c.Data.cardId == "ASS_001") c.CurrentCooldown = duration;
            foreach (var c in p.Deck.DiscardPile)
                if (c.Data != null && c.Data.cardId == "ASS_001") c.CurrentCooldown = duration;
            Debug.Log($"[Card] {p.DisplayName} 收刀：{duration} 回合敌人无法命中你的破绽，但同期不可刺杀");
            return true;
        }
    }

    // ============= 11. 重装上阵 =============
    /// <summary>
    /// 重装上阵：切换为远程姿态，必须在队友身后才能攻击敌人。
    /// 实现：给玩家添加 RangedStanceStatus 标记。
    /// </summary>
    [Serializable]
    public class HeavyArmorEffect : CardEffect
    {
        public int duration = 3;
        public override bool Execute(CardCastContext ctx)
        {
            if (ctx.Caster is not PlayerEntity p) return false;
            p.StatusEffects.Add(new RangedStanceStatus(duration));
            Debug.Log($"[Card] {p.DisplayName} 重装上阵：进入远程姿态 {duration} 回合");
            return true;
        }
    }

    // ============= 12. 回旋镖 =============
    /// <summary>
    /// 回旋镖：对一条直线上的目标造成两次伤害（去程 + 回程）。
    /// 测试阶段：对所有 Targets 造成 2x 5 伤害。
    /// </summary>
    [Serializable]
    public class BoomerangEffect : CardEffect
    {
        public int damagePerHit = 5;

        public override bool Execute(CardCastContext ctx)
        {
            if (ctx.Targets == null || ctx.Targets.Count == 0) return false;
            int hitDir = BreakPointSystem.GetHitDirection(ctx.AttackDirection);
            foreach (var t in ctx.Targets)
            {
                t.TakeDamage(damagePerHit, hitDir, ctx.CasterClass);
                t.TakeDamage(damagePerHit, hitDir, ctx.CasterClass);
                Debug.Log($"[Card] 回旋镖：对 {t.DisplayName} 造成 2x{damagePerHit} 伤害");
            }
            return true;
        }
    }

    // ============= 状态类 =============

    /// <summary>流血：每回合开始造成 stacks 点真实伤害，duration 回合</summary>
    public class BleedStatus : StatusEffect
    {
        public BleedStatus(int stacks, int turns)
        {
            Id = "STATUS_BLEED";
            Stacks = stacks;
            RemainingTurns = turns;
        }
        public override void OnTurnStart(CombatEntity owner)
        {
            owner.TakeDamage(Stacks, -1, null);
            Debug.Log($"[Status] {owner.DisplayName} 流血造成 {Stacks} 伤害");
            RemainingTurns--;
        }
        public override void OnTurnEnd(CombatEntity owner) { }
    }

    /// <summary>中毒：减半回血效果，由 PlayerEntity / EnemyEntity 在 Heal 时检查</summary>
    public class PoisonStatus : StatusEffect
    {
        public float HealMultiplier;
        public PoisonStatus(int turns, float healMul)
        {
            Id = "STATUS_POISON";
            Stacks = 1;
            RemainingTurns = turns;
            HealMultiplier = healMul;
        }
        public override void OnTurnStart(CombatEntity owner) { RemainingTurns--; }
        public override void OnTurnEnd(CombatEntity owner) { }
    }

    /// <summary>下回合敌人必出现指定 Class 破绽（按兵不动）</summary>
    public class GuaranteedBreakPointStatus : StatusEffect
    {
        public BreakPointType ForcedClass;
        public GuaranteedBreakPointStatus(BreakPointType cls, int turns)
        {
            Id = "STATUS_GUARANTEED_BREAK";
            ForcedClass = cls;
            RemainingTurns = turns;
            Stacks = 1;
        }
        public override void OnTurnStart(CombatEntity owner)
        {
            // 直接在 owner 的 0 号方向（敌人面前）强制刷一个破绽
            owner.BreakPoints.Set(0, ForcedClass, 1);
            RemainingTurns--;
        }
        public override void OnTurnEnd(CombatEntity owner) { }
    }

    /// <summary>反客为主姿态：下回合若被攻击则闪避并反打一张随机卡</summary>
    public class CounterStanceStatus : StatusEffect
    {
        public CounterStanceStatus(int turns)
        {
            Id = "STATUS_COUNTER";
            RemainingTurns = turns;
            Stacks = 1;
        }
        public override void OnTurnStart(CombatEntity owner) { }
        public override void OnTurnEnd(CombatEntity owner) { RemainingTurns--; }
    }

    /// <summary>收刀姿态：敌人无法命中你的破绽</summary>
    public class SheatheStatus : StatusEffect
    {
        public SheatheStatus(int turns)
        {
            Id = "STATUS_SHEATHE";
            RemainingTurns = turns;
            Stacks = 1;
        }
        public override void OnTurnStart(CombatEntity owner) { }
        public override void OnTurnEnd(CombatEntity owner) { RemainingTurns--; }
    }

    /// <summary>远程姿态：必须站在队友身后才可攻击</summary>
    public class RangedStanceStatus : StatusEffect
    {
        public RangedStanceStatus(int turns)
        {
            Id = "STATUS_RANGED";
            RemainingTurns = turns;
            Stacks = 1;
        }
        public override void OnTurnStart(CombatEntity owner) { }
        public override void OnTurnEnd(CombatEntity owner) { RemainingTurns--; }
    }
}
