using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

// 故事背景"五张知识卡片"横滑容器：拖动结束后吸附到最近的卡片，
// 圆点按钮可点击直达（SnapTo），并高亮当前卡片对应的圆点。
public class CardSnapScroller : MonoBehaviour, IEndDragHandler
{
    public ScrollRect scrollRect;
    public int cardCount = 5;
    public float snapTime = 0.25f;
    public Image[] dots;
    public Color activeDotColor = new Color(0.55f, 0.32f, 0.1f, 1f);
    public Color inactiveDotColor = new Color(0.55f, 0.32f, 0.1f, 0.25f);

    private Coroutine routine;

    private void Update()
    {
        if (dots == null || scrollRect == null) return;
        int current = Mathf.RoundToInt(scrollRect.horizontalNormalizedPosition * (cardCount - 1));
        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] == null) continue;
            dots[i].color = (i == current) ? activeDotColor : inactiveDotColor;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (scrollRect == null || cardCount <= 1) return;
        int nearest = Mathf.RoundToInt(scrollRect.horizontalNormalizedPosition * (cardCount - 1));
        SnapTo(nearest);
    }

    public void SnapTo(int index)
    {
        if (scrollRect == null || cardCount <= 1) return;
        index = Mathf.Clamp(index, 0, cardCount - 1);
        float target = (float)index / (cardCount - 1);
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(SnapRoutine(target));
    }

    private IEnumerator SnapRoutine(float target)
    {
        ScrollRect sr = scrollRect;
        float start = sr.horizontalNormalizedPosition;
        float t = 0f;
        while (t < snapTime)
        {
            t += Time.unscaledDeltaTime;
            sr.horizontalNormalizedPosition = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / snapTime));
            yield return null;
        }
        sr.horizontalNormalizedPosition = target;
        routine = null;
    }
}
