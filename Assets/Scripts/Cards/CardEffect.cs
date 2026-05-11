using System;
using System.Collections.Generic;
using JailerGame.Core;
using JailerGame.Characters;
using JailerGame.Combat.BreakPoint;
using JailerGame.Combat.HexGrid;
using UnityEngine;

namespace JailerGame.Cards
{
    /// <summary>
    /// 卡牌效果基类。
    /// 一个 Card 由多个 Effect 串联组成（如"刺杀+流血"= DamageEffect + ApplyStatusEffect）
    ///
    /// 用 [SerializeReference] 让 ScriptableObject 能存储多态列表。
    /// </summary>
    [Serializable]
    public abstract class CardEffect
    {
        /// <summary>执行效果。返回是否成功执行（用于卡牌 Combo 判断）</summary>
        public abstract bool Execute(CardCastContext ctx);
    }

    /// <summary>
    /// 卡牌施放上下文：所有效果共享的"环境"。
    /// </summary>
    public class CardCastContext
    {
        public CombatEntity Caster;
        public List<CombatEntity> Targets = new();
        public HexCoordinates? TargetCell;
        /// <summary>从施法者朝向目标的世界坐标方向（用于破绽判定）</summary>
        public int AttackDirection = -1;
        /// <summary>施法者的 Class（决定能命中哪种破绽）</summary>
        public BreakPointType CasterClass = BreakPointType.None;

        public BreakPointSystem BreakPointSystem;

        // ====== 卡牌结算后的副作用标记（由具体效果设置，TurnManager / BossArenaManager 读取）======

        /// <summary>本张卡结算完毕后立即结束本回合（暗中前行、按兵不动）</summary>
        public bool EndTurnAfter = false;

        /// <summary>结算完毕后将当前手牌（除本卡外）全部丢入弃牌堆（殊死一搏）</summary>
        public bool DiscardEntireHandAfter = false;

        /// <summary>商人卡专用：本次出牌实际花费金币（撒币 / 时间就是金钱用）</summary>
        public int GoldSpent = 0;
    }

    // ============= 具体效果实现 =============

    /// <summary>造成固定伤害（会走破绽判定）</summary>
    [Serializable]
    public class DamageEffect : CardEffect
    {
        [Tooltip("基础伤害值")]
        public int damage = 5;
        [Tooltip("命中破绽时的伤害倍率（默认 2.0）")]
        public float critMultiplier = 2.0f;

        public override bool Execute(CardCastContext ctx)
        {
            if (ctx.Targets == null || ctx.Targets.Count == 0) return false;
            foreach (var target in ctx.Targets)
            {
                int hitDir = BreakPointSystem.GetHitDirection(ctx.AttackDirection);
                int finalDmg = damage;
                // 让 TakeDamage 内部判断破绽，这里只传入数值
                target.TakeDamage(finalDmg, hitDir, ctx.CasterClass);
            }
            return true;
        }
    }

    /// <summary>给目标添加格挡</summary>
    [Serializable]
    public class BlockEffect : CardEffect
    {
        public int blockAmount = 5;
        public bool targetSelf = true;

        public override bool Execute(CardCastContext ctx)
        {
            if (targetSelf)
            {
                ctx.Caster.Block += blockAmount;
                return true;
            }
            foreach (var t in ctx.Targets) t.Block += blockAmount;
            return true;
        }
    }

    /// <summary>移动到目标格</summary>
    [Serializable]
    public class MoveEffect : CardEffect
    {
        [Tooltip("最大移动步数（0=使用角色 MoveRange）")]
        public int maxSteps = 0;

        public override bool Execute(CardCastContext ctx)
        {
            if (!ctx.TargetCell.HasValue) return false;
            var grid = HexGridManager.Instance?.Map;
            if (grid == null) return false;

            int steps = maxSteps > 0 ? maxSteps : ctx.Caster.MoveRange;
            var path = HexPathfinding.FindPath(grid, ctx.Caster.Position, ctx.TargetCell.Value, steps);
            if (path == null || path.Count <= 1) return false;

            // 占位与离位
            grid.GetCell(ctx.Caster.Position).Occupant = null;
            ctx.Caster.Position = path[^1].Coordinates;
            grid.GetCell(ctx.Caster.Position).Occupant = ctx.Caster;
            return true;
        }
    }

    /// <summary>制造破绽（战士嘲讽、刺客虚晃）</summary>
    [Serializable]
    public class CreateBreakPointEffect : CardEffect
    {
        public BreakPointType type;
        [Tooltip("在目标的哪个方向制造破绽，-1 = 攻击方向反向")]
        public int direction = -1;
        public int duration = 2;

        public override bool Execute(CardCastContext ctx)
        {
            if (ctx.Targets.Count == 0 || ctx.BreakPointSystem == null) return false;
            int dir = direction < 0 ? BreakPointSystem.GetHitDirection(ctx.AttackDirection) : direction;
            foreach (var t in ctx.Targets)
                ctx.BreakPointSystem.ForceSpawn(t, dir, type, duration);
            return true;
        }
    }

    /// <summary>抽 N 张卡</summary>
    [Serializable]
    public class DrawCardEffect : CardEffect
    {
        public int count = 1;
        public override bool Execute(CardCastContext ctx)
        {
            // 实际抽卡走玩家牌库；CombatEntity 不存牌库，所以 PlayerEntity 重写时实现
            if (ctx.Caster is PlayerEntity p) p.Deck.Draw(count);
            return true;
        }
    }
}
