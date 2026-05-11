namespace JailerGame.Combat.BreakPoint
{
    /// <summary>
    /// 蜂窝格 6 条边中的一条上的"破绽槽"。
    /// 每个实体身上 6 条边各有一个槽，共 6 个槽。
    /// 每个槽独立刷新（类似剑姬被动的 4 个破绽点）。
    /// </summary>
    public class BreakPointSlot
    {
        public int DirectionIndex;       // 0~5，世界坐标方向（与朝向无关，绝对方向）
        public BreakPointType Type;      // 当前是什么类型的破绽（None=无）
        public bool IsActive => Type != BreakPointType.None;
        public int RemainingTurns;       // 剩余持续回合，0 = 永久直到被消耗或刷新

        public BreakPointSlot(int directionIndex)
        {
            DirectionIndex = directionIndex;
            Type = BreakPointType.None;
        }

        public void Set(BreakPointType type, int turns = 0)
        {
            Type = type;
            RemainingTurns = turns;
        }

        public void Clear()
        {
            Type = BreakPointType.None;
            RemainingTurns = 0;
        }

        public void TickTurn()
        {
            if (!IsActive) return;
            if (RemainingTurns <= 0) return; // 0 = 永久
            RemainingTurns--;
            if (RemainingTurns == 0) Clear();
        }
    }
}
