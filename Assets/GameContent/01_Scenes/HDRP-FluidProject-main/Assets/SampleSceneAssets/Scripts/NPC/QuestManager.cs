using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    [SerializeField] private DialogueDataContainer newDialogueForNPC; // 新对话资产

    public void UpdateNPCDialogue(NPCInteract npc)
    {
        // 直接修改 NPC 身上的引用
        npc.SetDialogueContainer(newDialogueForNPC);
    }
}
