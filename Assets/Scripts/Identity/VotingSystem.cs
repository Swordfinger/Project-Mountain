using System.Collections.Generic;
using JailerGame.Characters;
using UnityEngine;

namespace JailerGame.Identity
{
    /// <summary>
    /// 投票系统：每层结束或每 10 分钟发起一次。
    /// 规则：
    /// - 每人 1 票，投票对象必须是仍存活的玩家，可弃票
    /// - 票数最高的玩家被驱逐
    /// - 平票视为弃票（不驱逐）
    /// - 如果驱逐对象是使者：使者直接失败，告密者继续闯关
    /// - 如果驱逐对象不是使者：所有投他票的玩家受惩罚（按人数平均分担）
    /// - 使者可以使用 FakeVote 让自己的票看起来像是别人投的
    /// </summary>
    public class VotingSystem
    {
        public class VoteResult
        {
            public PlayerEntity Ejected;       // 被驱逐者（null = 弃票/平票）
            public bool IsEmissary;            // 被驱逐者是不是使者
            public List<PlayerEntity> Penalized = new(); // 投错票需受惩罚的玩家
        }

        private readonly Dictionary<string, string> _votes = new(); // voterId -> targetId（"" = 弃票）

        public void CastVote(PlayerEntity voter, PlayerEntity target)
        {
            _votes[voter.Id] = target?.Id ?? "";
        }

        public VoteResult Tally(IList<PlayerEntity> players)
        {
            var result = new VoteResult();
            var counts = new Dictionary<string, int>();
            foreach (var kv in _votes)
            {
                if (string.IsNullOrEmpty(kv.Value)) continue;
                counts.TryGetValue(kv.Value, out var c);
                counts[kv.Value] = c + 1;
            }

            if (counts.Count == 0) return result; // 全弃票

            // 找最高票
            int max = -1;
            string winnerId = null;
            int tieCount = 0;
            foreach (var kv in counts)
            {
                if (kv.Value > max) { max = kv.Value; winnerId = kv.Key; tieCount = 1; }
                else if (kv.Value == max) tieCount++;
            }
            if (tieCount > 1) return result; // 平票，无人被驱逐

            foreach (var p in players)
            {
                if (p.Id == winnerId)
                {
                    result.Ejected = p;
                    result.IsEmissary = p.Identity == PlayerIdentity.Emissary;
                    break;
                }
            }

            // 错投的玩家受惩罚
            if (result.Ejected != null && !result.IsEmissary)
            {
                foreach (var kv in _votes)
                {
                    if (kv.Value == winnerId)
                    {
                        var voter = players.FindIndex(p => p.Id == kv.Key);
                        if (voter >= 0) result.Penalized.Add(players[voter]);
                    }
                }
            }

            return result;
        }

        public void Clear() => _votes.Clear();
    }

    static class ListExt
    {
        public static int FindIndex<T>(this IList<T> list, System.Predicate<T> match)
        {
            for (int i = 0; i < list.Count; i++) if (match(list[i])) return i;
            return -1;
        }
    }
}
