using UnityEngine;
using UnityEngine.UI;

public class BookReaderController : MonoBehaviour
{
    public RectTransform bookArea;
    public Text pageIndicator;

    private BookPro bookPro;
    private int totalPages;
    private bool isTurningPage;

    private void Start()
    {
        SetupBookPro();
    }

    // 书本与全部书页（含正文文本）已在 Start 场景中直接搭建并序列化，
    // 这里只接管场景里的 BookPro，不再在运行时实例化或生成任何页面。
    private void SetupBookPro()
    {
        if (bookPro == null)
        {
            if (bookArea != null)
                bookPro = bookArea.GetComponentInChildren<BookPro>(true);
            if (bookPro == null)
                bookPro = Object.FindObjectOfType<BookPro>();
        }

        if (bookPro == null)
        {
            Debug.LogWarning("BookReaderController: 场景中未找到 BookPro。");
            return;
        }

        totalPages = bookPro.papers != null ? bookPro.papers.Length * 2 : 0;
        bookPro.interactable = true;
        bookPro.StartFlippingPaper = 0;
        bookPro.EndFlippingPaper = Mathf.Max(0, (bookPro.papers != null ? bookPro.papers.Length : 1) - 1);

        if (bookPro.OnFlip == null) bookPro.OnFlip = new UnityEngine.Events.UnityEvent();
        bookPro.OnFlip.RemoveListener(UpdatePageIndicator);
        bookPro.OnFlip.AddListener(UpdatePageIndicator);

        bookPro.UpdatePages();
        UpdatePageIndicator();
    }

    public void NextPage()
    {
        if (bookPro == null || isTurningPage || bookPro.currentPaper > bookPro.EndFlippingPaper)
            return;

        isTurningPage = true;
        PageFlipper.FlipPage(bookPro, 0.5f, FlipMode.RightToLeft, OnPageTurnComplete);
    }

    public void PreviousPage()
    {
        if (bookPro == null || isTurningPage || bookPro.currentPaper <= bookPro.StartFlippingPaper)
            return;

        isTurningPage = true;
        PageFlipper.FlipPage(bookPro, 0.5f, FlipMode.LeftToRight, OnPageTurnComplete);
    }

    private void OnPageTurnComplete()
    {
        isTurningPage = false;
        UpdatePageIndicator();
    }

    private void UpdatePageIndicator()
    {
        if (pageIndicator == null || bookPro == null || bookPro.papers == null)
            return;

        int total = bookPro.papers.Length * 2;
        int current = bookPro.currentPaper;
        int left = current * 2;
        int right = current * 2 + 1;

        if (total == 0)
            pageIndicator.text = string.Empty;
        else if (current <= 0)
            pageIndicator.text = "1 / " + total;
        else if (right > total)
            pageIndicator.text = left + " / " + total;
        else
            pageIndicator.text = left + "-" + right + " / " + total;
    }
}
