using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XiYouJi.Events;

public class VisibilityEventListener : MonoBehaviour
{
    [Header("监听的事件 Key")]
    [SerializeField] private string showEventKey;   // 例如 "ShowStone"
    [SerializeField] private string hideEventKey;   // 例如 "HideStone"

    [SerializeField] private GameObject[] targetObjects;
    [Header("阻塞对话配置")]
    [Tooltip("是否在触发事件时阻塞对话，等待完成后继续")]
    [SerializeField] private bool blockDialogue = true;

    [Tooltip("阻塞后自动恢复的时间（秒），<=0 表示手动恢复")]
    [SerializeField] private float autoUnblockDelay = 0f;
    private void OnEnable()
    {
        EventBus<SceneTriggerEvent>.Subscribe(OnSceneTrigger);
    }

    private void OnDisable()
    {
        EventBus<SceneTriggerEvent>.Unsubscribe(OnSceneTrigger);
    }

    private IEnumerator AutoUnblockAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        UnblockDialogue();
    }

    public void UnblockDialogue()
    {
        if (!blockDialogue) return;

        DialogueController.Instance?.UnblockProceed();
        DialogueUIController.Instance?.UnblockProceed();
    }
    private void OnSceneTrigger(SceneTriggerEvent evt)
    {
        if (evt.EventKey == showEventKey)
        {
            // 如果要阻塞对话，先锁住
            if (blockDialogue)
            {
                DialogueController.Instance?.BlockProceed();
                DialogueUIController.Instance?.BlockProceed();
            }

            foreach (var obj in targetObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                    Debug.Log($"显示物体：{obj.name}");
                }

                // 如果设置了自动恢复时间
                if (blockDialogue && autoUnblockDelay > 0)
                {
                    StartCoroutine(AutoUnblockAfter(autoUnblockDelay));
                }
                // 如果 autoUnblockDelay <= 0，则需要手动恢复（比如动画结束回调）
            }
        }
        else if (evt.EventKey == hideEventKey)
        {
            foreach (var obj in targetObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                    Debug.Log($"隐藏物体：{obj.name}");
                }
            }
        }
    }


    }


