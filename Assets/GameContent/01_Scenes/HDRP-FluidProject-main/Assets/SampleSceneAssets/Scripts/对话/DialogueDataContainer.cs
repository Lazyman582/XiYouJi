using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueData", menuName = "对话系统/对话数据容器")]
public class DialogueDataContainer:ScriptableObject
{
    public List<DialogueData> dialoguePieces = new List<DialogueData>();
    public string title;
    public string author;
    // 其他元数据
}