using System.Collections.Generic;
using JailerGame.Characters;
using UnityEngine;

namespace JailerGame.Identity
{
    /// <summary>
    /// 身份分配与查询管理器。
    /// 单机阶段：直接随机选一个玩家做使者。
    /// 联机阶段：由 Host/服务器分配，并通过 TargetRpc 只通知"自己是不是使者"给对应玩家。
    ///
    /// 注意：使者的"思路D"能力分三层：
    ///   1) 隐藏被动加成（永远生效，不会暴露）
    ///   2) 主动技能按钮（一旦使用则可能暴露身份）
    ///   3) 剧情节点互动（如释放被封印的使者副本）
    /// </summary>
    public class IdentityManager
    {
        private readonly Dictionary<string, PlayerIdentity> _assignments = new();

        /// <summary>从玩家列表中随机指派 1 名使者，其余为告密者</summary>
        public void AssignIdentities(IList<PlayerEntity> players, int seed = 0)
        {
            if (players == null || players.Count == 0) return;
            var rng = seed == 0 ? new System.Random() : new System.Random(seed);
            int emissaryIndex = rng.Next(players.Count);
            for (int i = 0; i < players.Count; i++)
            {
                var id = i == emissaryIndex ? PlayerIdentity.Emissary : PlayerIdentity.Informer;
                players[i].Identity = id;
                _assignments[players[i].Id] = id;
            }
            Debug.Log($"[Identity] 身份分配完成，使者是 {players[emissaryIndex].DisplayName}（仅服务端可见）");
        }

        public PlayerIdentity Get(string playerId) =>
            _assignments.TryGetValue(playerId, out var id) ? id : PlayerIdentity.Informer;

        public PlayerEntity FindEmissary(IList<PlayerEntity> players)
        {
            foreach (var p in players)
                if (p.Identity == PlayerIdentity.Emissary) return p;
            return null;
        }
    }
}
