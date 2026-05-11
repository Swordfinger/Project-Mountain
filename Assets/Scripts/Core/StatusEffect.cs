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

    // BleedStatus 已挪到 JailerGame.Cards.AssassinCardEffects.cs（带完整伤害结算）
}
