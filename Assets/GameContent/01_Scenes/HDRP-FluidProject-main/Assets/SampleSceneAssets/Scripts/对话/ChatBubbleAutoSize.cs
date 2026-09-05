using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 聊天气泡自适应：
/// 宽度随文字伸缩（不超过 maxWidth，超过自动换行），高度随内容增长。
/// 实现 ILayoutElement，父级 VerticalLayoutGroup 会按这里计算的尺寸排布条目。
/// 同时负责条目内部排版：NameText 在上、ContentText 在下，左上对齐。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ChatBubbleAutoSize : MonoBehaviour, ILayoutElement
{
    [Tooltip("气泡最大宽度（像素），文字超过此宽度自动换行")]
    [SerializeField] private float maxWidth = 370f;

    [Tooltip("名字与正文之间的间距")]
    [SerializeField] private float nameContentSpacing = 6f;

    private RectTransform rect;
    private TMP_Text nameText;
    private TMP_Text contentText;

    // 当前计算出的气泡尺寸（供 ILayoutElement 读取）
    private float bubbleWidth;
    private float bubbleHeight;

    // 缓存，避免每帧重复测量（打字机逐字显示时内容会变化，变了才重测）
    private string cachedText;
    private int cachedMaxVisibleCharacters;

    #region ILayoutElement
    // 新版 Unity 的 ILayoutElement 新增了这两个方法；
    // 本组件的尺寸来源是 bubbleWidth/bubbleHeight 属性，这里无需额外计算。
    public void CalculateLayoutInputHorizontal() { }
    public void CalculateLayoutInputVertical() { }
    public float minWidth => bubbleWidth;
    public float preferredWidth => bubbleWidth;
    public float flexibleWidth => 0f;
    public float minHeight => bubbleHeight;
    public float preferredHeight => bubbleHeight;
    public float flexibleHeight => 0f;
    public int layoutPriority => 1;
    #endregion

    private void Awake()
    {
        rect = (RectTransform)transform;
        nameText = rect.Find("NameText")?.GetComponent<TMP_Text>();
        contentText = rect.Find("ContentText")?.GetComponent<TMP_Text>();
        cachedText = null;
        cachedMaxVisibleCharacters = int.MinValue;
    }

    /// <summary>文本赋值后立即调用一次，避免第一帧尺寸闪烁。</summary>
    public void Refresh()
    {
        Apply();
    }

    private void LateUpdate()
    {
        if (contentText == null)
            return;

        // 打字机效果通常是逐字修改 maxVisibleCharacters（或替换文字），
        // 只有这两者变化时才重新测量，避免每帧无效开销。
        if (contentText.text == cachedText && contentText.maxVisibleCharacters == cachedMaxVisibleCharacters)
            return;

        cachedText = contentText.text;
        cachedMaxVisibleCharacters = contentText.maxVisibleCharacters;
        Apply();
    }

    private void Apply()
    {
        if (contentText == null)
            return;

        // 先按最大宽度测量：得到“自然宽度”与“按 maxWidth 换行后的高度”
        RectTransform contentRect = contentText.rectTransform;
        contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth);
        contentText.ForceMeshUpdate(true, true);

        float contentW = Mathf.Min(contentText.preferredWidth, maxWidth);
        float contentH = contentText.preferredHeight;

        bool showName = nameText != null && nameText.gameObject.activeSelf;
        float nameW = 0f, nameH = 0f;
        if (showName)
        {
            nameText.ForceMeshUpdate(true, true);
            nameW = nameText.preferredWidth;
            nameH = nameText.preferredHeight;
        }

        bubbleWidth = Mathf.Max(contentW, nameW);
        bubbleHeight = contentH + (nameH > 0f ? nameH + nameContentSpacing : 0f);

        // 条目内部排版：名字在上、正文在下，左上对齐
        if (showName)
        {
            RectTransform r = nameText.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = Vector2.zero;
            r.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bubbleWidth);
            r.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, nameH);
        }

        contentRect.anchorMin = contentRect.anchorMax = new Vector2(0f, 1f);
        contentRect.pivot = new Vector2(0f, 1f);
        contentRect.anchoredPosition = new Vector2(0f, -(nameH > 0f ? nameH + nameContentSpacing : 0f));
        contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentW);
        contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentH);

        // 尺寸变化时通知父级 VerticalLayoutGroup 重新排布
        LayoutRebuilder.MarkLayoutForRebuild(rect);
    }
}
