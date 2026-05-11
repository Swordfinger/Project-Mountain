using System;
using System.Collections.Generic;
using JailerGame.Combat.HexGrid;
using JailerGame.Combat.BreakPoint;

namespace JailerGame.Core
{
    /// <summary>
    /// 所有可参战实体（玩家、敌人、Boss、被封印的使者）的逻辑基类。
    /// 纯 C#，不挂 MonoBehaviour，便于在服务端权威逻辑里跑。
    /// 视觉部分由 CombatEntityView (MonoBehaviour) 负责显示。
    /// </summary>
    public abstract class CombatEntity
    {
        public string Id { get; }
        public string DisplayName { get; set; }
        public EntityFaction Faction { get; set; }

        // —— 数值 ——
        public int MaxHp { get; set; }
        public int CurrentHp { get; protected set; }
        public int Block { get; set; } // 格挡，受伤前先扣
        public int BaseSpeed { get; set; } // 速度结算用，越高越先动
        public int MoveRange { get; set; }

        // —— 位置与朝向 ——
        public HexCoordinates Position { get; set; }
        /// <summary>朝向：0~5 对应 HexCoordinates.Directions 索引；Boss 转身会改这个值</summary>
        public int FacingDirection { get; set; }

        // —— 状态 ——
        public bool IsAlive => CurrentHp > 0;
        public List<StatusEffect> StatusEffects { get; } = new();

        // —— 破绽（每个实体身上都有 6 边的破绽槽，玩家和 Boss 都有）——
        public BreakPointSlots BreakPoints { get; }

        // —— 事件（UI/音效/网络都靠订阅这些）——
        public event Action<CombatEntity, int> OnDamaged;       // 实体, 实际伤害
        public event Action<CombatEntity, int> OnHealed;        // 实体, 实际回复
        public event Action<CombatEntity> OnDied;
        public event Action<CombatEntity, int> OnFacingChanged; // 实体, 新朝向

        protected CombatEntity(string id, int maxHp, int baseSpeed, int moveRange)
        {
            Id = id;
            MaxHp = maxHp;
            CurrentHp = maxHp;
            BaseSpeed = baseSpeed;
            MoveRange = moveRange;
            BreakPoints = new BreakPointSlots(this);
        }

        public virtual void TakeDamage(int rawDamage, int directionFromAttacker = -1, BreakPointType? attackerClass = null)
        {
            int damage = rawDamage;

            // 破绽判定：如果攻击方向命中破绽，伤害翻倍
            if (directionFromAttacker >= 0 && attackerClass.HasValue)
            {
                var hit = BreakPoints.GetSlot(directionFromAttacker);
                if (hit.IsActive && hit.Type == attackerClass.Value)
                {
                    damage = (int)Math.Ceiling(damage * 2f);
                    BreakPoints.Consume(directionFromAttacker); // 命中后消耗，下回合按概率重刷
                }
            }

            // 格挡先抵
            if (Block > 0)
            {
                int absorbed = Math.Min(Block, damage);
                Block -= absorbed;
                damage -= absorbed;
            }

            if (damage > 0)
            {
                CurrentHp = Math.Max(0, CurrentHp - damage);
                OnDamaged?.Invoke(this, damage);
                if (CurrentHp == 0) Die();
            }
        }

        public virtual void Heal(int amount)
        {
            int actual = Math.Min(amount, MaxHp - CurrentHp);
            CurrentHp += actual;
            OnHealed?.Invoke(this, actual);
        }

        public virtual void SetFacing(int dir)
        {
            int normalized = ((dir % 6) + 6) % 6;
            if (normalized == FacingDirection) return;
            FacingDirection = normalized;
            OnFacingChanged?.Invoke(this, normalized);
            // 转身后，破绽位置一般会被重新刷（具体策略由 BreakPointManager 在回合开始时处理）
            BreakPoints.MarkFacingChanged();
        }

        protected virtual void Die()
        {
            OnDied?.Invoke(this);
        }

        public void StartTurn()
        {
            // 回合开始：清空格挡（每回合刷新格挡值是常见设计，杀戮尖塔同款）
            Block = 0;

            // 处理 DOT/恢复类状态
            for (int i = StatusEffects.Count - 1; i >= 0; i--)
            {
                StatusEffects[i].OnTurnStart(this);
                if (StatusEffects[i].IsExpired) StatusEffects.RemoveAt(i);
            }
        }

        public void EndTurn()
        {
            for (int i = StatusEffects.Count - 1; i >= 0; i--)
            {
                StatusEffects[i].OnTurnEnd(this);
                if (StatusEffects[i].IsExpired) StatusEffects.RemoveAt(i);
            }
        }
    }

    /// <summary>实体阵营（决定卡牌作用对象、AI 仇恨、胜负判定）</summary>
    public enum EntityFaction
    {
        Player,        // 告密者玩家
        Emissary,      // 使者（表面是 Player，内部标记）
        SealedEmissary,// 被封印的使者（大魔王副本）
        EnemyMinion,   // 普通怪
        EnemyElite,    // 精英怪
        Gatekeeper,    // 守门人
        FinalBoss,     // 狱卒使（最终 Boss）
    }
}
