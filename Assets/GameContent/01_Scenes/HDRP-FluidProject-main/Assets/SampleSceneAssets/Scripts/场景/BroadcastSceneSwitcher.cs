using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using XiYouJi.Events;

// 收到指定广播后执行过场：激活黑屏（挂 SimpleFade 的物体自动淡入）→ 延迟 switchDelay 秒（此时屏幕已全黑）
// → 播放 Timeline + 激活一个物体 + 失活一个物体。黑屏的淡出和自动失活由 SimpleFade 自己完成。
// 以后换一组过场，再挂一个本组件、填不同的 Key 即可。
public class BroadcastSceneSwitcher : MonoBehaviour
{
    [Tooltip("要监听的广播Key（如点击广播发出的 上路）")]
    public string listenKey;

    [Header("黑屏")]
    [Tooltip("黑屏物体（带 Image + SimpleFade）")]
    public GameObject blackScreen;

    [Tooltip("激活黑屏后等多久再切换（SimpleFade 淡入 0.5 秒，默认 0.6 保证已全黑）")]
    public float switchDelay = 0.6f;

    [Header("黑屏盖住屏幕时执行")]
    [Tooltip("要播放的 Timeline（laonai走路）")]
    public PlayableDirector director;

    [Tooltip("要激活的物体（场景三_孙悟空）")]
    public GameObject activateObject;

    [Tooltip("要失活的物体（场景二_孙悟空）")]
    public GameObject deactivateObject;

    private bool running;

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
        if (running || string.IsNullOrEmpty(listenKey)) return;
        if ((e.EventKey ?? "").Trim() != listenKey.Trim()) return;
        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        running = true;

        if (blackScreen != null)
            blackScreen.SetActive(true);   // SimpleFade：淡入0.5s → 停0.3s → 淡出0.5s → 自动失活

        yield return new WaitForSeconds(switchDelay);

        if (director != null)
        {
            director.time = 0;
            director.Play();
        }
        if (activateObject != null) activateObject.SetActive(true);
        if (deactivateObject != null) deactivateObject.SetActive(false);

        running = false;
    }
}
