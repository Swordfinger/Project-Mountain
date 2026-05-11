using System.Collections.Generic;
using JailerGame.Cards;
using JailerGame.Characters;
using JailerGame.Combat.HexGrid;
using UnityEngine;

namespace JailerGame.BossArena
{
    /// <summary>
    /// BossArena 一键启动器。
    ///
    /// 用法（最少配置）：
    /// 1. 新建空场景
    /// 2. 创建空 GameObject 命名 "HexGridManager"，挂 HexGridManager 组件（width=8, height=8, cellSize=1）
    /// 3. 创建空 GameObject 命名 "Boss"，挂 BossController 组件（startCol=4, startRow=4）
    /// 4. 创建空 GameObject 命名 "Player1"、"Player2" ... 各挂 PlayerController + 指定 CharacterData
    /// 5. 创建空 GameObject 命名 "BossArenaSceneStarter"，挂本组件，把上面的引用拖进去
    /// 6. 按 Play —— Console 会输出完整 1v1 战斗日志
    ///
    /// 也可以直接挂 BossArenaManager，然后用 Inspector 手动配 boss 与 playerControllers，
    /// 本脚本只是为了在场景中自动找齐组件并设到 BossArenaManager 上。
    /// </summary>
    public class BossArenaSceneStarter : MonoBehaviour
    {
        [Header("如果留空，会从场景中自动 FindObjectsOfType")]
        public BossController boss;
        public List<PlayerController> playerControllers = new();

        [Header("玩家初始格（与 playerControllers 对齐；超出则用 (0,0+i)）")]
        public List<Vector2Int> playerStartOffsets = new();

        [Header("自动创建 BossArenaManager（如果场景没有）")]
        public bool autoCreateManager = true;

        private void Awake()
        {
            // 自动找 Boss
            if (boss == null) boss = FindObjectOfType<BossController>();

            // 自动找玩家
            if (playerControllers.Count == 0)
                playerControllers.AddRange(FindObjectsOfType<PlayerController>());

            if (boss == null)
            {
                Debug.LogError("[BossArenaSceneStarter] 场景中没有找到 BossController");
                return;
            }
            if (playerControllers.Count == 0)
            {
                Debug.LogError("[BossArenaSceneStarter] 场景中没有找到 PlayerController");
                return;
            }

            // 部署玩家位置（在 PlayerController.Start 跑之前）
            for (int i = 0; i < playerControllers.Count; i++)
            {
                var pc = playerControllers[i];
                if (pc == null) continue;
                Vector2Int off = (i < playerStartOffsets.Count)
                    ? playerStartOffsets[i]
                    : new Vector2Int(0, i);
                // PlayerController 自身有 startCol/startRow（如果有）；这里强制赋值
                var col = pc.GetType().GetField("startCol");
                var row = pc.GetType().GetField("startRow");
                if (col != null) col.SetValue(pc, off.x);
                if (row != null) row.SetValue(pc, off.y);
            }
        }

        private void Start()
        {
            var mgr = FindObjectOfType<BossArenaManager>();
            if (mgr == null && autoCreateManager)
            {
                var go = new GameObject("BossArenaManager(Auto)");
                mgr = go.AddComponent<BossArenaManager>();
            }
            if (mgr == null) return;

            mgr.boss = boss;
            mgr.playerControllers = new List<PlayerController>(playerControllers);
            Debug.Log($"[BossArenaSceneStarter] 已注入 Boss={boss.name}，玩家 {playerControllers.Count} 名。BossArenaManager 即将启动战斗循环。");
        }
    }
}
