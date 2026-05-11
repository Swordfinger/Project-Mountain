using System;
using System.Collections.Generic;
using UnityEngine;

namespace JailerGame.Cards
{
    /// <summary>
    /// 玩家在战斗中的牌库 + 手牌 + 弃牌堆 + 消耗堆。
    /// 完全照着杀戮尖塔模型，新人易理解。
    /// </summary>
    public class Deck
    {
        public List<Card> DrawPile { get; } = new();
        public List<Card> Hand { get; } = new();
        public List<Card> DiscardPile { get; } = new();
        public List<Card> ExhaustPile { get; } = new();

        public int HandLimit { get; set; } = 10;

        public event Action OnDeckChanged;

        private readonly System.Random _rng;

        public Deck(IEnumerable<CardData> startingCards, int seed = 0)
        {
            _rng = seed == 0 ? new System.Random() : new System.Random(seed);
            foreach (var data in startingCards)
                DrawPile.Add(new Card(data));
            Shuffle(DrawPile);
        }

        public void Draw(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (Hand.Count >= HandLimit)
                {
                    Debug.Log("[Deck] 手牌已满，跳过抽牌");
                    break;
                }
                if (DrawPile.Count == 0) ReshuffleDiscardIntoDraw();
                if (DrawPile.Count == 0) break;
                var card = DrawPile[^1];
                DrawPile.RemoveAt(DrawPile.Count - 1);
                Hand.Add(card);
            }
            OnDeckChanged?.Invoke();
        }

        public void Discard(Card c)
        {
            Hand.Remove(c);
            if (c.Data.exhaustAfterUse) ExhaustPile.Add(c);
            else DiscardPile.Add(c);
            OnDeckChanged?.Invoke();
        }

        public void ReshuffleDiscardIntoDraw()
        {
            DrawPile.AddRange(DiscardPile);
            DiscardPile.Clear();
            Shuffle(DrawPile);
        }

        public void Shuffle(List<Card> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        public void TickCooldowns()
        {
            foreach (var c in Hand) if (c.CurrentCooldown > 0) c.CurrentCooldown--;
            foreach (var c in DrawPile) if (c.CurrentCooldown > 0) c.CurrentCooldown--;
            foreach (var c in DiscardPile) if (c.CurrentCooldown > 0) c.CurrentCooldown--;
        }
    }
}
