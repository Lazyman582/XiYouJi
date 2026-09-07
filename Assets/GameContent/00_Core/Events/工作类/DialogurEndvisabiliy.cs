using DiscoDialoguePrototype;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XiYouJi.Events;

public class DialogurEndvisabiliy : MonoBehaviour
{
    [Header("要控制的物体")]
    [SerializeField] private GameObject targetObject;

    [Header("结束时执行的动作")]
    [SerializeField] private bool setActive = true;   // true=显示, false=隐藏

    [Header("过滤指定对话容器（可选）")]
    [SerializeField] private DialogueDataContainer targetContainer;

    [Header("延迟控制")]
    [SerializeField] private bool useDelay = false;        // 是否启用延迟
    [SerializeField] private float delayTime = 2f;         // 延迟秒数

    // ---------- 生命周期：订阅/取消订阅 ----------
    private void OnEnable()
    {
        EventBus<DialogueChoiceSelected>.Subscribe(OnDialogueChoice, 0);
    }

    private void OnDisable()
    {
        EventBus<DialogueChoiceSelected>.Unsubscribe(OnDialogueChoice);
    }

    // ---------- 事件处理 ----------
    private void OnDialogueChoice(DialogueChoiceSelected evt)
    {
        // 1. 必须是由“结束选项”触发的事件
        if (evt.TargetIndex != -1) return;

        // 2. 容器过滤
        if (targetContainer != null)
        {
            var currentContainer = DialogueController.Instance?.currentContainer;
            if (currentContainer != targetContainer) return;
        }

        // 3. 执行逻辑（根据 useDelay 决定是否延迟）
        if (targetObject != null)
        {
            if (useDelay && delayTime > 0f)
            {
                StartCoroutine(DelayedExecute());
            }
            else
            {
                ExecuteImmediate();
            }
        }
    }

    private void ExecuteImmediate()
    {
        targetObject.SetActive(setActive);
        Debug.Log($"对话结束，立即控制 {targetObject.name} 执行 {setActive}");
    }

    private IEnumerator DelayedExecute()
    {
        Debug.Log($"对话结束，等待 {delayTime} 秒后控制 {targetObject.name} 执行 {setActive}");
        yield return new WaitForSeconds(delayTime);
        targetObject.SetActive(setActive);
        Debug.Log($"延迟结束，控制 {targetObject.name} 执行 {setActive}");
    }
}