using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCInteractionController : MonoBehaviour
{
    [Header("碰撞体控制")]
    [SerializeField] private Collider npcCollider;          // 3D 碰撞体
    // 如果是 2D 游戏，用 Collider2D，下面会兼容

    [Header("对话结束后是否恢复碰撞体")]
    [SerializeField] private bool enableColliderAfterDialogue = true; // 可在 Inspector 调整

    private void OnEnable()
    {
        // 订阅对话事件
        DialogueController.OnDialogueStart += OnDialogueStart;
        DialogueController.OnDialogueEnd += OnDialogueEnd;
    }

    private void OnDisable()
    {
        // 取消订阅，防止内存泄漏
        DialogueController.OnDialogueStart -= OnDialogueStart;
        DialogueController.OnDialogueEnd -= OnDialogueEnd;
    }

    private void OnDialogueStart()
    {
        // 对话开始：禁用碰撞体（无论之前是什么状态）
        SetColliderEnabled(false);
        Debug.Log($"{gameObject.name} 碰撞体已禁用");
    }

    private void OnDialogueEnd()
    {
        // 对话结束：根据 bool 决定是否恢复
        if (enableColliderAfterDialogue)
        {
            SetColliderEnabled(true);
            Debug.Log($"{gameObject.name} 碰撞体已恢复");
        }
        else
        {
            Debug.Log($"{gameObject.name} 碰撞体保持禁用");
        }
    }

    private void SetColliderEnabled(bool enabled)
    {
        if (npcCollider != null)
            npcCollider.enabled = enabled;
        else
            Debug.LogWarning($"{gameObject.name} 未指定碰撞体组件");
    }
}