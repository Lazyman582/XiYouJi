using System.Collections;
using UnityEngine;
using XiYouJi.Events;

/// <summary>
/// “某句讲完”后播放 Animator 动画的监听器（比如 NPC 做一个动作）：
/// 订阅 DialogueLineFinished → Animator.Play(状态名) → 等状态播完 →
/// 调用 DialogueUIController.Instance.ResumeDialogue() 回调，对话恢复。
///
/// 用法：
/// 1. 挂到任意物体上，listenEventKey 填对话数据里的 finishEventKey；
/// 2. 把目标 Animator（NPC/物体）拖进 animator，stateName 填 Animator Controller
///    里已有的状态名（该状态要设成非循环，否则用 FixedDuration 模式）；
/// 3. 对话数据那一句勾选 broadcastOnFinish，finishEventKey 填同一个 Key。
/// </summary>
public class AnimatorTaskListener : MonoBehaviour
{
    private enum WaitMode
    {
        WaitForStateEnd,   // 等状态播完（要求非循环状态）
        FixedDuration,     // 播固定秒数后恢复（循环状态用这个）
    }

    [Header("要响应的事件 Key（对应 DialogueData.finishEventKey，留空=全部响应）")]
    [SerializeField] private string listenEventKey;

    [Header("要播放动画的 Animator（留空则取本物体上的）")]
    [SerializeField] private Animator animator;

    [Header("要播放的状态名（须与 Animator Controller 内一致）")]
    [SerializeField] private string stateName = "Talk";
    [SerializeField] private int layer = 0;

    [Header("等待方式")]
    [SerializeField] private WaitMode waitMode = WaitMode.WaitForStateEnd;

    [Header("FixedDuration 模式的播放时长（秒）")]
    [SerializeField] private float fixedDuration = 2f;

    [Header("安全超时（秒），超时强制恢复对话")]
    [SerializeField] private float timeout = 15f;

    private Coroutine taskRoutine;

    private void OnEnable()
    {
        EventBus<DialogueLineFinished>.Subscribe(OnLineFinished);
    }

    private void OnDisable()
    {
        EventBus<DialogueLineFinished>.Unsubscribe(OnLineFinished);
        if (taskRoutine != null)
        {
            StopCoroutine(taskRoutine);
            taskRoutine = null;
        }
    }

    private void OnLineFinished(DialogueLineFinished evt)
    {
        if (!string.IsNullOrEmpty(listenEventKey) && evt.EventKey != listenEventKey)
            return;

        if (taskRoutine != null)
            StopCoroutine(taskRoutine);
        taskRoutine = StartCoroutine(RunTask());
    }

    private IEnumerator RunTask()
    {
        var anim = animator != null ? animator : GetComponent<Animator>();
        if (anim == null)
        {
            Debug.LogWarning("[AnimatorTaskListener] 没找到 Animator，直接恢复对话");
            Finish();
            yield break;
        }

        anim.enabled = true;
        anim.speed = 1f;
        anim.Play(stateName, layer, 0f);
        Debug.Log($"[AnimatorTaskListener] 收到讲完事件，播放状态：{stateName}");

        if (waitMode == WaitMode.FixedDuration)
        {
            yield return new WaitForSeconds(fixedDuration);
        }
        else
        {
            yield return null;   // 等 Play 生效，状态切过去
            float waited = 0f;
            while (waited < timeout)
            {
                var st = anim.GetCurrentAnimatorStateInfo(layer);
                if (st.IsName(stateName) && st.normalizedTime >= 1f && !anim.IsInTransition(layer))
                    break;
                waited += Time.deltaTime;
                yield return null;
            }
            if (waited >= timeout)
                Debug.LogWarning($"[AnimatorTaskListener] 等待状态 {stateName} 播完超时，强制恢复对话");
        }

        Finish();
    }

    private void Finish()
    {
        taskRoutine = null;
        // 动画播完 → 回调，恢复对话
        if (DialogueUIController.Instance != null)
            DialogueUIController.Instance.ResumeDialogue();
    }
}
