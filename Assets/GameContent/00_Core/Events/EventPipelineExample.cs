using UnityEngine;

namespace XiYouJi.Events.Examples
{
    public sealed class PlayerDamaged : IGameEvent
    {
        public PlayerDamaged(int amount, string reason)
        {
            Amount = amount;
            Reason = reason;
        }

        public int Amount { get; private set; }
        public string Reason { get; private set; }
    }

    /// <summary>
    /// 可选的最小示例：挂到场景中的任意 GameObject 后，在 Inspector 组件菜单执行
    /// "Publish Demo Player Damaged"，即可看到三个阶段按 order 执行。
    /// </summary>
    public sealed class EventPipelineExample : MonoBehaviour
    {
        private IEventSubscription validationSubscription;
        private IEventSubscription uiSubscription;
        private IEventSubscription rewardSubscription;

        private void OnEnable()
        {
            validationSubscription = GameEvents.Global.Subscribe<PlayerDamaged>(
                ValidateDamage,
                order: -100,
                name: "ValidateDamage");

            uiSubscription = GameEvents.Global.Subscribe<PlayerDamaged>(
                ShowDamage,
                order: 0,
                name: "ShowDamage");

            rewardSubscription = GameEvents.Global.Subscribe<PlayerDamaged>(
                GiveRage,
                order: 100,
                name: "GiveRage");
        }

        private void OnDisable()
        {
            if (validationSubscription != null) validationSubscription.Dispose();
            if (uiSubscription != null) uiSubscription.Dispose();
            if (rewardSubscription != null) rewardSubscription.Dispose();
        }

        [ContextMenu("Publish Demo Player Damaged")]
        private void PublishDemoDamage()
        {
            EventExecutionReport report = GameEvents.Publish(
                new PlayerDamaged(25, "训练场木桩"),
                this);

            Debug.Log(
                string.Format(
                    "[EventPipeline] {0}: {1}/{2} stages, canceled={3}, elapsed={4}ms",
                    report.EventType.Name,
                    report.InvokedStageCount,
                    report.RegisteredStageCount,
                    report.IsCanceled,
                    report.ElapsedMilliseconds),
                this);
        }

        private static void ValidateDamage(EventContext<PlayerDamaged> context)
        {
            if (context.Event.Amount <= 0)
            {
                context.Cancel("伤害必须大于 0");
                return;
            }

            Debug.Log("[EventPipeline] ValidateDamage passed.");
        }

        private static void ShowDamage(EventContext<PlayerDamaged> context)
        {
            Debug.Log(string.Format(
                "[EventPipeline] UI: 玩家受到 {0} 点伤害，来源：{1}",
                context.Event.Amount,
                context.Event.Reason));
        }

        private static void GiveRage(EventContext<PlayerDamaged> context)
        {
            Debug.Log("[EventPipeline] Reward: 累积怒气。");
        }
    }
}
