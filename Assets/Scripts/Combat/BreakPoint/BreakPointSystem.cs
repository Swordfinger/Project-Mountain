using System.Collections.Generic;
using JailerGame.Core;
using UnityEngine;

namespace JailerGame.Combat.BreakPoint
{
    /// <summary>
    /// 破绽刷新系统：每个回合开始时为参战实体计算破绽。
    ///
    /// 设计要点（来自策划讨论）：
    /// - 每个职业看到的破绽是独立的（同一边对刺客可能没破绽、对神偷却有）
    /// - 6 边独立按概率刷新（类剑姬被动）
    /// - Boss 转身后整个破绽布局重新计算
    /// - 破绽刷新概率每个实体独立配置
    ///
    /// 这套系统不直接渲染，由 UI 监听 BreakPointSlots.OnSlotChanged 事件画图标。
    /// </summary>
    public class BreakPointSystem
    {
        public struct Config
        {
            /// <summary>每条边出现破绽的基础概率（0~1）</summary>
            public float baseSpawnChance;
            /// <summary>这个实体身上会刷哪几种 Class 的破绽（每个职业玩家只看到自己那一类）</summary>
            public BreakPointType[] activeClasses;
            /// <summary>转身后是否清空已存在的破绽（true=清空重刷，false=保留旧的+追加新的）</summary>
            public bool resetOnFacingChange;
            /// <summary>新出现的破绽默认持续几回合（0 = 永久直到被命中消耗）</summary>
            public int defaultDuration;
        }

        public Config DefaultConfig = new Config
        {
            baseSpawnChance = 0.4f,
            activeClasses = new[]
            {
                BreakPointType.Wound, BreakPointType.PickPoint,
                BreakPointType.Crack, BreakPointType.MagicWeak,
                BreakPointType.Greed, BreakPointType.Sin,
            },
            resetOnFacingChange = true,
            defaultDuration = 2,
        };

        private readonly System.Random _random;

        public BreakPointSystem(int seed = 0)
        {
            _random = seed == 0 ? new System.Random() : new System.Random(seed);
        }

        /// <summary>
        /// 在每回合开始时为某实体重新计算破绽（每个 Class 独立 6 边判定）
        /// </summary>
        public void RefreshFor(CombatEntity entity, Config? cfg = null)
        {
            var config = cfg ?? DefaultConfig;

            bool facingChanged = entity.BreakPoints.ConsumeFacingDirty();
            if (facingChanged && config.resetOnFacingChange)
                entity.BreakPoints.ClearAll();

            // 注意：这里我们让每条边独立按概率"是否刷新"
            // 如果这条边已经有破绽且未到期，跳过（保留旧的）
            for (int dir = 0; dir < 6; dir++)
            {
                var slot = entity.BreakPoints.GetSlot(dir);
                if (slot.IsActive && slot.RemainingTurns != 0) continue;

                if (_random.NextDouble() < config.baseSpawnChance)
                {
                    var type = config.activeClasses[_random.Next(config.activeClasses.Length)];
                    entity.BreakPoints.Set(dir, type, config.defaultDuration);
                }
            }
        }

        /// <summary>
        /// 玩家用某种 Class 制造破绽（战士嘲讽、刺客虚晃等技能效果）
        /// </summary>
        public void ForceSpawn(CombatEntity target, int direction, BreakPointType type, int duration = 2)
        {
            target.BreakPoints.Set(direction, type, duration);
            Debug.Log($"[BreakPoint] 强制在 {target.DisplayName} 的 {direction} 方向制造 {type} 破绽");
        }

        /// <summary>
        /// 把"攻击者世界坐标方向"转换成"目标格的破绽方向"。
        /// 例：攻击者在目标的"东边"打过来，目标的"西边"被命中（方向反转）。
        /// </summary>
        public static int GetHitDirection(int attackerToTargetWorldDir)
        {
            // 反向 = +3
            return (attackerToTargetWorldDir + 3) % 6;
        }
    }
}
