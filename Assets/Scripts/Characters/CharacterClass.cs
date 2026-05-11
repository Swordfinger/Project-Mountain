using JailerGame.Combat.BreakPoint;

namespace JailerGame.Characters
{
    /// <summary>6 个职业枚举。新增 DLC 角色时追加。</summary>
    public enum CharacterClass
    {
        Assassin,  // 刺客
        Thief,     // 神偷
        Merchant,  // 商人
        Warrior,   // 战士
        Priest,    // 牧师
        Mage,      // 法师
    }

    public static class CharacterClassExtensions
    {
        /// <summary>每个职业对应的破绽 Class（玩家只看得见自己这一类）</summary>
        public static BreakPointType ToBreakPointType(this CharacterClass cls) => cls switch
        {
            CharacterClass.Assassin => BreakPointType.Wound,
            CharacterClass.Thief    => BreakPointType.PickPoint,
            CharacterClass.Merchant => BreakPointType.Greed,
            CharacterClass.Warrior  => BreakPointType.Crack,
            CharacterClass.Priest   => BreakPointType.Sin,
            CharacterClass.Mage     => BreakPointType.MagicWeak,
            _ => BreakPointType.None,
        };
    }
}
