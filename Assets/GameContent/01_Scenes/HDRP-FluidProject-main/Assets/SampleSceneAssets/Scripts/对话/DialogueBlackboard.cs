using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
[System.Serializable]
public class DialogueData
{
    public string speaker;
    [TextArea] public string content;
    public Sprite portrait;
    public int nextIndex = -1; // -1 表示无跳转，按顺序+1
    public List<DialogueChoice> choices = new List<DialogueChoice>();
    
}
[System.Serializable]
public class DialogueChoice
{
    public string choiceText;   // 选项显示的文字
    public int targetIndex;     // 选择后跳转到的对话索引
}
[System.Serializable]
public class SpeakerAvatar
{
    public string speakerName;   // 说话人名字，如 "NPC"
    public Sprite avatarSprite;  // 对应的头像图片
}


public class DialogueBlackboard : MonoBehaviour
{
    [SerializeField] public List<DialogueData> dialoguePieces = new List<DialogueData>();

    public int GetDialogueCount() => dialoguePieces.Count;
    // 如果只需要顺序访问，直接用 List 即可
    public DialogueData GetDialogue(int index)
    {
        if (index >= 0 && index < dialoguePieces.Count)
            return dialoguePieces[index];
        Debug.LogWarning($"对话索引 {index} 超出范围 (0~{dialoguePieces.Count - 1})");
        return null;
    }
    public void ClearDialogueData()
    {
        dialoguePieces?.Clear(); // 或 dialoguePieces = null;
       
    }
    public bool HasDialogue(int index)
    {
        return index >= 0 && index < dialoguePieces.Count;
    }

    public int Count => dialoguePieces.Count;
}