using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DialogueController : MonoBehaviour
{
    public static DialogueController Instance { get; private set; }

    [Header("当前对话数据")]
    private DialogueDataContainer currentContainer;
    private int currentIndex = 0;
    [SerializeField]public int CurrentIndex => currentIndex;
    public static event System.Action OnDialogueStart;

    private bool isBlocked = false;

    public bool IsBlocked => isBlocked;

    public void BlockProceed()
    {
        isBlocked = true;
        Debug.Log("[DialogueController] 对话流程被阻塞");
    }

    public void UnblockProceed()
    {
        isBlocked = false;
        Debug.Log("[DialogueController] 对话流程已恢复");
        // 恢复后自动继续显示下一条
        if (DialogueUIController.Instance != null)
        {
            DialogueUIController.Instance.ShowCurrentDialogue();
        }
    }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // 由 NPC 调用，开始新对话
    public void StartDialogue(DialogueDataContainer container)
    {
        if (container == null)
        {
            Debug.LogError("试图开始空对话容器");
            return;
        }
        currentContainer = container;
        currentIndex = 0;
        OnDialogueStart?.Invoke();
    }

    public DialogueData GetCurrentDialogue()
    {
        if (currentContainer == null || currentIndex < 0 || currentIndex >= currentContainer.dialoguePieces.Count)
            return null;
        return currentContainer.dialoguePieces[currentIndex];
    }

    public void NextDialogue()
    {
        if (isBlocked)
        {
            Debug.Log("[DialogueController] 对话被阻塞，无法继续");
            return;
        }
        if (currentContainer == null) return;
        int next = currentIndex + 1;
        if (next < currentContainer.dialoguePieces.Count)
            currentIndex = next;
        else
            Debug.Log("已经是最后一条对话");
    }

    public void JumpToDialogue(int targetIndex)
    {
        if (currentContainer != null && targetIndex >= 0 && targetIndex < currentContainer.dialoguePieces.Count)
            currentIndex = targetIndex;
    }

    public void ResetDialogue() => currentIndex = 0;

    public bool IsEnd()
    {
        if (currentContainer == null) return true;
        
        return currentIndex >= currentContainer.dialoguePieces.Count - 1;

    }

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

    public bool HasDialogue(int index)
    {
        return currentContainer != null && index >= 0 && index < currentContainer.dialoguePieces.Count;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}

