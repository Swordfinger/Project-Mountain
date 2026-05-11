using System.Collections;
using System.Collections.Generic;
using JailerGame.Cards;
using JailerGame.Characters;
using JailerGame.Combat;
using JailerGame.Combat.HexGrid;
using JailerGame.Core;
using UnityEngine;

namespace JailerGame.BossArena
{
    /// <summary>
    /// Boss 1v1 战斗管理器。
    ///
    /// 流程：
    /// 1. 战斗开始时，按 Boss 当前位置生成螺旋编号
    /// 2. 每个"大回合"开始：
    ///    a. 扫描所有存活玩家，按编号从小到大排序得到出场顺序
    ///    b. 依次让 Boss 和每位玩家进行 1v1 子回合（其他玩家观战/准备卡牌）
    ///    c. 每轮 1v1 后检查 Boss 是否到撤退阈值（80%/60%/40%/20%）
    ///       —— 如果到了，Boss 后退 1 格，重建编号
    /// 3. 直到 Boss 死或所有玩家死
    ///
    /// 测试 Boss 行为（可配置）：
    ///   - 攻击 → 攻击 → 回血，循环
    /// </summary>
    public class BossArenaManager : MonoBehaviour
    {
        public static BossArenaManager Instance { get; private set; }

        [Header("场景里的玩家与 Boss")]
        public List<PlayerController> playerControllers = new();
        public BossController boss;

        [Header("螺旋编号最大圈数")]
        [Range(2, 10)] public int maxRing = 6;

        [Header("Boss 撤退阈值（HP 百分比，从大到小）")]
        public float[] retreatThresholds = { 0.8f, 0.6f, 0.4f, 0.2f };

        [Header("Boss 撤退方向（朝门 / 后退方向，HexCoordinates.Directions 索引 0~5）")]
        [Range(0, 5)] public int retreatDirectionIndex = 3; // 默认 West（向左退）

        [Header("每个 1v1 子回合的最大持续回合数（防止无限循环）")]
        public int maxRoundsPerDuel = 20;

        public SpiralHexNumbering Numbering { get; private set; }
        private readonly HashSet<int> _firedThresholds = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (boss == null || boss.Entity == null)
            {
                Debug.LogError("[BossArena] 未指定 BossController 或 BossEntity 未初始化");
                return;
            }

            Numbering = new SpiralHexNumbering(boss.Entity.Position, maxRing);
            LogNumbering();
            StartCoroutine(BattleLoop());
        }

        private IEnumerator BattleLoop()
        {
            while (true)
            {
                if (!boss.Entity.IsAlive)
                {
                    Debug.Log("[BossArena] === Boss 已死，玩家胜利 ===");
                    yield break;
                }
                if (!HasAnyAlivePlayer())
                {
                    Debug.Log("[BossArena] === 所有玩家阵亡，Boss 胜利 ===");
                    yield break;
                }

                // 1. 按编号排序得到出场顺序
                var queue = BuildEngagementQueue();
                if (queue.Count == 0)
                {
                    Debug.Log("[BossArena] 编号范围内无玩家，扩大圈数或玩家应该靠近 Boss");
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                Debug.Log($"[BossArena] >>> 新一轮交战开始，出场顺序：" +
                          string.Join(" → ", queue.ConvertAll(t => $"#{t.number}{t.player.DisplayName}")));

                // 2. 依次进行 1v1
                foreach (var entry in queue)
                {
                    if (!boss.Entity.IsAlive) break;
                    if (!entry.player.IsAlive) continue;

                    yield return RunDuel(entry.player, entry.number);

                    // 3. 1v1 结束后检查撤退
                    if (boss.Entity.IsAlive && CheckRetreatThreshold())
                    {
                        TryBossRetreat();
                        // 重建后这轮其他玩家先不打了，下个大回合重新计算编号
                        break;
                    }
                }

                yield return null;
            }
        }

        // ============ 编号 / 出场顺序 ============

        private struct DuelEntry { public int number; public PlayerEntity player; }

        private List<DuelEntry> BuildEngagementQueue()
        {
            var list = new List<DuelEntry>();
            foreach (var pc in playerControllers)
            {
                if (pc?.Entity == null || !pc.Entity.IsAlive) continue;
                int n = Numbering.GetNumber(pc.Entity.Position);
                if (n <= 0) continue; // 0 号是 Boss 自己；-1 表示不在编号范围
                list.Add(new DuelEntry { number = n, player = pc.Entity });
            }
            list.Sort((a, b) => a.number.CompareTo(b.number));
            return list;
        }

        // ============ 1v1 子回合 ============

        private IEnumerator RunDuel(PlayerEntity player, int slotNumber)
        {
            Debug.Log($"[BossArena] —— 1v1 开始：Boss vs {player.DisplayName}（编号 #{slotNumber}）——");

            int round = 1;
            while (round <= maxRoundsPerDuel && boss.Entity.IsAlive && player.IsAlive)
            {
                Debug.Log($"[BossArena] [{player.DisplayName}] Round {round}");

                // 谁先动：基础速度高的先动；速度相同则玩家先
                bool playerFirst = player.BaseSpeed >= boss.Entity.BaseSpeed;

                if (playerFirst) yield return PlayerTurn(player);
                else yield return BossTurn(player);

                if (!boss.Entity.IsAlive || !player.IsAlive) break;

                if (playerFirst) yield return BossTurn(player);
                else yield return PlayerTurn(player);

                round++;
            }

            Debug.Log($"[BossArena] —— 1v1 结束 —— Boss HP: {boss.Entity.CurrentHp}/{boss.Entity.MaxHp}, " +
                      $"{player.DisplayName} HP: {player.CurrentHp}/{player.MaxHp}");
        }

        private IEnumerator PlayerTurn(PlayerEntity player)
        {
            player.StartTurn();
            player.RefillEnergy();
            player.Deck.TickCooldowns();
            player.Deck.Draw(2);

            // 第一阶段没有 UI，玩家由 BossArenaPlayerAI（或人类操作）打牌
            // 这里给个简单 AI：按手牌顺序尝试出 1 张可出的牌
            var aiCard = ChooseSimpleCard(player);
            if (aiCard != null)
            {
                player.TrySpendEnergy(aiCard.Data.energyCost);
                var ctx = new CardCastContext
                {
                    Caster = player,
                    AttackDirection = 0,
                    CasterClass = player.Class.ToBreakPointType(),
                };
                ctx.Targets.Add(boss.Entity);
                foreach (var fx in aiCard.Data.effects) fx.Execute(ctx);
                Debug.Log($"  → {player.DisplayName} 打出 [{aiCard.Data.cardName}]");
                player.Deck.Discard(aiCard);
            }
            else
            {
                Debug.Log($"  → {player.DisplayName} 无法出牌，跳过");
            }

            player.EndTurn();
            yield return new WaitForSeconds(0.3f);
        }

        private Card ChooseSimpleCard(PlayerEntity player)
        {
            foreach (var c in player.Deck.Hand)
                if (c.CanPlay(player.Energy)) return c;
            return null;
        }

        private IEnumerator BossTurn(PlayerEntity opponent)
        {
            boss.Entity.StartTurn();
            var hits = boss.Entity.ExecuteAgainst(opponent);
            boss.Entity.EndTurn();
            yield return new WaitForSeconds(0.3f);
        }

        // ============ Boss 撤退 ============

        private bool CheckRetreatThreshold()
        {
            float pct = (float)boss.Entity.CurrentHp / boss.Entity.MaxHp;
            for (int i = 0; i < retreatThresholds.Length; i++)
            {
                if (pct <= retreatThresholds[i] && !_firedThresholds.Contains(i))
                {
                    _firedThresholds.Add(i);
                    Debug.Log($"[BossArena] Boss 血量降至 {pct * 100:0}%，触发第 {i + 1} 次撤退");
                    return true;
                }
            }
            return false;
        }

        private void TryBossRetreat()
        {
            var grid = HexGridManager.Instance?.Map;
            if (grid == null) return;

            var newPos = boss.Entity.Position.Neighbor(retreatDirectionIndex);
            var cell = grid.GetCell(newPos);
            if (cell == null || cell.Occupant != null)
            {
                Debug.Log("[BossArena] 撤退方向被阻挡，Boss 留在原地");
                return;
            }

            var oldCell = grid.GetCell(boss.Entity.Position);
            if (oldCell != null) oldCell.Occupant = null;
            boss.Entity.Position = newPos;
            cell.Occupant = boss.Entity;

            Numbering.Rebuild(newPos, maxRing);
            Debug.Log($"[BossArena] Boss 撤退至 {newPos}，重建编号");
            LogNumbering();
        }

        // ============ 工具 ============

        private bool HasAnyAlivePlayer()
        {
            foreach (var pc in playerControllers)
                if (pc?.Entity != null && pc.Entity.IsAlive) return true;
            return false;
        }

        private void LogNumbering()
        {
            int shown = 0;
            var sb = new System.Text.StringBuilder("[BossArena] 编号映射：");
            foreach (var kv in Numbering.EnumerateAll())
            {
                sb.Append($" #{kv.Key}={kv.Value}");
                if (++shown >= 19) { sb.Append(" ..."); break; }
            }
            Debug.Log(sb.ToString());
        }
    }
}
