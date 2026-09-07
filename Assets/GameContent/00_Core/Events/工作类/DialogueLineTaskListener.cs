using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using XiYouJi.Events;

/// <summary>
/// “某句讲完”事件监听模板：
/// 订阅 DialogueLineFinished（按 EventKey 过滤）→ 执行自己的任务 → 任务完成后调用
/// DialogueUIController.Instance.ResumeDialogue() 作为回调，对话按钮恢复交互、流程继续。
///
/// 用法：
/// 1. 把本组件挂到场景中任意物体上；
/// 2. listenEventKey 填对话数据里配置的 finishEventKey（留空表示响应所有讲完事件）；
/// 3. 任务逻辑写在 RunTask() 里（默认是等待 taskDuration 秒），或直接在 Inspector
///    里给 onTaskStart / onTaskComplete 绑定要执行的操作。
/// </summary>
public class DialogueLineTaskListener : MonoBehaviour
{
    [Header("要响应的事件 Key（对应 DialogueData.finishEventKey，留空=全部响应）")]
    [SerializeField] private string listenEventKey;

    [Header("模拟任务耗时（秒），<=0 表示当帧完成")]
    [SerializeField] private float taskDuration = 2f;

    [Header("回调后是否自动推进下一句（当前句无选项时生效）")]
    [SerializeField] private bool autoContinue = false;

    [Header("任务开始 / 完成时触发（可选，Inspector 里绑定）")]
    [SerializeField] private UnityEvent onTaskStart;
    [SerializeField] private UnityEvent onTaskComplete;

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

        // 防止同一 Key 短时间内重复触发导致任务协程重叠
        if (taskRoutine != null)
            StopCoroutine(taskRoutine);
        taskRoutine = StartCoroutine(RunTask());
    }

    // 任务本体：在这里做真正的事（播动画、移动物体、等待某个条件……）
    private IEnumerator RunTask()
    {
        onTaskStart?.Invoke();
        Debug.Log($"[DialogueLineTaskListener] 收到讲完事件，任务开始（key={listenEventKey}，耗时 {taskDuration}s）");

        if (taskDuration > 0f)
            yield return new WaitForSeconds(taskDuration);

        onTaskComplete?.Invoke();
        taskRoutine = null;

        // 任务完成 → 回调，恢复对话
        if (DialogueUIController.Instance != null)
            DialogueUIController.Instance.ResumeDialogue(autoContinue);
    }
}
