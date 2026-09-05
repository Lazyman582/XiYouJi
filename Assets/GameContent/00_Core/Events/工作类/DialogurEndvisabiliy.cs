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

    [SerializeField] private DialogueDataContainer targetContainer;
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

     
        if (targetContainer != null)
        {
            var currentContainer = DialogueController.Instance?.currentContainer;
            if (currentContainer != targetContainer) return;
        }

        // 3. 执行逻辑
        if (targetObject != null)
        {
            targetObject.SetActive(setActive);
            Debug.Log($"对话结束，控制 {targetObject.name} 执行 {setActive}");
        }

    }
}