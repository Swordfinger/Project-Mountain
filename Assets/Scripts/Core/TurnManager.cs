using System;
using System.Collections;
using System.Collections.Generic;
using JailerGame.Cards;
using JailerGame.Characters;
using JailerGame.Combat;
using JailerGame.Combat.BreakPoint;
using JailerGame.Combat.HexGrid;
using UnityEngine;

namespace JailerGame.Core
{
    /// <summary>
    /// 战斗回合管理器：驱动"准备 → 提交 → 结算 → Boss 回合"的完整流程。
    /// 同时出牌 + 30s 限时 + 速度结算（基础速度 → 离 Boss 距离）。
    ///
    /// 这是 MonoBehaviour，但所有重要逻辑在协程里调纯 C# 系统。
    /// 联机时把这层换成"接收服务器命令"的实现即可。
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        public static TurnManager Instance { get; private set; }

        [Header("回合时间")]
        public float planPhaseSeconds = 30f;

        [Header("破绽刷新随机种子（0=每次开局随机）")]
        public int breakPointSeed = 0;

        public List<PlayerEntity> Players { get; } = new();
        public List<EnemyEntity> Enemies { get; } = new();

        public int CurrentTurn { get; private set; } = 0;
        public TurnPhase Phase { get; private set; } = TurnPhase.Idle;

        public event Action<int> OnTurnStarted;
        public event Action<int> OnTurnEnded;
        public event Action<TurnPhase> OnPhaseChanged;

        private BreakPointSystem _breakPoint;
        // 玩家排好队的"待执行"卡牌（同时出牌阶段每人累积，结算时按速度排序）
        private readonly List<PendingCard> _pendingCards = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _breakPoint = new BreakPointSystem(breakPointSeed);
        }

        public void RegisterPlayer(PlayerEntity p) { if (!Players.Contains(p)) Players.Add(p); }
        public void RegisterEnemy(EnemyEntity e) { if (!Enemies.Contains(e)) Enemies.Add(e); }

        public void StartCombat()
        {
            CurrentTurn = 0;
            StartCoroutine(BattleLoop());
        }

        private IEnumerator BattleLoop()
        {
            while (true)
            {
                if (!HasAlivePlayer()) { Debug.Log("[Turn] 所有告密者倒下，战斗失败"); yield break; }
                if (!HasAliveEnemy())  { Debug.Log("[Turn] 所有敌人击败，战斗胜利"); yield break; }

                CurrentTurn++;
                OnTurnStarted?.Invoke(CurrentTurn);
                Debug.Log($"========== 回合 {CurrentTurn} 开始 ==========");

                // 1. 回合开始：能量、抽牌、状态结算、破绽刷新
                yield return StartPhase(TurnPhase.TurnStart);
                BeginTurnUpkeep();

                // 2. 准备阶段：玩家选卡（30s 倒计时）
                yield return StartPhase(TurnPhase.PlanPhase);
                yield return WaitForPlanPhase();

                // 3. 结算阶段：按速度执行所有"待执行"卡牌
                yield return StartPhase(TurnPhase.ResolvePhase);
                ResolvePendingCards();

                // 4. Boss/敌人回合
                yield return StartPhase(TurnPhase.EnemyPhase);
                ExecuteEnemyTurns();

                // 5. 回合结束清理
                yield return StartPhase(TurnPhase.TurnEnd);
                EndTurnCleanup();
                OnTurnEnded?.Invoke(CurrentTurn);

                yield return null;
            }
        }

        private IEnumerator StartPhase(TurnPhase phase)
        {
            Phase = phase;
            OnPhaseChanged?.Invoke(phase);
            Debug.Log($"[Phase] → {phase}");
            yield return null;
        }

        private void BeginTurnUpkeep()
        {
            foreach (var p in Players)
            {
                if (!p.IsAlive) continue;
                p.StartTurn();
                p.RefillEnergy();
                p.Deck.TickCooldowns();
                p.Deck.Draw(2); // 每回合抽 2 张，可调
                _breakPoint.RefreshFor(p); // 玩家破绽（让 Boss 攻击玩家时也能命中）
            }
            foreach (var e in Enemies)
            {
                if (!e.IsAlive) continue;
                e.StartTurn();
                e.BreakPoints.TickTurnAll();
                _breakPoint.RefreshFor(e);
            }
        }

        private IEnumerator WaitForPlanPhase()
        {
            // 真实游戏里玩家在这阶段提交卡牌（通过 SubmitCard 接口）
            // 第一阶段先靠 PlayerAI 或测试代码自动调 SubmitCard 来模拟
            float t = planPhaseSeconds;
            while (t > 0 && !AllPlayersLockedIn())
            {
                t -= Time.deltaTime;
                yield return null;
            }
        }

        private bool AllPlayersLockedIn() => false; // 第一阶段总返回 false，等倒计时

        /// <summary>玩家在准备阶段调用此接口提交一张要打的卡（同时出牌）</summary>
        public void SubmitCard(PlayerEntity caster, Card card, CardCastContext partialContext)
        {
            if (Phase != TurnPhase.PlanPhase)
            {
                Debug.LogWarning("[Turn] 不在准备阶段，无法提交卡牌");
                return;
            }
            if (!card.CanPlay(caster.Energy)) return;
            if (!caster.TrySpendEnergy(card.Data.energyCost)) return;

            partialContext.Caster = caster;
            partialContext.BreakPointSystem = _breakPoint;
            _pendingCards.Add(new PendingCard { Card = card, Context = partialContext, Caster = caster });
        }

        private void ResolvePendingCards()
        {
            // FirstStrike 优先
            _pendingCards.Sort((a, b) =>
            {
                int ta = (int)a.Card.Data.timing;
                int tb = (int)b.Card.Data.timing;
                if (ta != tb) return ta == (int)CardTiming.FirstStrike ? -1
                                  : tb == (int)CardTiming.FirstStrike ? 1 : ta.CompareTo(tb);

                int sa = a.Caster.BaseSpeed + a.Card.Data.speedModifier;
                int sb = b.Caster.BaseSpeed + b.Card.Data.speedModifier;
                if (sa != sb) return sb.CompareTo(sa); // 速度高先动

                int da = NearestEnemyDistance(a.Caster);
                int db = NearestEnemyDistance(b.Caster);
                return da.CompareTo(db); // 离 Boss 近的先动
            });

            foreach (var pc in _pendingCards)
            {
                if (!pc.Caster.IsAlive) continue;
                Debug.Log($"[Resolve] {pc.Caster.DisplayName} 打出 [{pc.Card.Data.cardName}]");
                foreach (var fx in pc.Card.Data.effects)
                    fx.Execute(pc.Context);
                pc.Caster.Deck.Discard(pc.Card);
                if (pc.Card.Data.cooldownTurns > 0)
                    pc.Card.CurrentCooldown = pc.Card.Data.cooldownTurns;
            }
            _pendingCards.Clear();
        }

        private int NearestEnemyDistance(PlayerEntity p)
        {
            int min = int.MaxValue;
            foreach (var e in Enemies)
            {
                if (!e.IsAlive) continue;
                int d = HexCoordinates.Distance(p.Position, e.Position);
                if (d < min) min = d;
            }
            return min == int.MaxValue ? 0 : min;
        }

        private void ExecuteEnemyTurns()
        {
            var alivePlayers = new List<CombatEntity>();
            foreach (var p in Players) if (p.IsAlive) alivePlayers.Add(p);
            foreach (var e in Enemies)
            {
                if (!e.IsAlive) continue;
                e.ExecuteAI(alivePlayers);
            }
        }

        private void EndTurnCleanup()
        {
            foreach (var p in Players) if (p.IsAlive) p.EndTurn();
            foreach (var e in Enemies) if (e.IsAlive) e.EndTurn();
        }

        private bool HasAlivePlayer()
        {
            foreach (var p in Players) if (p.IsAlive) return true;
            return false;
        }
        private bool HasAliveEnemy()
        {
            foreach (var e in Enemies) if (e.IsAlive) return true;
            return false;
        }

        private struct PendingCard
        {
            public Card Card;
            public CardCastContext Context;
            public PlayerEntity Caster;
        }
    }

    public enum TurnPhase
    {
        Idle,
        TurnStart,
        PlanPhase,
        ResolvePhase,
        EnemyPhase,
        TurnEnd,
    }
}
