using JailerGame.Core;
using UnityEngine;

namespace JailerGame.Characters
{
    /// <summary>
    /// 把 PlayerEntity（纯逻辑）和 GameObject 视觉绑定的 MonoBehaviour。
    /// 第一阶段只做最小绑定，后续可加动画、特效、UI。
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public CharacterData characterData;
        public PlayerEntity Entity { get; private set; }

        private void Start()
        {
            if (characterData == null)
            {
                Debug.LogError($"[PlayerController:{name}] 未设置 CharacterData，请在 Inspector 拖入");
                enabled = false;
                return;
            }
            Entity = new PlayerEntity(name, characterData);
            Debug.Log($"[Player] {Entity.DisplayName} 加入战斗：HP {Entity.CurrentHp}/{Entity.MaxHp}, 速度 {Entity.BaseSpeed}");
            Entity.OnDamaged += (e, d) => Debug.Log($"[Player] {e.DisplayName} 受到 {d} 伤害（剩 {e.CurrentHp}）");
            Entity.OnDied += e => Debug.Log($"[Player] {e.DisplayName} 已倒下");
        }
    }
}
