// =====================================================================
// 联机占位文件
// 第一阶段不依赖 Mirror / Facepunch.Steamworks，避免编译报错。
// 第五阶段将这些用 #if MIRROR 包起来并替换成真实实现。
// =====================================================================

using System;
using UnityEngine;

namespace JailerGame.Networking
{
    /// <summary>
    /// 网络命令接口：所有"会改变状态"的操作都通过它走，
    /// 联机时由权威端验证并广播执行。这就是反作弊的基石。
    /// </summary>
    public interface ICommand
    {
        bool Validate(GameStateSnapshot state);
        void Execute(GameStateSnapshot state);
        byte[] Serialize();
    }

    /// <summary>
    /// 战斗状态快照（联机同步用，第一阶段只是占位）。
    /// </summary>
    [Serializable]
    public class GameStateSnapshot
    {
        public int turnNumber;
        public string[] alivePlayerIds;
        public string[] aliveEnemyIds;
    }

    /// <summary>
    /// Steam Lobby 占位。第五阶段用 Facepunch.Steamworks 接 Steam Matchmaking。
    /// </summary>
    public class SteamLobbyPlaceholder : MonoBehaviour
    {
        public void CreateLobby(int maxPlayers = 6)
        {
            Debug.Log($"[Net] (占位) 创建 {maxPlayers} 人 Lobby —— 第五阶段会接 Steam");
        }
        public void JoinLobby(ulong lobbyId)
        {
            Debug.Log($"[Net] (占位) 加入 Lobby {lobbyId} —— 第五阶段实现");
        }
    }

    /// <summary>
    /// 反作弊验证器占位：因为你们租服务器，权威逻辑跑在服务端，
    /// 客户端发来的命令必须在这里通过校验才能执行。
    /// </summary>
    public static class AntiCheatValidator
    {
        public static bool ValidateCardPlay(GameStateSnapshot state, string playerId, string cardId)
        {
            // 第五阶段：检查能量、冷却、手牌确实有这张卡、目标合法等
            return true;
        }
    }
}
