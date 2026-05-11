using UnityEngine;

namespace JailerGame.Core
{
    /// <summary>
    /// 全局游戏总控（场景间持久化）。
    /// 第一阶段只放空壳，第三阶段补 4 层世界、入口选择、晋级判定等。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("当前所在层数（1~4）")]
        public int currentLayer = 1;

        [Header("当前对局玩家数（5 或 6）")]
        public int playerCount = 5;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
