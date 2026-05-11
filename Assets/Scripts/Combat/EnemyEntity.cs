using System.Collections.Generic;
using JailerGame.Combat.HexGrid;
using JailerGame.Combat.BreakPoint;
using JailerGame.Core;
using UnityEngine;

namespace JailerGame.Combat
{
    /// <summary>
    /// 敌人战斗实体。AI 在 EndTurn 阶段决策下一步行动。
    /// </summary>
    public class EnemyEntity : CombatEntity
    {
        public EnemyData Data { get; }
        public int CurrentDamage { get; }

        public EnemyActionPattern NextAction { get; private set; }

        private readonly System.Random _rng;

        public EnemyEntity(string id, EnemyData data, int playerCount, int seed = 0)
            : base(id, ScaledHp(data, playerCount), data.baseSpeed, data.moveRange)
        {
            Data = data;
            DisplayName = data.displayName;
            CurrentDamage = ScaledDamage(data, playerCount);
            _rng = seed == 0 ? new System.Random() : new System.Random(seed);
            Faction = data.type switch
            {
                EnemyType.Minion         => EntityFaction.EnemyMinion,
                EnemyType.Elite          => EntityFaction.EnemyElite,
                EnemyType.Gatekeeper     => EntityFaction.Gatekeeper,
                EnemyType.SealedEmissary => EntityFaction.SealedEmissary,
                EnemyType.FinalBoss      => EntityFaction.FinalBoss,
                _                        => EntityFaction.EnemyMinion,
            };
            DecideNextAction();
        }

        private static int ScaledHp(EnemyData data, int playerCount) =>
            Mathf.RoundToInt(data.baseMaxHp * (1f + (playerCount - 1) * data.hpScalePerPlayer));

        private static int ScaledDamage(EnemyData data, int playerCount) =>
            Mathf.RoundToInt(data.baseDamage * (1f + (playerCount - 1) * data.damageScalePerPlayer));

        public void DecideNextAction()
        {
            if (Data.actionPatterns == null || Data.actionPatterns.Count == 0) return;
            int totalWeight = 0;
            foreach (var p in Data.actionPatterns) totalWeight += Mathf.Max(1, p.weight);
            int roll = _rng.Next(totalWeight);
            int acc = 0;
            foreach (var p in Data.actionPatterns)
            {
                acc += Mathf.Max(1, p.weight);
                if (roll < acc) { NextAction = p; return; }
            }
            NextAction = Data.actionPatterns[0];
        }

        /// <summary>执行 AI 行动，返回受影响的玩家</summary>
        public List<CombatEntity> ExecuteAI(List<CombatEntity> playerEntities)
        {
            var hits = new List<CombatEntity>();
            if (NextAction == null) return hits;

            switch (NextAction.actionType)
            {
                case EnemyActionType.Attack:
                {
                    if (playerEntities.Count == 0) break;
                    var target = playerEntities[_rng.Next(playerEntities.Count)];
                    int dirToTarget = HexCoordinates.DirectionTo(Position, target.Position);
                    if (dirToTarget < 0) dirToTarget = 0;
                    target.TakeDamage(CurrentDamage, BreakPointSystem.GetHitDirection(dirToTarget), null);
                    hits.Add(target);
                    Debug.Log($"[Enemy] {DisplayName} 攻击 {target.DisplayName}，造成 {CurrentDamage} 伤害");
                    break;
                }
                case EnemyActionType.AOEAttack:
                {
                    foreach (var t in playerEntities)
                    {
                        t.TakeDamage(NextAction.value, -1, null);
                        hits.Add(t);
                    }
                    Debug.Log($"[Enemy] {DisplayName} 范围攻击，造成 {NextAction.value} 伤害");
                    break;
                }
                case EnemyActionType.Defend:
                {
                    Block += NextAction.value;
                    Debug.Log($"[Enemy] {DisplayName} 增加 {NextAction.value} 格挡");
                    break;
                }
                case EnemyActionType.Heal:
                {
                    Heal(NextAction.value);
                    break;
                }
            }

            // 转身（破绽刷新由 BreakPointSystem 在下回合开始时处理）
            if (NextAction.turnAfterAction)
            {
                int newFacing = _rng.Next(6);
                SetFacing(newFacing);
            }

            DecideNextAction(); // 决定下一回合招式
            return hits;
        }
    }

}
