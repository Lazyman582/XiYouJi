using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DialogueController : MonoBehaviour
{
    [Header("对话黑板")]
    [SerializeField] public DialogueBlackboard blackboard;

    [Header("当前索引")]
    [SerializeField] private int currentIndex = 0;

    public bool HasChoices
    {
        get
        {
            var data = GetCurrentDialogue();
            return data != null && data.choices != null && data.choices.Count > 0;
        }
    }
    public List<DialogueChoice> GetCurrentChoices()
    {
        var data = GetCurrentDialogue();
        return data != null ? data.choices : null;
    }
    public void SelectChoice(int targetIndex)
    {
        if (HasDialogue(targetIndex))
        {
            currentIndex = targetIndex;
        }
        else
        {
            Debug.LogWarning($"目标索引 {targetIndex} 无效");
        }
    }
    public int CurrentIndex => currentIndex;

    public DialogueData GetCurrentDialogue()
    {
        if (blackboard == null)
        {
            Debug.LogError("DialogueBlackboard 未赋值！");
            return null;
        }
        return blackboard.GetDialogue(currentIndex);
    }

   

    // 推进到下一句（优先使用 nextIndex 跳转）
    public void NextDialogue()
    {
        if (blackboard == null) return;
        int next = currentIndex + 1;
        if (blackboard.HasDialogue(next))   // 检查下一个索引是否存在
        {
            currentIndex = next;
        }
        else
        {
            Debug.Log("已经是最后一条对话，无法继续");
        }
    }

    // 强制跳转到指定索引（外部调用，例如按钮点击）
    public void JumpToDialogue(int targetIndex)
    {
        if (blackboard != null && blackboard.HasDialogue(targetIndex))
            currentIndex = targetIndex;
        else
            Debug.LogWarning($"无法跳转到索引 {targetIndex}");
    }

    public void ResetDialogue() => currentIndex = 0;

    public bool IsEnd()
    {
        if (blackboard == null) return true;
        // 如果当前索引已经是最后一条，则结束
        return currentIndex >= blackboard.GetDialogueCount() - 1;

    }

    // 可选：设置当前索引（慎用）
    public void SetIndex(int index)
    {
        if (blackboard != null && blackboard.HasDialogue(index))
            currentIndex = index;
    }
    public bool HasDialogue(int index)
    {
        return blackboard != null && blackboard.HasDialogue(index);
    }

}