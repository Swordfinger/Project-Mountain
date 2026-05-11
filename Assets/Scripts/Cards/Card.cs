using System;

namespace JailerGame.Cards
{
    /// <summary>
    /// 卡牌的运行时实例（Card = Data + 运行时状态如冷却）。
    /// 同一张 CardData 可以被多个 Card 实例引用（如卡组里有 2 张刺杀）。
    /// </summary>
    public class Card
    {
        public CardData Data { get; }
        public string InstanceId { get; }
        public int CurrentCooldown { get; set; } = 0;
        public bool IsLocked { get; set; } = false;
        /// <summary>临时强化（神偷"熟能生巧"用）</summary>
        public int TempDamageBonus { get; set; } = 0;

        public Card(CardData data)
        {
            Data = data;
            InstanceId = Guid.NewGuid().ToString("N");
        }

        public bool CanPlay(int currentEnergy) =>
            CurrentCooldown == 0 && currentEnergy >= Data.energyCost && !IsLocked;
    }
}
