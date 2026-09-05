using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BookReaderController : MonoBehaviour
{
    public RectTransform bookArea;
    public Text pageIndicator;
    public GameObject bookProPrefab;

    private BookPro bookPro;
    private int totalPages = 5;
    private bool isTurningPage;

    private string[] pageContents = new string[]
    {
        "第一页\n\n却说三藏师徒，次日天明，整顿行装，马不停蹄，往西而去。\n\n时值初冬，但见那：霜凋红叶林，风落枯黄草。山前翠柏尚青青，涧下寒泉犹浩浩。\n\n师徒们正行处，忽见一座高山。唐僧道：'徒弟们，且休闲讲。那个会求雨，与他求一场甘雨，以济民瘼。'",
        "第二页\n\n好大圣，捻着诀，使个隐身法，径到那山坡之下。只见那壁厢有一个女子，生得：\n\n冰肌藏玉骨，衫领露酥胸。柳眉积翠黛，杏眼闪银星。\n\n月样容仪俏，天然性格清。体似燕藏柳，声如莺啭林。\n\n那女子手提一个青砂罐儿，从西向东，径奔唐僧而来。",
        "第三页\n\n行者认得那女子是个妖精，放下钵盂，掣铁棒，当头就打。\n\n唬得个长老用手扯住道：'悟空！你要打谁？'\n\n行者道：'师父，你面前这个女子，是个妖精。他是一个潜灵作怪的僵尸，在此迷人败本；被我识破，就待走时，被我使个定身法定住，现了本相。'",
        "第四页\n\n那妖怪又变做一个老妇人，年满八旬，手拄着一根弯头竹杖，一步一声的哭着走来。\n\n八戒见了，大惊道：'师父！不好了！那妈妈来寻人了！'\n\n唐僧道：'悟空！你怎么又打死人？'\n\n行者道：'师父莫怪，且看看是何等人。'近前看时，那里是甚妈妈，却是一具骷髅。",
        "第五页\n\n那妖怪第三次，又变做一个老公公，真是个白发盈颠，手持数珠，口里念着经，从南坡上走来。\n\n行者笑道：'你是个妖精，瞒不得我！'\n\n举棒便打，那妖怪倒地，现了本相，原来是一堆白骨，脊梁上写着'白骨夫人'四字。\n\n这就是三打白骨精的故事，揭示了妖邪善于伪装，而悟空火眼金睛能识破一切真相。"
    };

    private void Start()
    {
        SetupBookPro();
    }

    private void SetupBookPro()
    {
        var prefab = bookProPrefab != null
            ? bookProPrefab
            : Resources.Load<GameObject>("BookPro");

        if (prefab == null || bookArea == null)
        {
            Debug.LogWarning("BookReaderController: BookPro prefab or BookArea not found.");
            return;
        }

        var bookObj = Instantiate(prefab, bookArea);
        bookObj.name = "BookProInstance";
        var bookRect = bookObj.GetComponent<RectTransform>();
        if (bookRect != null)
        {
            bookRect.anchorMin = Vector2.zero;
            bookRect.anchorMax = Vector2.one;
            bookRect.offsetMin = Vector2.zero;
            bookRect.offsetMax = Vector2.zero;
        }

        bookPro = bookObj.GetComponent<BookPro>();
        if (bookPro == null) return;

        bookPro.interactable = true;
        HidePrefabSamplePages();

        var coverFront = CreatePage("CoverPage", "西游记\n\n三打白骨精\n\n原著阅读", 28);
        var page1 = CreatePage("Page1", pageContents[0], 16);
        var page2 = CreatePage("Page2", pageContents[1], 16);
        var page3 = CreatePage("Page3", pageContents[2], 16);
        var page4 = CreatePage("Page4", pageContents[3], 16);
        var page5 = CreatePage("Page5", pageContents[4], 16);
        bookPro.papers = new Paper[]
        {
            new Paper { Front = coverFront, Back = page1 },
            new Paper { Front = page2, Back = page3 },
            new Paper { Front = page4, Back = page5 },
        };

        bookPro.StartFlippingPaper = 0;
        bookPro.EndFlippingPaper = bookPro.papers.Length - 1;
        totalPages = pageContents.Length;

        if (bookPro.OnFlip == null) bookPro.OnFlip = new UnityEngine.Events.UnityEvent();
        bookPro.OnFlip.AddListener(UpdatePageIndicator);
        bookPro.UpdatePages();
        UpdatePageIndicator();
    }

    private void HidePrefabSamplePages()
    {
        if (bookPro.papers == null)
            return;

        foreach (Paper paper in bookPro.papers)
        {
            if (paper == null)
                continue;

            if (paper.Front != null)
                BookUtility.HidePage(paper.Front);
            if (paper.Back != null)
                BookUtility.HidePage(paper.Back);
        }
    }

    private GameObject CreatePage(string pageName, string text, int fontSize)
    {
        var pageObj = new GameObject(pageName, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        pageObj.transform.SetParent(bookArea, false);
        var pageRect = pageObj.GetComponent<RectTransform>();
        pageRect.anchorMin = Vector2.zero;
        pageRect.anchorMax = Vector2.one;
        pageRect.offsetMin = Vector2.zero;
        pageRect.offsetMax = Vector2.zero;

        var img = pageObj.GetComponent<Image>();
        img.color = new Color(0.96f, 0.93f, 0.86f, 1f);

        var textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(pageObj.transform, false);
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.08f, 0.08f);
        textRect.anchorMax = new Vector2(0.92f, 0.92f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var textComp = textObj.GetComponent<Text>();
        textComp.text = text;
        textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (textComp.font == null) textComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        textComp.fontSize = fontSize;
        textComp.color = new Color(0.15f, 0.12f, 0.1f);
        textComp.alignment = TextAnchor.UpperLeft;
        textComp.lineSpacing = 1.5f;

        return pageObj;
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
        if (pageIndicator == null)
            return;

        if (bookPro == null || bookPro.currentPaper <= 0)
            pageIndicator.text = "封面 / " + totalPages;
        else if (bookPro.currentPaper == 1)
            pageIndicator.text = "1-2 / " + totalPages;
        else if (bookPro.currentPaper == 2)
            pageIndicator.text = "3-4 / " + totalPages;
        else
            pageIndicator.text = "5 / " + totalPages;
    }
}
