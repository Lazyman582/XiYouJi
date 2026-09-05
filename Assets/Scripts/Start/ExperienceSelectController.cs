using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ExperienceSelectController : MonoBehaviour
{
    public RectTransform scrollContent;
    public GameObject[] characterCards;
    public GameObject[] indicatorDots;

    public float cardSpacing = 400f;
    public float snapSpeed = 8f;
    public float dragThreshold = 50f;

    private int currentIndex;
    private float targetX = 0f;
    private float currentX = 0f;
    private bool isSnapping = false;
    private Vector2 dragStartPos;
    private float dragStartX;
    private bool pointerInArea = false;
    private Color activeDotColor = new Color(0.95f, 0.85f, 0.5f);
    private Color inactiveDotColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    private void Start()
    {
        if (characterCards != null && characterCards.Length > 0)
        {
            currentIndex = characterCards.Length / 2;
            SnapToIndex(currentIndex, false);
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("CharSelect_"))
            {
                Transform selBtn = child.Find("SelectBtn");
                if (selBtn != null)
                {
                    int idx = int.Parse(child.name.Substring(child.name.IndexOf('_') + 1));
                    Button btn = selBtn.GetComponent<Button>();
                    if (btn != null) btn.onClick.AddListener(() => SelectCharacter(idx));
                }
            }
        }
    }

    private void OnEnable()
    {
        if (characterCards != null && characterCards.Length > 0)
        {
            currentIndex = characterCards.Length / 2;
            SnapToIndex(currentIndex, false);
            PlayEnterAnimation();
        }
    }

    private void Update()
    {
        if (characterCards == null || characterCards.Length == 0) return;
        HandleInput();
        HandleSnapping();
        UpdateCardScales();
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RectTransform rect = scrollContent != null ? scrollContent : GetComponent<RectTransform>();
            if (rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, null))
            {
                pointerInArea = true;
                isSnapping = false;
                dragStartPos = Input.mousePosition;
                dragStartX = currentX;
            }
        }

        if (Input.GetMouseButton(0) && pointerInArea)
        {
            float deltaX = Input.mousePosition.x - dragStartPos.x;
            currentX = dragStartX + deltaX;
            if (scrollContent != null)
                scrollContent.anchoredPosition = new Vector2(currentX, scrollContent.anchoredPosition.y);
        }

        if (Input.GetMouseButtonUp(0) && pointerInArea)
        {
            pointerInArea = false;
            float totalDrag = currentX - dragStartX;
            if (Mathf.Abs(totalDrag) > dragThreshold)
            {
                if (totalDrag > 0 && currentIndex > 0) currentIndex--;
                else if (totalDrag < 0 && currentIndex < characterCards.Length - 1) currentIndex++;
            }
            isSnapping = true;
            targetX = GetTargetX(currentIndex);
            UpdateIndicators();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            if (currentIndex > 0) { currentIndex--; SnapToIndex(currentIndex, true); }
        }
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            if (currentIndex < characterCards.Length - 1) { currentIndex++; SnapToIndex(currentIndex, true); }
        }
    }

    private void HandleSnapping()
    {
        if (!isSnapping || scrollContent == null) return;
        currentX = Mathf.Lerp(currentX, targetX, Time.deltaTime * snapSpeed);
        scrollContent.anchoredPosition = new Vector2(currentX, scrollContent.anchoredPosition.y);
        if (Mathf.Abs(currentX - targetX) < 0.5f)
        {
            currentX = targetX;
            scrollContent.anchoredPosition = new Vector2(targetX, scrollContent.anchoredPosition.y);
            isSnapping = false;
        }
    }

    private void SnapToIndex(int index, bool animate)
    {
        currentIndex = index;
        targetX = GetTargetX(index);
        if (animate) { isSnapping = true; }
        else
        {
            currentX = targetX;
            if (scrollContent != null) scrollContent.anchoredPosition = new Vector2(targetX, scrollContent.anchoredPosition.y);
        }
        UpdateIndicators();
    }

    private float GetTargetX(int index)
    {
        if (characterCards == null || index < 0 || index >= characterCards.Length ||
            characterCards[index] == null)
            return -index * cardSpacing;

        RectTransform cardRect = characterCards[index].GetComponent<RectTransform>();
        return cardRect != null ? -cardRect.anchoredPosition.x : -index * cardSpacing;
    }

    private void UpdateCardScales()
    {
        if (characterCards == null) return;
        for (int i = 0; i < characterCards.Length; i++)
        {
            if (characterCards[i] == null) continue;
            float targetScale = (i == currentIndex) ? 1.1f : 0.85f;
            float s = Mathf.Lerp(characterCards[i].transform.localScale.x, targetScale, Time.deltaTime * 8f);
            characterCards[i].transform.localScale = new Vector3(s, s, 1f);

            CanvasGroup cg = characterCards[i].GetComponent<CanvasGroup>();
            if (cg == null) cg = characterCards[i].AddComponent<CanvasGroup>();
            cg.alpha = Mathf.Lerp(cg.alpha, (i == currentIndex) ? 1f : 0.6f, Time.deltaTime * 8f);
        }
    }

    private void UpdateIndicators()
    {
        if (indicatorDots == null) return;
        for (int i = 0; i < indicatorDots.Length; i++)
        {
            if (indicatorDots[i] == null) continue;
            Image dotImg = indicatorDots[i].GetComponent<Image>();
            if (dotImg != null) dotImg.DOColor((i == currentIndex) ? activeDotColor : inactiveDotColor, 0.3f);
            indicatorDots[i].transform.DOScale(Vector3.one * ((i == currentIndex) ? 1.3f : 1f), 0.3f);
        }
    }

    private void PlayEnterAnimation()
    {
        if (characterCards == null) return;
        for (int i = 0; i < characterCards.Length; i++)
        {
            if (characterCards[i] == null) continue;
            characterCards[i].transform.localScale = Vector3.zero;
            characterCards[i].transform.DOScale(Vector3.one * 0.85f, 0.5f).SetEase(Ease.OutBack).SetDelay(0.1f * i + 0.2f);
        }
    }

    public void SelectCharacter(int index)
    {
        Debug.Log("Selected character: " + index);
    }
}
