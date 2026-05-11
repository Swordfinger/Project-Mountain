using JailerGame.Core;
using UnityEngine;

namespace JailerGame.UI
{
    /// <summary>
    /// UI 占位：监听 TurnManager 的事件并在控制台打印。
    /// 第二阶段你们自己写真正的 UI（uGUI/UGUI 或 UI Toolkit），
    /// 通过同样的事件订阅就能无缝替换。
    /// </summary>
    public class UIPlaceholder : MonoBehaviour
    {
        private void Start()
        {
            if (TurnManager.Instance == null) return;
            TurnManager.Instance.OnPhaseChanged += p => Debug.Log($"[UI] 阶段切换 → {p}");
            TurnManager.Instance.OnTurnStarted += t => Debug.Log($"[UI] 回合 {t} 开始");
            TurnManager.Instance.OnTurnEnded += t => Debug.Log($"[UI] 回合 {t} 结束");
        }
    }
}
