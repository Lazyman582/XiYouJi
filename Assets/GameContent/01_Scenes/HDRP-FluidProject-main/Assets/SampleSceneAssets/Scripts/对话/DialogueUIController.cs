using EasyTextEffects;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using EasyTextEffects;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUIController : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private Transform contentParent;      // ScrollView 的 Content
    [SerializeField] private GameObject entryTemplate;     // 对话条目模板
    [SerializeField] private Button nextButton;            // “继续”按钮
    [SerializeField] private Button choiceButtonTemplate;  // 选项按钮模板
    [SerializeField] private GameObject dialoguePanelRoot; // 对话面板根节点
    [SerializeField] private Image avatarImage;            // 说话人头像

    // 这个变量保留，用途不明，不动它
    public GameObject gg;

    // 存储历史对话条目（用于清空）
    private List<GameObject> historyEntries = new List<GameObject>();
    // 存储当前显示的选项按钮（用于清除）
    private List<Button> currentChoiceButtons = new List<Button>();

    public static DialogueUIController Instance { get; private set; }

    public static event System.Action OnDialogueEnd;
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

    private void Start()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextButtonClicked);
        ClosePanel();
    }

    public void ClosePanel()
    {
        if (dialoguePanelRoot != null)
            dialoguePanelRoot.SetActive(false);
    }

    public void OpenPanel()
    {
        if (dialoguePanelRoot != null)
            dialoguePanelRoot.SetActive(true);
    }

    // 显示当前对话（追加到信息流）
    public void ShowCurrentDialogue()
    {
        var controller = DialogueController.Instance;
        if (controller == null) return;

        DialogueData data = controller.GetCurrentDialogue();
        if (data == null)
        {
            EndDialogue();
            return;
        }
        Debug.Log($"[对话调试] 当前索引: {controller.CurrentIndex} ");
        // 添加对话条目
        AddDialogueEntry(data.speaker, data.content, data.portrait);

        // 清除旧的选项按钮
        ClearChoiceButtons();

        // 判断是否有选项
        if (controller.HasChoices)
        {
            // 有选项：隐藏“继续”按钮，创建选项按钮
            nextButton.interactable = false;
         

            var choices = controller.GetCurrentChoices();
            foreach (var choice in choices)
            {
                Button btn = Instantiate(choiceButtonTemplate, contentParent);
                btn.gameObject.SetActive(true);
                btn.GetComponentInChildren<TMP_Text>().text = choice.choiceText;

                int target = choice.targetIndex;
                btn.onClick.AddListener(() =>
                {
                    ClearChoiceButtons(); // 立即移除选项按钮
                    if (target == -1)
                    {
                        // 选项标记为结束对话
                        EndDialogue();
                        Debug.Log("选项结束对话");
                    }
                    else
                    {
                        // 跳转到目标索引
                       
                        controller.JumpToDialogue(target);
                        ShowCurrentDialogue(); // 刷新显示
                    }
                });
                currentChoiceButtons.Add(btn);
            }

            StartCoroutine(ScrollToBottom());
        }
        else
        {
            // 无选项：显示“继续”按钮
            nextButton.gameObject.SetActive(true);
            if (controller.IsEnd())
                nextButton.interactable = false;
            else
                nextButton.interactable = true;
        }

        // 如果已经是最后一句且无选项，禁用“继续”并自动结束？
        if (controller.IsEnd() && !controller.HasChoices)
        {
            nextButton.interactable = false;
            // 如果你想自动结束，可以调用 EndDialogue();
        }
    }

    // 添加一条对话条目
    private void AddDialogueEntry(string speaker, string content, Sprite portrait)
    {
        GameObject entry = Instantiate(entryTemplate, contentParent);
        entry.SetActive(true);
        historyEntries.Add(entry); // 存储以便清空

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

        // 更新头像
        if (avatarImage != null)
        {
            avatarImage.sprite = portrait != null ? portrait : null;
        }

        // 文本特效（如果有）
        TextEffect effect = contentText.GetComponent<TextEffect>();
        if (effect != null)
            effect.StartManualEffects();
    }

    // 清除选项按钮
    private void ClearChoiceButtons()
    {
        foreach (var btn in currentChoiceButtons)
        {
            if (btn != null) Destroy(btn.gameObject);
        }
        currentChoiceButtons.Clear();
    }

    // 清空所有历史对话条目
    public void ClearHistory()
    {
        foreach (var entry in historyEntries)
        {
            if (entry != null) Destroy(entry);
        }
        historyEntries.Clear();
    }

    // 结束对话（清空历史、选项，重置索引，关闭面板）
    public void EndDialogue()
    {
        ClearHistory();
        ClearChoiceButtons();

        var controller = DialogueController.Instance;
        if (controller != null)
            controller.ResetDialogue();

        if (nextButton != null)
            nextButton.gameObject.SetActive(false);

        // 关闭面板（根据需求，也可以保留打开但隐藏按钮）
        ClosePanel();

        // 如果 gg 有特殊用途，可以处理，这里保持原样
        if (gg != null)
            gg.SetActive(false);

        OnDialogueEnd?.Invoke();

    }

    // 点击“继续”按钮
    public void OnNextButtonClicked()
    {
        var controller = DialogueController.Instance;
        if (controller == null) return;
        if (controller.IsEnd()) return;

        controller.NextDialogue();
        ShowCurrentDialogue();
    }

    // 滚动到底部（协程）
    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        var scrollRect = GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
            scrollRect.normalizedPosition = new Vector2(0, 0);
    }

    private void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextButtonClicked);
    }
}