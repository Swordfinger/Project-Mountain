using JailerGame.Combat.BreakPoint;

namespace JailerGame.Characters
{
    /// <summary>
    /// 刺客职业逻辑（特化行为）。
    /// 第一阶段它和 PlayerEntity 行为基本一致，靠卡牌做差异化。
    /// 后续如果有"职业被动"（如刺客每回合首次攻击若命中破绽再 +50% 伤害）放在这里。
    /// </summary>
    public class Assassin : PlayerEntity
    {
        public Assassin(string id, CharacterData data) : base(id, data)
        {
        }

        // 占位：刺客被动 - 第一击命中破绽时额外伤害
        public bool FirstAttackBonusReady = true;

        public BreakPointType ClassBreakPoint => BreakPointType.Wound;
    }
}
