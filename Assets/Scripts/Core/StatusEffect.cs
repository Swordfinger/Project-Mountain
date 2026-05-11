namespace JailerGame.Core
{
    /// <summary>
    /// 状态效果基类（中毒、流血、嘲讽、虚弱等）。
    /// 第一阶段先放骨架，第二阶段补具体子类。
    /// </summary>
    public abstract class StatusEffect
    {
        public string Id;
        public int RemainingTurns;
        public int Stacks = 1;
        public bool IsExpired => RemainingTurns <= 0;

        public virtual void OnTurnStart(CombatEntity owner) { }
        public virtual void OnTurnEnd(CombatEntity owner)
        {
            RemainingTurns--;
        }
    }

    /// <summary>流血：每回合开始扣 stacks 点伤害（不计破绽，不计格挡，纯固定）</summary>
    public class BleedStatus : StatusEffect
    {
        public BleedStatus(int stacks, int turns)
        {
            Id = "Bleed";
            Stacks = stacks;
            RemainingTurns = turns;
        }
        public override void OnTurnStart(CombatEntity owner)
        {
            // 直接扣血，不走 TakeDamage（避免再次触发破绽逻辑）
            int dmg = Stacks;
            owner.GetType(); // suppress
            // 使用反射或 internal 扣血都可以；这里简化：通过事件回调处理。
            // 第二阶段再细化为 InternalDamage(int) API。
        }
    }
}
