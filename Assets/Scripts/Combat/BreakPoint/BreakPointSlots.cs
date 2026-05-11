using System;
using System.Collections.Generic;
using JailerGame.Core;

namespace JailerGame.Combat.BreakPoint
{
    /// <summary>
    /// 一个实体身上的 6 个破绽槽集合。
    /// 注意：DirectionIndex 是世界坐标方向（绝对方向），
    /// 而玩家攻击时使用的"相对方向"由 BreakPointSystem 转换。
    /// </summary>
    public class BreakPointSlots
    {
        private readonly BreakPointSlot[] _slots = new BreakPointSlot[6];
        private readonly CombatEntity _owner;
        private bool _facingDirty = true;

        public event Action<int, BreakPointSlot> OnSlotChanged; // 方向, 槽

        public BreakPointSlots(CombatEntity owner)
        {
            _owner = owner;
            for (int i = 0; i < 6; i++)
                _slots[i] = new BreakPointSlot(i);
        }

        public BreakPointSlot GetSlot(int directionIndex)
        {
            int idx = ((directionIndex % 6) + 6) % 6;
            return _slots[idx];
        }

        public IReadOnlyList<BreakPointSlot> AllSlots => _slots;

        public void Set(int direction, BreakPointType type, int turns = 0)
        {
            var slot = GetSlot(direction);
            slot.Set(type, turns);
            OnSlotChanged?.Invoke(direction, slot);
        }

        public void Consume(int direction)
        {
            var slot = GetSlot(direction);
            slot.Clear();
            OnSlotChanged?.Invoke(direction, slot);
        }

        public void TickTurnAll()
        {
            for (int i = 0; i < 6; i++)
            {
                bool wasActive = _slots[i].IsActive;
                _slots[i].TickTurn();
                if (wasActive && !_slots[i].IsActive)
                    OnSlotChanged?.Invoke(i, _slots[i]);
            }
        }

        public void MarkFacingChanged() => _facingDirty = true;
        public bool ConsumeFacingDirty()
        {
            bool was = _facingDirty;
            _facingDirty = false;
            return was;
        }

        public void ClearAll()
        {
            for (int i = 0; i < 6; i++)
            {
                _slots[i].Clear();
                OnSlotChanged?.Invoke(i, _slots[i]);
            }
        }
    }
}
