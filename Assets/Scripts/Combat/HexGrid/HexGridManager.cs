using UnityEngine;

namespace JailerGame.Combat.HexGrid
{
    /// <summary>
    /// MonoBehaviour 包装：负责生成 HexGridMap，并暴露给场景里其它系统使用。
    /// 第一阶段不做渲染（不画格子），只把数据准备好。
    /// 第二阶段把 Hex Game Studio 的视觉资源塞进 OnCellCreated 即可。
    /// </summary>
    public class HexGridManager : MonoBehaviour
    {
        [Header("地图尺寸")]
        [Range(4, 12)] public int gridWidth = 8;
        [Range(4, 12)] public int gridHeight = 8;
        public float cellSize = 1.0f;

        [Header("调试可视化")]
        public bool drawGizmos = true;
        public Color gizmoColor = new Color(0.4f, 0.8f, 1f, 0.4f);

        public HexGridMap Map { get; private set; }

        public static HexGridManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildMap();
        }

        public void BuildMap()
        {
            Map = new HexGridMap(gridWidth, gridHeight, cellSize);
            Debug.Log($"[HexGridManager] 蜂窝格地图生成完成：{gridWidth}x{gridHeight}，共 {Map.Cells.Count} 格");
        }

        // —— Gizmos：在 Scene 视图中画蜂窝格轮廓，方便看 ——
        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;
            var map = Map ?? new HexGridMap(gridWidth, gridHeight, cellSize);
            Gizmos.color = gizmoColor;
            foreach (var kv in map.Cells)
            {
                var center = kv.Key.ToWorld(cellSize) + transform.position;
                DrawHexGizmo(center, cellSize);
            }
        }

        private static void DrawHexGizmo(Vector3 center, float size)
        {
            Vector3 prev = Vector3.zero;
            for (int i = 0; i <= 6; i++)
            {
                float angle = Mathf.Deg2Rad * (60f * i);
                var p = center + new Vector3(size * Mathf.Cos(angle), 0, size * Mathf.Sin(angle));
                if (i > 0) Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }
    }
}
