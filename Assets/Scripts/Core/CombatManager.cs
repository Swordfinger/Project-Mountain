using System.Collections.Generic;
using JailerGame.Characters;
using JailerGame.Combat;
using JailerGame.Combat.HexGrid;
using JailerGame.Identity;
using UnityEngine;

namespace JailerGame.Core
{
    /// <summary>
    /// 战斗启动器。把场景中的玩家、敌人、地图绑定到 TurnManager，
    /// 并执行身份分配、初始位置部署、然后 StartCombat。
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        [Header("场景里的玩家与敌人")]
        public List<PlayerController> playerControllers = new();
        public List<EnemyEntityHolder> enemyHolders = new();

        [Header("是否分配使者身份（单角色测试时关闭）")]
        public bool assignEmissary = true;

        public IdentityManager Identity { get; private set; }

        private void Start()
        {
            Identity = new IdentityManager();

            var playerEntities = new List<PlayerEntity>();
            foreach (var pc in playerControllers)
            {
                if (pc.Entity == null) continue;
                playerEntities.Add(pc.Entity);
                TurnManager.Instance.RegisterPlayer(pc.Entity);
            }

            // 给每名玩家放到一个不冲突的起始格
            DeployPlayersToGrid(playerEntities);

            int playerCount = playerEntities.Count;
            foreach (var holder in enemyHolders)
            {
                if (holder.enemyData == null) continue;
                var entity = new EnemyEntity(holder.gameObject.name, holder.enemyData, playerCount);
                holder.RuntimeEntity = entity;
                DeployEntityToGrid(entity, holder.startCol, holder.startRow);
                TurnManager.Instance.RegisterEnemy(entity);
            }

            if (assignEmissary && playerEntities.Count >= 2)
                Identity.AssignIdentities(playerEntities);

            TurnManager.Instance.StartCombat();
        }

        private void DeployPlayersToGrid(List<PlayerEntity> players)
        {
            var map = HexGridManager.Instance?.Map;
            if (map == null) { Debug.LogError("[Combat] HexGridManager 未初始化"); return; }
            // 简单部署：玩家排在第 0 列
            int row = 0;
            foreach (var p in players)
            {
                var coord = HexCoordinates.FromOffsetCoordinates(0, row++ % map.Height);
                p.Position = coord;
                map.GetCell(coord).Occupant = p;
            }
        }

        private void DeployEntityToGrid(CombatEntity entity, int col, int row)
        {
            var map = HexGridManager.Instance?.Map;
            if (map == null) return;
            var coord = HexCoordinates.FromOffsetCoordinates(col, row);
            if (!map.Contains(coord))
                coord = HexCoordinates.FromOffsetCoordinates(map.Width - 1, map.Height / 2);
            entity.Position = coord;
            map.GetCell(coord).Occupant = entity;
        }
    }

    /// <summary>编辑器里挂在敌人 GameObject 上，绑定 EnemyData 与起始坐标</summary>
    [System.Serializable]
    public class EnemyEntityHolder
    {
        public GameObject gameObject;
        public EnemyData enemyData;
        public int startCol = 7;
        public int startRow = 4;
        [System.NonSerialized] public EnemyEntity RuntimeEntity;
    }
}
