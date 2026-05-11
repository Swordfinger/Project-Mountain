using System.Collections.Generic;
using JailerGame.Cards;
using UnityEngine;

namespace JailerGame.Characters
{
    /// <summary>
    /// 角色基础数据资产。
    /// 在 Unity 里右键 Create → JailerGame → Character Data 创建。
    /// 策划只需配置数值和起始卡组即可。
    /// </summary>
    [CreateAssetMenu(fileName = "Character_New", menuName = "JailerGame/Character Data", order = 2)]
    public class CharacterData : ScriptableObject
    {
        [Header("身份")]
        public string characterId;
        public string displayName;
        public CharacterClass characterClass;
        [TextArea(3, 8)] public string backstory;
        public Sprite portrait;

        [Header("基础数值")]
        public int maxHp = 80;
        public int baseSpeed = 8;
        public int moveRange = 3;
        public int maxEnergyPerTurn = 3;
        public int initialHandSize = 5;

        [Header("起始卡组")]
        public List<CardData> startingDeck = new();

        [Header("可选解锁卡（构筑池）")]
        public List<CardData> unlockableDeckPool = new();
    }
}
