using UnityEngine;

// 制作名单自动滚动：内容从视口底部匀速上移，滚出后循环。
// 挂在制作名单面板上，content 指向 ScrollRect 的 Content（锚点底部、pivot 底部）。
public class CreditsAutoScroller : MonoBehaviour
{
    public RectTransform content;
    public float speed = 45f;
    public float edgePadding = 60f;

    private float viewportHeight;
    private float contentHeight;
    private float posMin;
    private float posMax;
    private float pos;

    private void OnEnable()
    {
        if (content == null) return;
        Canvas.ForceUpdateCanvases();
        RectTransform viewport = content.parent as RectTransform;
        contentHeight = content.rect.height;
        viewportHeight = viewport != null ? viewport.rect.height : 400f;
        posMin = -edgePadding;
        posMax = contentHeight + edgePadding;
        pos = posMin;
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, pos);
    }

    private void Update()
    {
        if (content == null) return;
        pos += speed * Time.deltaTime;
        if (pos > posMax) pos = posMin;
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, pos);
    }
}
