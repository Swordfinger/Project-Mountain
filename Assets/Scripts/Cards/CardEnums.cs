namespace JailerGame.Cards
{
    public enum CardRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary,
    }

    public enum CardCategory
    {
        Attack,    // 攻击
        Skill,     // 技能（buff、debuff、控制）
        Power,     // 持续效果（整局生效）
        Movement,  // 位移
        Special,   // 特殊（殊死一搏、按兵不动等）
    }

    public enum CardTargetType
    {
        Self,          // 自己
        SingleEnemy,   // 单个敌人
        SingleAlly,    // 单个友军
        AllEnemies,    // 所有敌人
        AllAllies,     // 所有友军
        HexCell,       // 指定格子（位移、AOE）
        None,          // 无目标（按兵不动）
    }

    /// <summary>
    /// 卡牌打出时机：决定速度结算阶段它在哪一拍执行
    /// </summary>
    public enum CardTiming
    {
        Normal,    // 正常按速度结算
        FirstStrike, // 优先打出（"暗中前行"那种 must-play-first）
        Reaction,  // 反应卡（敌方动作后触发，"反客为主"）
        EndOfTurn, // 回合结束
    }
}
