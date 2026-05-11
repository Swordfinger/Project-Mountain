using System.Collections.Generic;
using UnityEngine;

namespace JailerGame.Cards
{
    /// <summary>
    /// 全局卡牌数据库：在 Resources 里加载所有 CardData。
    /// 用于：联机时按 cardId 同步、商店随机出卡、关卡掉落等。
    /// </summary>
    [CreateAssetMenu(fileName = "CardDatabase", menuName = "JailerGame/Card Database")]
    public class CardDatabase : ScriptableObject
    {
        public List<CardData> allCards = new();

        private Dictionary<string, CardData> _lookup;

        public CardData GetById(string id)
        {
            if (_lookup == null) Build();
            return _lookup.TryGetValue(id, out var c) ? c : null;
        }

        private void Build()
        {
            _lookup = new Dictionary<string, CardData>();
            foreach (var c in allCards)
            {
                if (c == null || string.IsNullOrEmpty(c.cardId)) continue;
                if (_lookup.ContainsKey(c.cardId))
                    Debug.LogWarning($"[CardDatabase] 重复 cardId：{c.cardId}");
                else _lookup[c.cardId] = c;
            }
        }
    }
}
