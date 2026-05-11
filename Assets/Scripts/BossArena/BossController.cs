using JailerGame.Combat.HexGrid;
using UnityEngine;

namespace JailerGame.BossArena
{
    /// <summary>
    /// 把 BossEntity（纯逻辑）和场景中的 GameObject 绑定的 MonoBehaviour。
    /// </summary>
    public class BossController : MonoBehaviour
    {
        [Header("Boss 数值")]
        public string bossName = "测试守门人";
        public int maxHp = 200;
        public int baseSpeed = 5;
        public int attackDamage = 10;
        public int healAmount = 10;

        [Header("初始位置（蜂窝格 offset 坐标）")]
        public int startCol = 4;
        public int startRow = 4;

        public BossEntity Entity { get; private set; }

        private void Awake()
        {
            Entity = new BossEntity(name, bossName, maxHp, baseSpeed)
            {
                AttackDamage = attackDamage,
                HealAmount = healAmount,
            };

            Entity.OnDamaged += (e, d) => Debug.Log($"[Boss] 受到 {d} 伤害（剩 {e.CurrentHp}/{e.MaxHp}）");
            Entity.OnDied += e => Debug.Log("[Boss] === 倒下 ===");
        }

        private void Start()
        {
            // 部署到地图
            var map = HexGridManager.Instance?.Map;
            if (map == null) return;
            var coord = HexCoordinates.FromOffsetCoordinates(startCol, startRow);
            if (!map.Contains(coord))
            {
                Debug.LogError($"[Boss] 起始格 ({startCol},{startRow}) 不在地图内");
                return;
            }
            Entity.Position = coord;
            map.GetCell(coord).Occupant = Entity;
            Debug.Log($"[Boss] {bossName} 部署到 {coord}, HP {Entity.CurrentHp}/{Entity.MaxHp}");
        }
    }
}
