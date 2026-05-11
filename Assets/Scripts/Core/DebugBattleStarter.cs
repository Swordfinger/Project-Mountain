using JailerGame.Cards;
using JailerGame.Characters;
using JailerGame.Combat.BreakPoint;
using UnityEngine;

namespace JailerGame.Core
{
    /// <summary>
    /// Debug 用：键盘 1~5 让玩家提交手牌前 5 张里的某一张到 PendingCards。
    /// 等你们做完 UI 后可以删掉这个文件。
    /// </summary>
    public class DebugBattleStarter : MonoBehaviour
    {
        public PlayerController testPlayer;

        private void Update()
        {
            if (testPlayer == null || testPlayer.Entity == null) return;
            if (TurnManager.Instance == null || TurnManager.Instance.Phase != TurnPhase.PlanPhase) return;

            for (int i = 0; i < 5; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    TrySubmitHandCard(i);
            }
        }

        private void TrySubmitHandCard(int handIndex)
        {
            var p = testPlayer.Entity;
            if (handIndex >= p.Deck.Hand.Count) return;
            var card = p.Deck.Hand[handIndex];
            if (!card.CanPlay(p.Energy)) return;

            // 简单测试：目标固定为第一个敌人，攻击方向固定为 0
            var ctx = new CardCastContext
            {
                AttackDirection = 0,
                CasterClass = p.Class.ToBreakPointType(),
            };
            if (TurnManager.Instance.Enemies.Count > 0 && TurnManager.Instance.Enemies[0].IsAlive)
                ctx.Targets.Add(TurnManager.Instance.Enemies[0]);

            TurnManager.Instance.SubmitCard(p, card, ctx);
            Debug.Log($"[Debug] 提交了 [{card.Data.cardName}]（手牌位 {handIndex + 1}）");
        }
    }
}
