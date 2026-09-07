using UnityEngine;
using UnityEngine.Playables;
using XiYouJi.Events;

/// <summary>
/// “某句讲完”后播放 Timeline 的监听器：
/// 订阅 DialogueLineFinished → PlayableDirector.Play() → 播完（stopped 事件）→
/// 调用 DialogueUIController.Instance.ResumeDialogue() 回调，对话恢复。
///
/// 用法：
/// 1. 场景物体上挂 PlayableDirector，指定 TimelineAsset，取消勾选 Play On Awake；
/// 2. 同一物体（或任意物体）挂本组件，listenEventKey 填对话数据里的 finishEventKey，
///    并把 director 拖进来；
/// 3. 对话数据那一句勾选 broadcastOnFinish，finishEventKey 填同一个 Key。
/// </summary>
public class TimelineTaskListener : MonoBehaviour
{
    [Header("要响应的事件 Key（对应 DialogueData.finishEventKey，留空=全部响应）")]
    [SerializeField] private string listenEventKey;

    [Header("要播放的 PlayableDirector")]
    [SerializeField] private PlayableDirector director;

    [Header("播放期间隐藏对话框（适合过场演出，播完自动恢复）")]
    [SerializeField] private bool hideDialoguePanel = false;

    private bool registered;

    private void OnEnable()
    {
        EventBus<DialogueLineFinished>.Subscribe(OnLineFinished);
    }

    private void OnDisable()
    {
        EventBus<DialogueLineFinished>.Unsubscribe(OnLineFinished);
        UnregisterStopped();
    }

    private void OnLineFinished(DialogueLineFinished evt)
    {
        if (!string.IsNullOrEmpty(listenEventKey) && evt.EventKey != listenEventKey)
            return;
        if (director == null)
        {
            Debug.LogWarning("[TimelineTaskListener] 没有指定 PlayableDirector，直接恢复对话");
            Resume();
            return;
        }

        if (hideDialoguePanel && DialogueUIController.Instance != null)
            DialogueUIController.Instance.ClosePanel();

        // 从头播放；stopped 在播到结尾时触发
        director.time = 0;
        UnregisterStopped();
        director.stopped += OnDirectorStopped;
        registered = true;
        director.Play();
        Debug.Log($"[TimelineTaskListener] 收到讲完事件，播放 Timeline：{director.name}");
    }

    private void OnDirectorStopped(PlayableDirector dir)
    {
        UnregisterStopped();
        Resume();
    }

    private void Resume()
    {
        if (hideDialoguePanel && DialogueUIController.Instance != null)
            DialogueUIController.Instance.OpenPanel();

        // Timeline 播完 → 回调，恢复对话
        if (DialogueUIController.Instance != null)
            DialogueUIController.Instance.ResumeDialogue();
    }

    private void UnregisterStopped()
    {
        if (registered && director != null)
            director.stopped -= OnDirectorStopped;
        registered = false;
    }
}
