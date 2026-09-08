using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XiYouJi.Events;

// 监听对话 broadcastOnShow 的广播（EventBus<SceneTriggerEvent>），收到后设置指定 Animator 的 Bool 参数。
// 不用改对话系统代码：对话 asset 里勾选 broadcastOnShow 并填 broadcastEventKey，
// 本组件加一条条目、eventKey 填同一个 Key 即可。想控制别的控制器就再加一条条目。
public class BroadcastBoolSetter : MonoBehaviour
{
    [System.Serializable]
    public class BoolEntry
    {
        [Tooltip("要监听的广播Key，对应对话 asset 里填的 broadcastEventKey")]
        public string eventKey;

        [Tooltip("目标 Animator，留空则自动找本物体/子物体上的")]
        public Animator animator;

        [Tooltip("动画控制器里的 Bool 参数名（如 猪八戒丢篮子 控制器的 提篮）")]
        public string boolName;

        [Tooltip("收到广播时把 Bool 设为该值")]
        public bool value = true;

        [Tooltip("大于0时：N秒后自动把 Bool 设回相反值，防止 Bool 一直为 true 导致动画反复触发；0 = 不自动复位")]
        public float autoResetDelay = 0f;
    }

    [Tooltip("一条广播规则 = 一个条目，可配多条")]
    public List<BoolEntry> entries = new List<BoolEntry>();

    private void OnEnable()
    {
        EventBus<SceneTriggerEvent>.Subscribe(OnBroadcast);
    }

    private void OnDisable()
    {
        EventBus<SceneTriggerEvent>.Unsubscribe(OnBroadcast);
    }

    private void OnBroadcast(SceneTriggerEvent e)
    {
        foreach (BoolEntry entry in entries)
        {
            if (entry == null || string.IsNullOrEmpty(entry.eventKey)) continue;
            if ((e.EventKey ?? "").Trim() != entry.eventKey.Trim()) continue;

            Animator animator = entry.animator;
            if (animator == null) animator = GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogWarning($"【广播Bool】收到事件 {e.EventKey}，但条目里没有配置 Animator");
                continue;
            }

            animator.SetBool(entry.boolName, entry.value);
            Debug.Log($"【广播Bool】{e.EventKey} → {animator.name}.SetBool({entry.boolName}, {entry.value})");

            if (entry.autoResetDelay > 0f)
                StartCoroutine(ResetLater(entry, animator));
        }
    }

    private IEnumerator ResetLater(BoolEntry entry, Animator animator)
    {
        yield return new WaitForSeconds(entry.autoResetDelay);
        if (animator != null)
            animator.SetBool(entry.boolName, !entry.value);
    }
}
