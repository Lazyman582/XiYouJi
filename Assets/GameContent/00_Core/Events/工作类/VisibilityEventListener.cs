using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XiYouJi.Events;

public class VisibilityEventListener : MonoBehaviour
{
    [Header("监听的事件 Key")]
    [SerializeField] private string showEventKey;   // 例如 "ShowStone"
    [SerializeField] private string hideEventKey;   // 例如 "HideStone"

    private void OnEnable()
    {
        EventBus<SceneTriggerEvent>.Subscribe(OnSceneTrigger);
    }

    private void OnDisable()
    {
        EventBus<SceneTriggerEvent>.Unsubscribe(OnSceneTrigger);
    }

    private void OnSceneTrigger(SceneTriggerEvent evt)
    {
        if (evt.EventKey == showEventKey)
        {
            gameObject.SetActive(true);
        }
        else if (evt.EventKey == hideEventKey)
        {
            gameObject.SetActive(false);
        }
    }
}
