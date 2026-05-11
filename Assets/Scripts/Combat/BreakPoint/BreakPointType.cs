namespace JailerGame.Combat.BreakPoint
{
    /// <summary>
    /// 破绽类型，与职业一一对应。
    /// 每个玩家只看得见自己 Class 的破绽，互不干扰（对应 Q48 选项 B）。
    /// 之后扩展新职业（DLC）只需追加枚举值。
    /// </summary>
    public enum BreakPointType
    {
        None = 0,
        Wound = 1,       // 刺客 — 伤口
        PickPoint = 2,   // 神偷 — 可偷点
        Crack = 3,       // 战士 — 护甲缝隙
        MagicWeak = 4,   // 法师 — 魔法弱点
        Greed = 5,       // 商人 — 贪欲点
        Sin = 6,         // 牧师 — 罪孽点
    }
}
