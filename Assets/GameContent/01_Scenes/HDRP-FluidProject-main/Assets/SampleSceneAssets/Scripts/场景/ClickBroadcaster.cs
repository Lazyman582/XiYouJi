using UnityEngine;
using XiYouJi.Events;

// 点击物体 → 广播一个 SceneTriggerEvent（物体上需要有 Collider）。
// eventKey 填自定义 Key（如 "上路"），监听方（如 BroadcastSceneSwitcher）填同一个 Key。
// 也提供公开方法 BroadcastClick()，UI 按钮的 OnClick 可以直接绑它。
public class ClickBroadcaster : MonoBehaviour
{
    [Tooltip("点击后广播的事件Key")]
    public string eventKey;

    private void OnMouseDown()
    {
        BroadcastClick();
    }

    // 给 UI Button 的 OnClick 用（3D 物体不用手动调）
    public void BroadcastClick()
    {
        if (string.IsNullOrEmpty(eventKey))
        {
            Debug.LogWarning($"【点击广播】{gameObject.name} 没有填写 eventKey");
            return;
        }
        EventBus<SceneTriggerEvent>.Publish(new SceneTriggerEvent { EventKey = eventKey });
        Debug.Log($"【点击广播】{gameObject.name} → {eventKey}");
    }
}
