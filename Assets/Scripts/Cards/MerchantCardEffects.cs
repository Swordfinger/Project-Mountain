using System;
using System.Collections.Generic;
using JailerGame.Characters;
using JailerGame.Core;
using JailerGame.Combat.BreakPoint;
using UnityEngine;

namespace JailerGame.Cards
{
    /// <summary>
    /// 商人职业的特化卡牌效果。
    ///
    /// 测试阶段约定：
    /// - 所有"伤害"统一 5 点（撒币除外，撒币伤害 = 当前局内金币）
    /// - 商人对敌人造成伤害后，获得与所用能量相同的金币（写在 BattleGoldOnDamageHook 里）
    /// - 局内金币（BattleGold）与局外金币（PlayerEntity.Gold）分开存储
    ///
    /// 通用卡牌（攻击/防御/移动）复用 DamageEffect / BlockEffect / MoveEffect。
    /// </summary>

    // ============= 0. 局内金币扩展（挂在 PlayerEntity 上） =============
    /// <summary>商人专用：局内金币管理（结算时与局外金币分开，结算后丢弃）</summary>
    public static class MerchantGoldExtensions
    {
        // 用静态字典存"局内金币"，每次新战斗清空
        private static readonly Dictionary<string, int> _battleGold = new();

        public static int GetBattleGold(this PlayerEntity p)
        {
            if (p == null) return 0;
            return _battleGold.TryGetValue(p.Id, out var v) ? v : 0;
        }
        public static void AddBattleGold(this PlayerEntity p, int amount)
        {
            if (p == null || amount == 0) return;
            int cur = p.GetBattleGold();
            _battleGold[p.Id] = Mathf.Max(0, cur + amount);
            Debug.Log($"[Merchant] {p.DisplayName} 局内金币 {cur} → {_battleGold[p.Id]}（{(amount >= 0 ? "+" : "")}{amount}）");
        }
        public static bool TrySpendBattleGold(this PlayerEntity p, int amount)
        {
            if (p == null || amount <= 0) return amount == 0;
            int cur = p.GetBattleGold();
            if (cur < amount) return false;
            _battleGold[p.Id] = cur - amount;
            return true;
        }
        public static void ResetBattleGold(this PlayerEntity p)
        {
            if (p == null) return;
            _battleGold[p.Id] = 0;
        }

        /// <summary>暴利时刻倍率（默认 1，使用"暴利时刻"后变 2，状态结束恢复）</summary>
        private static readonly Dictionary<string, float> _profitMultiplier = new();
        public static float GetProfitMultiplier(this PlayerEntity p) =>
            _profitMultiplier.TryGetValue(p.Id, out var v) && v > 0 ? v : 1f;
        public static void SetProfitMultiplier(this PlayerEntity p, float mul) =>
            _profitMultiplier[p.Id] = mul;
    }

    // ============= 1. 攻击 =============
    // 直接用 DamageEffect { damage = 5, critMultiplier = 2.0f }
    // 配合下面的 GoldGainOnAttackEffect 一起放到 effects[] 里。

    /// <summary>
    /// 商人攻击副效果：造成伤害后获得 = 该卡能量消耗 x 倍率 的局内金币。
    /// 通常作为 DamageEffect 后面的第二个 effect。
    /// </summary>
    [Serializable]
    public class GoldGainOnAttackEffect : CardEffect
    {
        [Tooltip("基础获得金币（默认等于所用能量；填 0 = 自动用 ctx 中的卡牌能量消耗）")]
        public int baseGold = 0;

        public override bool Execute(CardCastContext ctx)
        {
            if (ctx.Caster is not PlayerEntity p) return false;
            // 暴利时刻倍率
            float mul = p.GetProfitMultiplier();
            int gain = Mathf.RoundToInt(Mathf.Max(1, baseGold) * mul);
            p.AddBattleGold(gain);
            return true;
        }
    }

    // ============= 2. 防御 =============
    // 用 BlockEffect { blockAmount = 5, targetSelf = true }

    // ============= 3. 移动 =============
    // 用 MoveEffect { maxSteps = 1 }

    // ============= 4. 撒币 =============
    /// <summary>
    /// 撒币：花光（或部分）局内金币，造成等额伤害。
    /// 测试阶段：花费所有局内金币，造成伤害 = 金币数。
    /// </summary>
    [Serializable]
    public class ScatterGoldEffect : CardEffect
    {
        [Tooltip("一次最多撒多少金币（0 = 全部）")]
        public int maxSpend = 0;

        public override bool Execute(CardCastContext ctx)
        {
            if (ctx.Caster is not PlayerEntity p) return false;
            if (ctx.Targets == null || ctx.Targets.Count == 0) return false;

            int avail = p.GetBattleGold();
            int spend = (maxSpend > 0) ? Mathf.Min(maxSpend, avail) : avail;
            if (spend <= 0)
            {
                Debug.Log($"[Card] 撒币：{p.DisplayName} 没有局内金币可花");
                return false;
            }
            p.TrySpendBattleGold(spend);
            ctx.GoldSpent = spend;

            int hitDir = BreakPointSystem.GetHitDirection(ctx.AttackDirection);
            foreach (var t in ctx.Targets)
            {
                t.TakeDamage(spend, hitDir, ctx.CasterClass);
                Debug.Log($"[Card] 撒币：{p.DisplayName} 砸了 {spend} 金币，对 {t.DisplayName} 造成 {spend} 伤害");
            }
            return true;
        }
    }

    // ============= 5. 百宝箱 =============
    /// <summary>
    /// 百宝箱：从仓库随机一个圣遗物给目标，本次战斗结束后归还。
    /// 测试阶段：仅打日志 + 用 RelicInstance 模拟一个临时圣遗物。
    /// </summary>
    [Serializable]
    public class TreasureChestEffect : CardEffect
    {
        public string[] possibleRelics = { "RELIC_LUCKY_COIN", "RELIC_SHIELD_PIN", "RELIC_RING" };

        public override bool Execute(CardCastContext ctx)
        {
            if (ctx.Caster is not PlayerEntity caster) return false;
            if (ctx.Targets == null || ctx.Targets.Count == 0) return false;

            string id = possibleRelics.Length > 0
                ? possibleRelics[UnityEngine.Random.Range(0, possibleRelics.Length)]
                : "RELIC_RANDOM";

            foreach (var t in ctx.Targets)
            {
                if (t is PlayerEntity ally)
                {
                    ally.Relics.Add(new RelicInstance { RelicId = id, DisplayName = "[临时] " + id, StackCount = 1 });
                    Debug.Log($"[Card] 百宝箱：{caster.DisplayName} 给 {ally.DisplayName} 临时圣遗物 [{id}]（战斗结束后归还）");
                }
            }
            return true;
        }
    }

    // ============= 6. 以物换物 =============
    /// <summary>
    /// 以物换物：与目标交换"同种类"的物品（含属性提升），持续 2 回合后归还。
    /// 测试阶段：交换双方的 BaseSpeed 数值并 2 回合后还原。
    /// </summary>
    [Serializable]
    public class BarterEffect : CardEffect
    {
        public int duration = 2;

        public override bool Execute(CardCastContext ctx)
        {
            if (ctx.Caster is not PlayerEntity p) return false;
            if (ctx.Targets == null || ctx.Targets.Count == 0) return false;
            var target = ctx.Targets[0];

            int origCaster = p.BaseSpeed;
            int origTarget = target.BaseSpeed;
            p.BaseSpeed = origTarget;
            target.BaseSpeed = origCaster;

            // 用一个状态在到期时还原
            p.StatusEffects.Add(new BarterRevertStatus(p, origCaster, target, origTarget, duration));
            Debug.Log($"[Card] 以物换物：{p.DisplayName}({origCaster}) ↔ {target.DisplayName}({origTarget}) 速度交换 {duration} 回合");
            return true;
        }
    }

    public class BarterRevertStatus : StatusEffect
    {
        private readonly PlayerEntity _caster;
        private readonly int _casterOrig;
        private readonly CombatEntity _target;
        private readonly int _targetOrig;

        public BarterRevertStatus(PlayerEntity caster, int casterOrig, CombatEntity target, int targetOrig, int turns)
        {
            Id = "STATUS_BARTER";
            _caster = caster; _casterOrig = casterOrig;
            _target = target; _targetOrig = targetOrig;
            RemainingTurns = turns; Stacks = 1;
        }
        public override void OnTurnStart(CombatEntity owner) { }
        public override void OnTurnEnd(CombatEntity owner)
        {
            RemainingTurns--;
            if (RemainingTurns <= 0)
            {
                _caster.BaseSpeed = _casterOrig;
                _target.BaseSpeed = _targetOrig;
                Debug.Log($"[Status] 以物换物到期：{_caster.DisplayName} 与 {_target.DisplayName} 速度归还");
            }
        }
    }

    // ============= 7. 购物时间 =============
    /// <summary>
    /// 购物时间：对局内打开商店，可租用任意物品。
    /// 测试阶段：仅打日志 + 给玩家 +20 局内金币（作为"借出"的占位）。
    /// </summary>
    [Serializable]
    public class ShopTimeEffect : CardEffect
    {
        public int loanGold = 20;
        public override bool Execute(CardCastContext ctx)
        {
            if (ctx.Caster is not PlayerEntity p) return false;
            p.AddBattleGold(loanGold);
            Debug.Log($"[Card] 购物时间：{p.DisplayName} 打开商店并借出 {loanGold} 金币（占位）");
            return true;
        }
    }

    // ============= 8. 暴利时刻 =============
    /// <summary>
    /// 暴利时刻：5 回合内获得的局内金币翻倍。
    /// </summary>
    [Serializable]
    public class ProfitTimeEffect : CardEffect
    {
        public int duration = 5;
        public float multiplier = 2f;

        public override bool Execute(CardCastContext ctx)
        {
            if (ctx.Caster is not PlayerEntity p) return false;
            p.SetProfitMultiplier(multiplier);
            p.StatusEffects.Add(new ProfitTimeStatus(p, multiplier, duration));
            Debug.Log($"[Card] 暴利时刻：{p.DisplayName} 局内金币获取 x{multiplier}，持续 {duration} 回合");
            return true;
        }
    }

    public class ProfitTimeStatus : StatusEffect
    {
        private readonly PlayerEntity _player;
        private readonly float _mul;
        public ProfitTimeStatus(PlayerEntity player, float mul, int turns)
        {
            Id = "STATUS_PROFIT_TIME";
            _player = player; _mul = mul;
            RemainingTurns = turns; Stacks = 1;
        }
        public override void OnTurnStart(CombatEntity owner) { }
        public override void OnTurnEnd(CombatEntity owner)
        {
            RemainingTurns--;
            if (RemainingTurns <= 0)
            {
                _player.SetProfitMultiplier(1f);
                Debug.Log($"[Status] 暴利时刻结束：{_player.DisplayName} 金币倍率恢复 x1");
            }
        }
    }

    // ============= 9. 废物利用 =============
    /// <summary>
    /// 废物利用：将任意张手牌（默认 1 张）丢入弃牌堆，获得这些卡能量消耗总和的局内金币。
    /// 测试阶段：丢弃手牌中能量消耗最高的一张。
    /// </summary>
    [Serializable]
    public class RecycleEffect : CardEffect
    {
        public int discardCount = 1;
        public override bool Execute(CardCastContext ctx)
        {
            if (ctx.Caster is not PlayerEntity p) return false;
            int gold = 0;
            for (int i = 0; i < discardCount; i++)
            {
                if (p.Deck.Hand.Count == 0) break;
                Card best = null;
                foreach (var c in p.Deck.Hand)
                    if (c.Data != null && c.Data.cardId != "MER_009"  // 排除"废物利用"自己
                        && (best == null || c.Data.energyCost > best.Data.energyCost))
                        best = c;
                if (best == null) break;
                gold += Mathf.Max(1, best.Data.energyCost);
                p.Deck.Discard(best);
            }
            float mul = p.GetProfitMultiplier();
            int gained = Mathf.RoundToInt(gold * mul);
            p.AddBattleGold(gained);
            Debug.Log($"[Card] 废物利用：{p.DisplayName} 弃牌共 {gold} 能耗 → 获 {gained} 金币（x{mul}）");
            return true;
        }
    }

    // ============= 10. 时间就是金钱 =============
    /// <summary>
    /// 时间就是金钱：对即将死亡的目标使用，让其免除死亡，
    /// 直到金币消耗完为止；伤害 1:1 消耗金币。
    /// 测试阶段：给目标添加 GoldShieldStatus，TakeDamage 前会先扣金币。
    /// </summary>
    [Serializable]
    public class TimeIsMoneyEffect : CardEffect
    {
        public override bool Execute(CardCastContext ctx)
        {
            if (ctx.Caster is not PlayerEntity p) return false;
            if (ctx.Targets == null || ctx.Targets.Count == 0) return false;
            int gold = p.GetBattleGold();
            if (gold <= 0)
            {
                Debug.Log($"[Card] 时间就是金钱：{p.DisplayName} 没有金币，无效");
                return false;
            }
            foreach (var t in ctx.Targets)
            {
                t.StatusEffects.Add(new GoldShieldStatus(p, gold));
                Debug.Log($"[Card] 时间就是金钱：{t.DisplayName} 获得 {gold} 金币护盾，由 {p.DisplayName} 提供");
            }
            // 立即扣空局内金币（占位实现，真实结算在 GoldShieldStatus 中按伤害扣）
            // 真实实现需要在 TakeDamage 路径中拦截，这里先用状态记录
            return true;
        }
    }

    public class GoldShieldStatus : StatusEffect
    {
        public readonly PlayerEntity Provider;
        public int RemainingGold;
        public GoldShieldStatus(PlayerEntity provider, int gold)
        {
            Id = "STATUS_GOLD_SHIELD";
            Provider = provider;
            RemainingGold = gold;
            Stacks = gold;
            RemainingTurns = -1; // 持续到金币耗尽
        }
        public override void OnTurnStart(CombatEntity owner) { }
        public override void OnTurnEnd(CombatEntity owner)
        {
            if (RemainingGold <= 0) RemainingTurns = 0; // 标记到期
        }
    }

    // ============= 11. 放弃决策 =============
    /// <summary>
    /// 放弃决策：中断（取消）一张正在结算的卡牌效果。
    /// 测试阶段：给场上所有玩家添加 NextCardCancelStatus，
    /// 下一张被结算的卡直接被跳过。
    /// </summary>
    [Serializable]
    public class CancelDecisionEffect : CardEffect
    {
        public override bool Execute(CardCastContext ctx)
        {
            if (ctx.Targets == null || ctx.Targets.Count == 0)
            {
                Debug.Log($"[Card] 放弃决策：未指定目标，跳过");
                return false;
            }
            foreach (var t in ctx.Targets)
            {
                t.StatusEffects.Add(new NextCardCancelStatus(1));
                Debug.Log($"[Card] 放弃决策：{t.DisplayName} 下一张卡将被中断");
            }
            return true;
        }
    }

    public class NextCardCancelStatus : StatusEffect
    {
        public NextCardCancelStatus(int turns)
        {
            Id = "STATUS_CARD_CANCEL";
            RemainingTurns = turns;
            Stacks = 1;
        }
        public override void OnTurnStart(CombatEntity owner) { }
        public override void OnTurnEnd(CombatEntity owner) { RemainingTurns--; }
    }
}
