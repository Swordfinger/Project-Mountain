using System.Collections.Generic;
using JailerGame.Cards;
using JailerGame.Core;
using JailerGame.Identity;
using UnityEngine;

namespace JailerGame.Characters
{
    /// <summary>
    /// 玩家实体。继承自 CombatEntity，但拥有牌库、能量、金币、圣遗物等玩家特有属性。
    /// </summary>
    public class PlayerEntity : CombatEntity
    {
        public CharacterClass Class { get; }
        public CharacterData Data { get; }
        public Deck Deck { get; }

        public int Gold { get; set; }
        public int Energy { get; private set; }
        public int MaxEnergy { get; }

        /// <summary>身份（普通告密者 / 使者）。这是 IdentityManager 给玩家分配的隐藏标记。</summary>
        public PlayerIdentity Identity { get; set; } = PlayerIdentity.Informer;

        public List<RelicInstance> Relics { get; } = new();

        public PlayerEntity(string id, CharacterData data) : base(id, data.maxHp, data.baseSpeed, data.moveRange)
        {
            Data = data;
            Class = data.characterClass;
            DisplayName = data.displayName;
            MaxEnergy = data.maxEnergyPerTurn;
            Energy = MaxEnergy;
            Faction = EntityFaction.Player;
            Deck = new Deck(data.startingDeck);
            Deck.Draw(data.initialHandSize);
        }

        /// <summary>每回合开始时调用</summary>
        public void RefillEnergy() => Energy = MaxEnergy;

        public bool TrySpendEnergy(int amount)
        {
            if (Energy < amount) return false;
            Energy -= amount;
            return true;
        }

        public void RefundEnergy(int amount) => Energy = Mathf.Min(MaxEnergy, Energy + amount);
    }

    /// <summary>圣遗物运行时实例（神偷"拿来"用）</summary>
    public class RelicInstance
    {
        public string RelicId;
        public string DisplayName;
        public int StackCount = 1;
        // 第二阶段补完整圣遗物效果系统
    }
}
