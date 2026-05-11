using System.Collections.Generic;
using JailerGame.Characters;
using JailerGame.Combat;
using JailerGame.Core;
using UnityEngine;

namespace JailerGame.BossArena
{
    /// <summary>
    /// 测试用 Boss 实体。
    /// 行为：攻击 → 攻击 → 回血 → 循环（while loop）
    /// 想换其他模式时改 _pattern 数组即可。
    /// </summary>
    public class BossEntity : CombatEntity
    {
        public int AttackDamage = 10;
        public int HealAmount = 10;

        // 行为序列：true = 攻击，false = 回血
        // 现在的模式："攻击, 攻击, 回血"
        private readonly BossActionType[] _pattern =
        {
            BossActionType.Attack,
            BossActionType.Attack,
            BossActionType.Heal,
        };
        private int _patternIndex = 0;

        public BossEntity(string id, string displayName, int maxHp, int speed)
            : base(id, maxHp, speed, 1)
        {
            DisplayName = displayName;
            Faction = EntityFaction.Gatekeeper;
        }

        /// <summary>对指定玩家执行下一招（while loop 行为）</summary>
        public List<CombatEntity> ExecuteAgainst(PlayerEntity opponent)
        {
            var hits = new List<CombatEntity>();
            if (!IsAlive) return hits;

            var act = _pattern[_patternIndex % _pattern.Length];
            _patternIndex++;

            switch (act)
            {
                case BossActionType.Attack:
                    if (opponent != null && opponent.IsAlive)
                    {
                        opponent.TakeDamage(AttackDamage, -1, null);
                        hits.Add(opponent);
                        Debug.Log($"  → Boss 攻击 {opponent.DisplayName}，造成 {AttackDamage} 伤害（剩 {opponent.CurrentHp}/{opponent.MaxHp}）");
                    }
                    break;

                case BossActionType.Heal:
                    Heal(HealAmount);
                    Debug.Log($"  → Boss 回血 {HealAmount}（{CurrentHp}/{MaxHp}）");
                    break;
            }
            return hits;
        }
    }

    public enum BossActionType
    {
        Attack,
        Heal,
    }
}
