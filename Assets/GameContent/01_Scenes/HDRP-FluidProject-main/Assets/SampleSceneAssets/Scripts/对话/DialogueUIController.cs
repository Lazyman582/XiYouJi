using EasyTextEffects;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUIController : MonoBehaviour
{
    [SerializeField] private DialogueController controller;
    [SerializeField] private Transform contentParent;      // 消息列表父对象（ScrollView 的 Content）
    [SerializeField] private GameObject entryTemplate;         // 消息文本模板
    [SerializeField] private Button nextButton;            // “继续”按钮（依旧保留，但选项时隐藏）
    public GameObject gg;
    [Header("选项按钮模板")]
    [SerializeField] private Button choiceButtonTemplate;  // 选项按钮模板（直接放在 contentParent 下作为子物体）

    [Header("UI 组件")]
    [SerializeField] private Image avatarImage;
    private List<TMP_Text> chatTexts = new List<TMP_Text>();
    private List<Button> currentChoiceButtons = new List<Button>(); // 当前显示的选项按钮

    private void Start()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextButtonClicked);

        ShowCurrentDialogue();
    }



    private void EndDialogueSilent()
{
    // 隐藏继续按钮
    if (nextButton != null) nextButton.gameObject.SetActive(false);
    // 清除残留选项（其实已经清除过）
    ClearChoiceButtons();
    // 保留 chatTexts 中的历史文字
    Debug.Log("对话已结束，历史保留");
}

    // 显示当前对话（追加到信息流），并决定显示“继续”或选项
    public void ShowCurrentDialogue()
    {
        if (controller == null) return;
        DialogueData data = controller.GetCurrentDialogue();
        if (data == null) { EndDialogue(); return; }

        // 添加一条新对话条目
        AddDialogueEntry(data.speaker, data.content, data.portrait);

        ClearChoiceButtons();

        // 3. 判断当前节点是否有选项
        if (controller.HasChoices)
        {
            // ---- 有选项：隐藏“继续”按钮，在信息流中创建选项按钮 ----
            nextButton.interactable=false;

            var choices = controller.GetCurrentChoices();
            foreach (var choice in choices)
            {
                Button btn = Instantiate(choiceButtonTemplate, contentParent);
                btn.gameObject.SetActive(true);
                btn.GetComponentInChildren<TMP_Text>().text = choice.choiceText;

                int target = choice.targetIndex;
                btn.onClick.AddListener(() => {
                    ClearChoiceButtons(); // 立即移除选项按钮
                    if (target == -1)
                    {
                        // 结束对话（保留历史，隐藏继续按钮）
                        if (nextButton != null) nextButton.gameObject.SetActive(false);
                        // 可选：重置索引，以便下次从头开始
                        // controller.ResetDialogue();
                        EndDialogue();
                        Debug.Log("选项结束对话");
                    }
                    else
                    {
                        controller.SelectChoice(target);
                        ShowCurrentDialogue(); // 只有正常跳转时才刷新
                    }
                });
                currentChoiceButtons.Add(btn);
            }

            // 滚动到底部，显示选项按钮
            StartCoroutine(ScrollToBottom());
        }
        else
        {
            // ---- 无选项：显示“继续”按钮 ----
            nextButton.gameObject.SetActive(true);
            if (controller.IsEnd())
                nextButton.interactable = false;
            else
                nextButton.interactable = true;
        }

        // 如果已经到最后一句且无选项，自动结束（或禁用按钮）
        if (controller.IsEnd() && !controller.HasChoices)
        {
            nextButton.interactable = false;
        }
    }

    // 克隆文本消息
    private void AddDialogueEntry(string speaker, string content, Sprite portrait)
    {
        GameObject entry = Instantiate(entryTemplate, contentParent);
        entry.SetActive(true);

        Transform nameTrans = entry.transform.Find("NameText");
        Transform contentTrans = entry.transform.Find("ContentText");

        if (nameTrans == null || contentTrans == null)
        {
            Debug.LogError("预制体结构错误：缺少 NameText 或 ContentText");
            return;
        }

        TMP_Text nameText = nameTrans.GetComponent<TMP_Text>();
        TMP_Text contentText = contentTrans.GetComponent<TMP_Text>();

        nameText.text = string.IsNullOrEmpty(speaker) ? "" : $"{speaker}:";
        contentText.text = content;
        contentText.ForceMeshUpdate();

        // 🆕 更新外部的头像（而不是从克隆体里找）
        if (avatarImage != null)
        {
            avatarImage.sprite = portrait != null ? portrait : null;
        }

        TextEffect effect = contentText.GetComponent<TextEffect>();
        if (effect != null)
            effect.StartManualEffects();
    }

    // 清除所有动态选项按钮
    private void ClearChoiceButtons()
    {
        foreach (var btn in currentChoiceButtons)
        {
            if (btn != null) Destroy(btn.gameObject);
        }
        currentChoiceButtons.Clear();
    }

    // 滚动到底部（协程确保布局更新）
    private System.Collections.IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        // 假设您在场景中有一个 ScrollRect 组件，可以通过引用或 Find 获取
        // 这里为了通用性，您需要将 ScrollRect 引用传入，或使用 GetComponentInParent
        var scrollRect = GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
            scrollRect.normalizedPosition = new Vector2(0, 0);
    }

    // 点击“继续”按钮
    public void OnNextButtonClicked()
    {
        if (controller == null) return;
        if (controller.IsEnd()) return;

        controller.NextDialogue();
        ShowCurrentDialogue();
    }

    // 清空所有历史记录
    public void ClearHistory()
    {
        foreach (TMP_Text t in chatTexts)
        {
            if (t != null) Destroy(t.gameObject);
        }
        chatTexts.Clear();
    }

    // 结束对话
    public void EndDialogue()
    {
        ClearHistory();
        ClearChoiceButtons();
        controller.ResetDialogue();
        if (controller != null && controller.blackboard != null)
            controller.blackboard.ClearDialogueData();
        nextButton.gameObject.SetActive(false);
        gg.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextButtonClicked);
    }
}