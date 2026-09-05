using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;

public static class SetupStartScene
{
    [MenuItem("XiYouJi/Build Start Scene UI")]
    public static void BuildUI()
    {
        // Cleanup old objects
        foreach (var entry in GameObject.FindObjectsOfType<StartSceneEntry>())
            GameObject.DestroyImmediate(entry.gameObject);
        var oldCanvas = GameObject.Find("MainCanvas");
        if (oldCanvas != null) GameObject.DestroyImmediate(oldCanvas);
        var oldES = GameObject.FindObjectOfType<EventSystem>();
        if (oldES != null) GameObject.DestroyImmediate(oldES.gameObject);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 14);

        // ========== EventSystem ==========
        GameObject esObj = new GameObject("EventSystem");
        esObj.AddComponent<EventSystem>();
        esObj.AddComponent<StandaloneInputModule>();

        // ========== StartSceneRoot ==========
        GameObject rootGO = new GameObject("StartSceneRoot");
        rootGO.AddComponent<StartSceneEntry>();

        // ========== MainCanvas ==========
        GameObject canvasObj = new GameObject("MainCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // Background
        CreateImage("Background", canvasObj.transform, new Color(0.08f, 0.06f, 0.12f, 1f), true);

        // RootPanel
        GameObject rootPanel = CreateGO("RootPanel", canvasObj.transform, true);

        // ========== COVER PANEL ==========
        GameObject coverPanel = CreateGO("CoverPanel", rootPanel.transform, true);
        coverPanel.AddComponent<CanvasGroup>();

        CreateImage("CoverBackgroundImage", coverPanel.transform, new Color(0.15f, 0.1f, 0.2f, 0.5f), true);

        GameObject coverTitle = CreateText("Title", coverPanel.transform,
            "\u897F\u6E38\u8BB0", 120, new Color(0.95f, 0.85f, 0.5f), TextAnchor.MiddleCenter, font);
        coverTitle.GetComponent<RectTransform>().SetAnchoredPos(
            new Vector2(0, 150), new Vector2(800, 150));
        coverTitle.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Overflow;

        GameObject coverSubtitle = CreateText("Subtitle", coverPanel.transform,
            "\u4E09\u6253\u767D\u9AA8\u7CBE \u00B7 \u6C89\u6D78\u5F0F\u4E92\u52A8\u4F53\u9A8C", 40,
            new Color(0.9f, 0.85f, 0.75f), TextAnchor.MiddleCenter, font);
        coverSubtitle.GetComponent<RectTransform>().SetAnchoredPos(
            new Vector2(0, 50), new Vector2(800, 60));
        coverSubtitle.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Overflow;

        GameObject enterBtn = CreateButton("EnterButton", coverPanel.transform,
            "\u8FDB\u5165\u4F53\u9A8C", 30, new Vector2(0, -120), new Vector2(300, 80),
            new Color(0.7f, 0.5f, 0.2f, 0.9f), font);

        CoverController coverCtrl = coverPanel.AddComponent<CoverController>();
        coverCtrl.titleText = coverTitle;
        coverCtrl.subtitleText = coverSubtitle;
        coverCtrl.enterButton = enterBtn;

        // ========== MAIN MENU PANEL ==========
        GameObject mainMenuPanel = CreateGO("MainMenuPanel", rootPanel.transform, true);
        mainMenuPanel.AddComponent<CanvasGroup>();
        mainMenuPanel.SetActive(false);

        CreateText("MenuTitle", mainMenuPanel.transform,
            "\u9009\u62E9\u6A21\u5757", 64, new Color(0.95f, 0.85f, 0.5f), TextAnchor.MiddleCenter, font)
            .GetComponent<RectTransform>().SetAnchoredPos(new Vector2(0, 300), new Vector2(600, 80));

        string[] moduleNames = { "\u4ECB\u7ECD\u6A21\u5757", "\u4F53\u9A8C\u6A21\u5757", "\u95EE\u7B54\u6A21\u5757" };
        string[] moduleDescs = { "\u4E86\u89E3\u540D\u8457\u80CC\u666F\u4E0E\u4EBA\u7269", "\u591A\u89C6\u89D2\u6C89\u6D78\u5267\u60C5\u4F53\u9A8C", "\u8DA3\u5473\u77E5\u8BC6\u95EE\u7B54\u6311\u6218" };
        Color[] moduleColors = {
            new Color(0.25f, 0.55f, 0.75f, 0.9f),
            new Color(0.75f, 0.45f, 0.2f, 0.9f),
            new Color(0.4f, 0.7f, 0.35f, 0.9f)
        };

        Button[] moduleBtns = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            float xPos = (i - 1) * 350;
            GameObject btnObj = CreateButtonGO("ModuleBtn_" + i, mainMenuPanel.transform,
                new Vector2(xPos, -20), new Vector2(280, 350), moduleColors[i]);

            CreateImage("Icon", btnObj.transform, new Color(1f, 1f, 1f, 0.3f), false)
                .GetComponent<RectTransform>().SetAnchoredPos(Vector2.zero + new Vector2(0, 60), new Vector2(150, 150));
            CreateText("BtnTitle", btnObj.transform, moduleNames[i], 36, Color.white, TextAnchor.MiddleCenter, font)
                .GetComponent<RectTransform>().SetAnchoredPos(new Vector2(0, -60), new Vector2(260, 50));
            CreateText("BtnDesc", btnObj.transform, moduleDescs[i], 22, new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleCenter, font)
                .GetComponent<RectTransform>().SetAnchoredPos(new Vector2(0, -110), new Vector2(260, 60));

            moduleBtns[i] = btnObj.GetComponent<Button>();
        }

        ModuleMenuController menuCtrl = mainMenuPanel.AddComponent<ModuleMenuController>();
        menuCtrl.introButton = moduleBtns[0].gameObject;
        menuCtrl.experienceButton = moduleBtns[1].gameObject;
        menuCtrl.projectButton = moduleBtns[2].gameObject;

        // ========== INTRODUCTION PANEL ==========
        GameObject introPanel = CreateGO("IntroductionPanel", rootPanel.transform, true);
        introPanel.AddComponent<CanvasGroup>();
        introPanel.SetActive(false);

        GameObject introTitle = CreateText("IntroTitle", introPanel.transform,
            "\u4ECB\u7ECD\u6A21\u5757", 52, new Color(0.95f, 0.85f, 0.5f), TextAnchor.MiddleCenter, font);
        RectTransform itRect = introTitle.GetComponent<RectTransform>();
        itRect.anchorMin = new Vector2(0f, 1f); itRect.anchorMax = new Vector2(1f, 1f);
        itRect.pivot = new Vector2(0.5f, 1f);
        itRect.anchoredPosition = new Vector2(0, -20); itRect.sizeDelta = new Vector2(0, 70);

        // Tab Container
        GameObject tabContainer = CreateGO("TabContainer", introPanel.transform, false);
        RectTransform tcRect = tabContainer.GetComponent<RectTransform>();
        tcRect.anchorMin = new Vector2(0f, 1f); tcRect.anchorMax = new Vector2(1f, 1f);
        tcRect.pivot = new Vector2(0.5f, 1f);
        tcRect.anchoredPosition = new Vector2(0, -110); tcRect.sizeDelta = new Vector2(0, 60);
        HorizontalLayoutGroup tabHlg = tabContainer.AddComponent<HorizontalLayoutGroup>();
        tabHlg.spacing = 20; tabHlg.childAlignment = TextAnchor.MiddleCenter;
        tabHlg.childForceExpandWidth = true; tabHlg.childForceExpandHeight = true;
        tabHlg.padding = new RectOffset(200, 200, 0, 0);

        string[] tabNames = { "\u6545\u4E8B\u80CC\u666F", "\u4EBA\u7269\u4ECB\u7ECD", "\u539F\u8457\u9605\u8BFB" };
        Button[] tabBtns = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject tb = CreateButton("Tab_" + i, tabContainer.transform, tabNames[i], 24,
                Vector2.zero, new Vector2(200, 50), new Color(0.3f, 0.3f, 0.35f, 0.9f), font);
            tabBtns[i] = tb.GetComponent<Button>();
        }

        // Content Area
        GameObject contentArea = CreateGO("ContentArea", introPanel.transform, true);
        RectTransform caRect = contentArea.GetComponent<RectTransform>();
        caRect.anchorMin = new Vector2(0.05f, 0.05f); caRect.anchorMax = new Vector2(0.95f, 0.8f);
        caRect.offsetMin = Vector2.zero; caRect.offsetMax = Vector2.zero;

        // --- Tab 0: Story Background ---
        GameObject bgContent = CreateGO("Content_StoryBg", contentArea.transform, true);
        bgContent.AddComponent<CanvasGroup>();

        GameObject bgScroll = CreateScrollView(bgContent.transform, "ScrollArea");
        Transform bgScrollContent = bgScroll.transform.Find("Viewport/Content");
        VerticalLayoutGroup bgVlg = bgScrollContent.gameObject.AddComponent<VerticalLayoutGroup>();
        bgVlg.spacing = 20; bgVlg.padding = new RectOffset(30, 30, 20, 20);
        bgVlg.childForceExpandWidth = true; bgVlg.childForceExpandHeight = false;
        bgVlg.childControlWidth = true; bgVlg.childControlHeight = false;
        ContentSizeFitter bgCsf = bgScrollContent.gameObject.AddComponent<ContentSizeFitter>();
        bgCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateImagePlaceholder("\u897F\u6E38\u8BB0\u80CC\u666F\u56FE\u7247", bgScrollContent, new Vector2(800, 300), font);

        string bgText = "\u300A\u897F\u6E38\u8BB0\u300B\u662F\u4E2D\u56FD\u53E4\u4EE3\u7B2C\u4E00\u90E8\u6D6A\u6F2B\u4E3B\u4E49\u7AE0\u56DE\u4F53\u957F\u7BC7\u795E\u9B54\u5C0F\u8BF4\uFF0C\u6210\u4E66\u4E8E16\u4E16\u7EAA\u660E\u671D\u4E2D\u53F6\u3002\u4F5C\u8005\u5434\u627F\u6069\uFF0C\u5B57\u6C5D\u5FE0\uFF0C\u53F7\u5C04\u9633\u5C71\u4EBA\uFF0C\u660E\u4EE3\u6587\u5B66\u5BB6\u3002\n\n\u5168\u4E66\u4EE5\u5510\u50E7\u5E08\u5F92\u897F\u5929\u53D6\u7ECF\u4E3A\u4E3B\u7EBF\uFF0C\u8BB2\u8FF0\u4E86\u5510\u50E7\u5728\u5B59\u609F\u7A7A\u3001\u732A\u516B\u6212\u3001\u6C99\u50E7\u7684\u4FDD\u62A4\u4E0B\uFF0C\u5386\u7ECF\u4E5D\u4E5D\u516B\u5341\u4E00\u96BE\uFF0C\u964D\u5996\u4F0F\u9B54\uFF0C\u6700\u7EC8\u5230\u8FBE\u897F\u5929\u53D6\u5F97\u771F\u7ECF\u7684\u6545\u4E8B\u3002\n\n\u300A\u897F\u6E38\u8BB0\u300B\u662F\u4E2D\u56FD\u53E4\u5178\u795E\u9B54\u5C0F\u8BF4\u7684\u5DC5\u5CF0\u4E4B\u4F5C\uFF0C\u4E0E\u300A\u4E09\u56FD\u6F14\u4E49\u300B\u300A\u6C34\u6D52\u4F20\u300B\u300A\u7EA2\u697C\u68A6\u300B\u5E76\u79F0\u4E2D\u56FD\u53E4\u5178\u56DB\u5927\u540D\u8457\u3002\u5176\u4E2D\u201C\u4E09\u6253\u767D\u9AA8\u7CBE\u201D\u662F\u6700\u4E3A\u7ECF\u5178\u7684\u7AE0\u8282\u4E4B\u4E00\uFF0C\u8BB2\u8FF0\u4E86\u5B59\u609F\u7A7A\u706B\u773C\u91D1\u775B\u8BC6\u7834\u5996\u90AA\u4F2A\u88C5\u7684\u6545\u4E8B\u3002";
        CreateScrollText(bgScrollContent, bgText, 24, new Color(0.9f, 0.88f, 0.82f), font);

        // --- Tab 1: Character Introduction ---
        GameObject charContent = CreateGO("Content_Characters", contentArea.transform, true);
        charContent.AddComponent<CanvasGroup>();
        charContent.SetActive(false);

        GameObject charScroll = CreateScrollView(charContent.transform, "ScrollArea");
        Transform charScrollContent = charScroll.transform.Find("Viewport/Content");
        VerticalLayoutGroup charVlg = charScrollContent.gameObject.AddComponent<VerticalLayoutGroup>();
        charVlg.spacing = 30; charVlg.padding = new RectOffset(30, 30, 20, 20);
        charVlg.childForceExpandWidth = true; charVlg.childForceExpandHeight = false;
        charVlg.childControlWidth = true; charVlg.childControlHeight = false;
        ContentSizeFitter charCsf = charScrollContent.gameObject.AddComponent<ContentSizeFitter>();
        charCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        string[,] charData = {
            { "\u5B59\u609F\u7A7A", "\u9F50\u5929\u5927\u5723\uFF0C\u5510\u50E7\u7684\u5927\u5F92\u5F1F\u3002\u706B\u773C\u91D1\u775B\uFF0C\u80FD\u8BC6\u7834\u4E00\u5207\u5996\u90AA\u3002\u6027\u683C\u684E\u9A9C\u4E0D\u9A6F\u5374\u5FE0\u5FC3\u803F\u803F\uFF0C\u5728\u4E09\u6253\u767D\u9AA8\u7CBE\u4E2D\u4E09\u6B21\u8BC6\u7834\u767D\u9AA8\u7CBE\u53D8\u5316\uFF0C\u5374\u88AB\u5510\u50E7\u8BEF\u89E3\u9A71\u9010\u3002\u4ED6\u7684\u5FE0\u8BDA\u4E0E\u59D4\u5C48\u6784\u6210\u4E86\u8FD9\u6BB5\u6545\u4E8B\u6700\u52A8\u4EBA\u7684\u90E8\u5206\u3002" },
            { "\u5510\u50E7", "\u5510\u4E09\u85CF\uFF0C\u53D6\u7ECF\u56E2\u961F\u7684\u6838\u5FC3\u3002\u8654\u8BDA\u7684\u4F5B\u6559\u5F92\uFF0C\u5FC3\u6000\u6148\u60B2\uFF0C\u8089\u773C\u51E1\u80CE\u65E0\u6CD5\u8FA8\u8BC6\u5996\u602A\u3002\u6027\u683C\u6148\u60B2\u4E3A\u6000\u5374\u8FC7\u4E8E\u8FC2\u8150\uFF0C\u5BB9\u6613\u53D7\u5230\u6311\u62E8\u3002\u7D27\u7B8D\u5492\u662F\u4ED6\u7EA6\u675F\u609F\u7A7A\u7684\u624B\u6BB5\uFF0C\u4E5F\u662F\u5E08\u5F92\u77DB\u76FE\u7684\u96C6\u4E2D\u4F53\u73B0\u3002" },
            { "\u6C99\u50E7", "\u6C99\u609F\u51C0\uFF0C\u53D6\u7ECF\u56E2\u961F\u4E2D\u6700\u4E3A\u6C89\u7A33\u7684\u89D2\u8272\u3002\u5FE0\u539A\u8001\u5B9E\uFF0C\u9ED8\u9ED8\u627F\u62C5\u884C\u674E\u91CD\u4EFB\u3002\u5728\u4E09\u6253\u767D\u9AA8\u7CBE\u4E2D\u867D\u7740\u58A8\u4E0D\u591A\uFF0C\u4F46\u4ED6\u4F5C\u4E3A\u65C1\u89C2\u8005\uFF0C\u89C1\u8BC1\u4E86\u5E08\u5F92\u95F4\u7684\u4FE1\u4EFB\u5371\u673A\u3002" },
            { "\u767D\u9AA8\u7CBE", "\u767D\u9AA8\u592B\u4EBA\uFF0C\u4E09\u6253\u767D\u9AA8\u7CBE\u4E2D\u7684\u6838\u5FC3\u5996\u602A\u3002\u72E1\u733E\u591A\u7AEF\uFF0C\u5584\u4E8E\u53D8\u5316\u4F2A\u88C5\u3002\u5148\u540E\u53D8\u5316\u4E3A\u7F8E\u8C8C\u6751\u59D1\u3001\u5BFB\u5973\u8001\u5987\u548C\u5BFB\u5973\u8001\u7FC1\uFF0C\u5229\u7528\u4EBA\u6027\u7684\u5F31\u70B9\u6765\u79BB\u95F4\u5E08\u5F92\u5173\u7CFB\u3002\u5979\u5E76\u975E\u6CD5\u529B\u6700\u5F3A\u7684\u5996\u602A\uFF0C\u5374\u662F\u6700\u5584\u4E8E\u5229\u7528\u4EBA\u5FC3\u7684\u5996\u602A\u3002" }
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject card = CreateGO("CharCard_" + i, charScrollContent, false);
            Image cardImg = card.AddComponent<Image>();
            cardImg.color = new Color(0.15f, 0.15f, 0.2f, 0.85f);
            LayoutElement cardLE = card.AddComponent<LayoutElement>();
            cardLE.preferredHeight = 220;
            HorizontalLayoutGroup cardHlg = card.AddComponent<HorizontalLayoutGroup>();
            cardHlg.spacing = 20; cardHlg.padding = new RectOffset(20, 20, 15, 15);
            cardHlg.childForceExpandWidth = false; cardHlg.childForceExpandHeight = true;
            cardHlg.childControlWidth = false; cardHlg.childControlHeight = true;

            CreateImagePlaceholder("\u4EBA\u7269\u7ACB\u7ED8", card.transform, new Vector2(160, 180), font)
                .AddComponent<LayoutElement>().preferredWidth = 160;

            GameObject textArea = CreateGO("TextArea", card.transform, false);
            LayoutElement taLE = textArea.AddComponent<LayoutElement>();
            taLE.flexibleWidth = 1;
            VerticalLayoutGroup taVlg = textArea.AddComponent<VerticalLayoutGroup>();
            taVlg.spacing = 8; taVlg.childForceExpandWidth = true; taVlg.childForceExpandHeight = false;
            taVlg.childControlWidth = true; taVlg.childControlHeight = false;

            GameObject nameObj = CreateText("Name", textArea.transform, charData[i, 0], 34,
                new Color(0.95f, 0.85f, 0.5f), TextAnchor.MiddleLeft, font);
            nameObj.AddComponent<LayoutElement>().preferredHeight = 45;

            GameObject descObj = CreateText("Desc", textArea.transform, charData[i, 1], 22,
                new Color(0.9f, 0.88f, 0.82f), TextAnchor.UpperLeft, font);
            LayoutElement descLE = descObj.AddComponent<LayoutElement>();
            descLE.preferredHeight = 150;
        }

        // --- Tab 2: Book Reading ---
        GameObject readContent = CreateGO("Content_BookReading", contentArea.transform, true);
        readContent.AddComponent<CanvasGroup>();
        readContent.SetActive(false);

        CreateText("Instruction", readContent.transform,
            "\u539F\u8457\u9605\u8BFB - \u7FFB\u9875\u6D4F\u89C8\u4E09\u6253\u767D\u9AA8\u7CBE\u539F\u6587", 28,
            new Color(0.9f, 0.88f, 0.82f, 0.8f), TextAnchor.MiddleCenter, font)
            .GetComponent<RectTransform>().SetAnchoredRect(new Vector2(0f, 0.85f), new Vector2(1f, 1f));

        GameObject bookArea = CreateImage("BookArea", readContent.transform, new Color(0.12f, 0.1f, 0.08f, 0.9f), false);
        bookArea.GetComponent<RectTransform>().SetAnchoredRect(new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.82f));

        GameObject pageTextObj = CreateText("PageText", bookArea.transform, "", 24,
            new Color(0.2f, 0.15f, 0.1f), TextAnchor.UpperLeft, font);
        RectTransform ptRect = pageTextObj.GetComponent<RectTransform>();
        ptRect.anchorMin = new Vector2(0, 0); ptRect.anchorMax = new Vector2(1, 1);
        ptRect.offsetMin = new Vector2(40, 40); ptRect.offsetMax = new Vector2(-40, -40);
        Text pageText = pageTextObj.GetComponent<Text>();
        pageText.lineSpacing = 1.5f;

        GameObject navContainer = CreateGO("NavButtons", readContent.transform, false);
        navContainer.GetComponent<RectTransform>().SetAnchoredRect(new Vector2(0.3f, 0f), new Vector2(0.7f, 0.08f));
        HorizontalLayoutGroup navHlg = navContainer.AddComponent<HorizontalLayoutGroup>();
        navHlg.spacing = 40; navHlg.childAlignment = TextAnchor.MiddleCenter;
        navHlg.childForceExpandWidth = false; navHlg.childForceExpandHeight = true;

        GameObject prevBtn = CreateButton("PrevBtn", navContainer.transform,
            "\u4E0A\u4E00\u9875", 24, Vector2.zero, new Vector2(140, 45),
            new Color(0.35f, 0.35f, 0.4f), font);

        GameObject pageIndObj = CreateText("PageIndicator", navContainer.transform, "1 / 5", 26,
            new Color(0.9f, 0.88f, 0.82f), TextAnchor.MiddleCenter, font);
        pageIndObj.AddComponent<LayoutElement>().preferredWidth = 120;

        GameObject nextBtn = CreateButton("NextBtn", navContainer.transform,
            "\u4E0B\u4E00\u9875", 24, Vector2.zero, new Vector2(140, 45),
            new Color(0.35f, 0.35f, 0.4f), font);

        BookReaderController bookCtrl = readContent.AddComponent<BookReaderController>();
        bookCtrl.bookArea = bookArea.GetComponent<RectTransform>();
        bookCtrl.pageIndicator = pageIndObj.GetComponent<Text>();
        bookCtrl.bookProPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Book-Page Curl Pro/Prefabs/BookPro.prefab");
        prevBtn.GetComponent<Button>().onClick.AddListener(() => bookCtrl.PreviousPage());
        nextBtn.GetComponent<Button>().onClick.AddListener(() => bookCtrl.NextPage());

        // Wire IntroductionController
        IntroductionController introCtrl = introPanel.AddComponent<IntroductionController>();
        introCtrl.tabButtons = tabBtns;
        introCtrl.contentPanels = new GameObject[] { bgContent, charContent, readContent };

        // ========== EXPERIENCE SELECT PANEL ==========
        GameObject expPanel = CreateGO("ExperienceSelectPanel", rootPanel.transform, true);
        expPanel.AddComponent<CanvasGroup>();
        expPanel.SetActive(false);

        CreateText("ExpTitle", expPanel.transform,
            "\u9009\u62E9\u89D2\u8272 \u00B7 \u5F00\u542F\u6C89\u6D78\u4F53\u9A8C", 48,
            new Color(0.95f, 0.85f, 0.5f), TextAnchor.MiddleCenter, font)
            .GetComponent<RectTransform>().SetAnchoredRect(new Vector2(0f, 0.85f), new Vector2(1f, 1f));

        CreateText("ExpSubtitle", expPanel.transform,
            "\u5DE6\u53F3\u6ED1\u52A8\u9009\u62E9\u89D2\u8272\uFF0C\u4ECE\u4E0D\u540C\u89C6\u89D2\u4F53\u9A8C\u4E09\u6253\u767D\u9AA8\u7CBE", 24,
            new Color(0.9f, 0.88f, 0.82f, 0.7f), TextAnchor.MiddleCenter, font)
            .GetComponent<RectTransform>().SetAnchoredRect(new Vector2(0f, 0.78f), new Vector2(1f, 0.85f));

        GameObject selectorArea = CreateGO("SelectorArea", expPanel.transform, false);
        selectorArea.GetComponent<RectTransform>().SetAnchoredRect(new Vector2(0.1f, 0.15f), new Vector2(0.9f, 0.78f));
        Image maskImg = selectorArea.AddComponent<Image>();
        maskImg.color = new Color(0, 0, 0, 0);
        selectorArea.AddComponent<Mask>().showMaskGraphic = false;

        GameObject scrollContentObj = CreateGO("ScrollContent", selectorArea.transform, false);
        RectTransform scRect = scrollContentObj.GetComponent<RectTransform>();
        scRect.anchorMin = new Vector2(0, 0); scRect.anchorMax = new Vector2(1, 1);
        scRect.pivot = new Vector2(0.5f, 0.5f);

        string[] charNames = { "\u5B59\u609F\u7A7A", "\u5510\u50E7", "\u767D\u9AA8\u7CBE" };
        Color[] charSelColors = {
            new Color(0.85f, 0.55f, 0.15f),
            new Color(0.55f, 0.35f, 0.65f),
            new Color(0.45f, 0.7f, 0.5f)
        };
        string[] charSelDescs = {
            "\u706B\u773C\u91D1\u775B\u7684\u9F50\u5929\u5927\u5723\n\u4ECE\u609F\u7A7A\u89C6\u89D2\u63ED\u9732\u5996\u90AA\u771F\u76F8",
            "\u6148\u60B2\u4E3A\u6000\u7684\u53D6\u7ECF\u50E7\u4EBA\n\u611F\u53D7\u5510\u50E7\u7684\u56F0\u60D1\u4E0E\u65E0\u5948",
            "\u5584\u4E8E\u53D8\u5316\u7684\u767D\u9AA8\u592B\u4EBA\n\u7AA5\u63A2\u5996\u602A\u7684\u8BE1\u8BA1\u4E0E\u5FC3\u601D"
        };

        GameObject[] charCards = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            float xPos = (i - 1) * 400;
            GameObject card = CreateGO("CharSelect_" + i, scrollContentObj.transform, false);
            Image cardImg = card.AddComponent<Image>();
            cardImg.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchoredPosition = new Vector2(xPos, 0);
            cardRect.sizeDelta = new Vector2(320, 420);
            charCards[i] = card;

            GameObject portrait = CreateImage("Portrait", card.transform,
                new Color(charSelColors[i].r, charSelColors[i].g, charSelColors[i].b, 0.6f), false);
            portrait.GetComponent<RectTransform>().SetAnchoredPos(new Vector2(0, 60), new Vector2(200, 200));
            CreateText("PortraitLabel", portrait.transform, "[\u4EBA\u7269\u5934\u50CF]", 24,
                new Color(1f, 1f, 1f, 0.5f), TextAnchor.MiddleCenter, font)
                .GetComponent<RectTransform>().StretchFull();

            CreateText("Name", card.transform, charNames[i], 36, charSelColors[i], TextAnchor.MiddleCenter, font)
                .GetComponent<RectTransform>().SetAnchoredPos(new Vector2(0, -80), new Vector2(300, 50));

            CreateText("Desc", card.transform, charSelDescs[i], 20,
                new Color(0.9f, 0.88f, 0.82f, 0.8f), TextAnchor.MiddleCenter, font)
                .GetComponent<RectTransform>().SetAnchoredPos(new Vector2(0, -145), new Vector2(300, 60));

            CreateButton("SelectBtn", card.transform,
                "\u5F00\u59CB\u4F53\u9A8C", 24, new Vector2(0, -175), new Vector2(200, 50),
                charSelColors[i], font);
        }

        // Indicator dots
        GameObject dotsContainer = CreateGO("DotsContainer", expPanel.transform, false);
        dotsContainer.GetComponent<RectTransform>().SetAnchoredRect(new Vector2(0.4f, 0.08f), new Vector2(0.6f, 0.14f));
        HorizontalLayoutGroup dotsHlg = dotsContainer.AddComponent<HorizontalLayoutGroup>();
        dotsHlg.spacing = 15; dotsHlg.childAlignment = TextAnchor.MiddleCenter;
        dotsHlg.childForceExpandWidth = false; dotsHlg.childForceExpandHeight = false;

        GameObject[] dots = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject dot = CreateGO("Dot_" + i, dotsContainer.transform, false);
            Image dotImg = dot.AddComponent<Image>();
            dotImg.color = i == 1 ? new Color(0.95f, 0.85f, 0.5f) : new Color(0.5f, 0.5f, 0.5f, 0.5f);
            dot.GetComponent<RectTransform>().sizeDelta = new Vector2(16, 16);
            LayoutElement dotLE = dot.AddComponent<LayoutElement>();
            dotLE.preferredWidth = 16; dotLE.preferredHeight = 16;
            dots[i] = dot;
        }

        ExperienceSelectController expCtrl = expPanel.AddComponent<ExperienceSelectController>();
        expCtrl.scrollContent = scrollContentObj.GetComponent<RectTransform>();
        expCtrl.characterCards = charCards;
        expCtrl.indicatorDots = dots;

        // ========== BACK BUTTON ==========
        GameObject backBtnObj = CreateButton("BackButton", canvasObj.transform,
            "\u2190 \u8FD4\u56DE", 26, Vector2.zero, new Vector2(120, 50),
            new Color(0.3f, 0.3f, 0.35f, 0.8f), font);
        RectTransform bbRect = backBtnObj.GetComponent<RectTransform>();
        bbRect.anchorMin = new Vector2(0, 1); bbRect.anchorMax = new Vector2(0, 1);
        bbRect.pivot = new Vector2(0, 1);
        bbRect.anchoredPosition = new Vector2(30, -30);
        backBtnObj.SetActive(false);
        backBtnObj.GetComponent<Button>().onClick.AddListener(() => {
            if (StartSceneManager.Instance != null) StartSceneManager.Instance.GoBack();
        });

        // ========== Wire StartSceneManager ==========
        StartSceneManager mgr = rootGO.AddComponent<StartSceneManager>();
        mgr.coverPanel = coverPanel;
        mgr.mainMenuPanel = mainMenuPanel;
        mgr.introductionPanel = introPanel;
        mgr.experienceSelectPanel = expPanel;
        mgr.backButton = backBtnObj;

        // Save
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("Start scene UI built successfully!");
    }

    [MenuItem("XiYouJi/Finish Start Scene Setup")]
    public static void FinishStartSceneSetup()
    {
        GameObject mainMenuPanel = FindSceneObject("MainMenuPanel");
        GameObject introButton = FindSceneObject("ModuleBtn_0");
        GameObject experienceButton = FindSceneObject("ModuleBtn_1");
        GameObject projectButton = FindSceneObject("ModuleBtn_2");
        GameObject introductionPanel = FindSceneObject("IntroductionPanel");
        GameObject tab0 = FindSceneObject("Tab_0");
        GameObject tab1 = FindSceneObject("Tab_1");
        GameObject tab2 = FindSceneObject("Tab_2");
        GameObject storyContent = FindSceneObject("Content_StoryBg");
        GameObject charactersContent = FindSceneObject("Content_Characters");
        GameObject bookReading = FindSceneObject("Content_BookReading");
        GameObject bookArea = FindSceneObject("BookArea");
        GameObject pageIndicator = FindSceneObject("PageIndicator");

        if (mainMenuPanel == null || introButton == null || experienceButton == null ||
            projectButton == null || introductionPanel == null || tab0 == null ||
            tab1 == null || tab2 == null || storyContent == null ||
            charactersContent == null || bookReading == null || bookArea == null ||
            pageIndicator == null)
        {
            Debug.LogError("Finish Start Scene Setup: required Start scene objects were not found.");
            return;
        }

        ModuleMenuController menuController = mainMenuPanel.GetComponent<ModuleMenuController>();
        if (menuController == null)
            menuController = Undo.AddComponent<ModuleMenuController>(mainMenuPanel);

        menuController.introButton = introButton;
        menuController.experienceButton = experienceButton;
        menuController.projectButton = projectButton;
        EditorUtility.SetDirty(menuController);

        IntroductionController introductionController =
            introductionPanel.GetComponent<IntroductionController>();
        if (introductionController == null)
        {
            Debug.LogError("Finish Start Scene Setup: IntroductionController was not found.");
            return;
        }

        introductionController.tabButtons = new Button[]
        {
            tab0.GetComponent<Button>(),
            tab1.GetComponent<Button>(),
            tab2.GetComponent<Button>()
        };
        introductionController.contentPanels = new GameObject[]
        {
            storyContent,
            charactersContent,
            bookReading
        };
        EditorUtility.SetDirty(introductionController);

        BookReaderController bookController = bookReading.GetComponent<BookReaderController>();
        if (bookController == null)
        {
            Debug.LogError("Finish Start Scene Setup: BookReaderController was not found.");
            return;
        }

        bookController.bookProPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Book-Page Curl Pro/Prefabs/BookPro.prefab");
        bookController.bookArea = bookArea.GetComponent<RectTransform>();
        bookController.pageIndicator = pageIndicator.GetComponent<Text>();
        EditorUtility.SetDirty(bookController);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Start scene remaining setup completed successfully.");
    }

    static GameObject FindSceneObject(string objectName)
    {
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in objects)
        {
            if (obj.name == objectName && obj.scene.IsValid())
                return obj;
        }

        return null;
    }

    // ===== Helper Methods =====

    static GameObject CreateGO(string name, Transform parent, bool stretch)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        if (stretch) obj.GetComponent<RectTransform>().StretchFull();
        return obj;
    }

    static GameObject CreateImage(string name, Transform parent, Color color, bool stretch)
    {
        GameObject obj = CreateGO(name, parent, stretch);
        Image img = obj.AddComponent<Image>();
        img.color = color;
        if (!stretch)
        {
            obj.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
            obj.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
        }
        return obj;
    }

    static GameObject CreateText(string name, Transform parent, string text, int fontSize, Color color, TextAnchor align, Font font)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
        obj.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
        Text t = obj.AddComponent<Text>();
        t.text = text;
        t.font = font;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        return obj;
    }

    static GameObject CreateButtonGO(string name, Transform parent, Vector2 pos, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        Image img = obj.AddComponent<Image>();
        img.color = color;
        Button btn = obj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = color * 1.2f;
        cb.pressedColor = color * 0.8f;
        btn.colors = cb;
        return obj;
    }

    static GameObject CreateButton(string name, Transform parent, string label, int fontSize, Vector2 pos, Vector2 size, Color color, Font font)
    {
        GameObject btnObj = CreateButtonGO(name, parent, pos, size, color);
        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(btnObj.transform, false);
        textObj.GetComponent<RectTransform>().StretchFull();
        Text t = textObj.AddComponent<Text>();
        t.text = label;
        t.font = font;
        t.fontSize = fontSize;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        return btnObj;
    }

    static GameObject CreateImagePlaceholder(string label, Transform parent, Vector2 size, Font font)
    {
        GameObject obj = new GameObject(label + "_Placeholder", typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);
        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = size.y;
        le.preferredWidth = size.x;

        GameObject labelObj = new GameObject("Label", typeof(RectTransform));
        labelObj.transform.SetParent(obj.transform, false);
        labelObj.GetComponent<RectTransform>().StretchFull();
        Text t = labelObj.AddComponent<Text>();
        t.text = "[\u8BF7\u66FF\u6362: " + label + "]";
        t.font = font;
        t.fontSize = 22;
        t.color = new Color(1f, 1f, 1f, 0.5f);
        t.alignment = TextAnchor.MiddleCenter;
        return obj;
    }

    static GameObject CreateScrollText(Transform parent, string text, int fontSize, Color color, Font font)
    {
        GameObject obj = new GameObject("StoryText", typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        Text t = obj.AddComponent<Text>();
        t.text = text;
        t.font = font;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = TextAnchor.UpperLeft;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.lineSpacing = 1.5f;
        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = 400;
        return obj;
    }

    static GameObject CreateScrollView(Transform parent, string name)
    {
        GameObject sv = new GameObject(name, typeof(RectTransform));
        sv.transform.SetParent(parent, false);
        sv.GetComponent<RectTransform>().StretchFull();
        Image svImg = sv.AddComponent<Image>();
        svImg.color = new Color(0, 0, 0, 0);
        svImg.raycastTarget = false;
        ScrollRect sr = sv.AddComponent<ScrollRect>();
        sr.horizontal = false; sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Elastic;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(sv.transform, false);
        viewport.GetComponent<RectTransform>().StretchFull();
        Image vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(0, 0, 0, 0);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        GameObject contentObj = new GameObject("Content", typeof(RectTransform));
        contentObj.transform.SetParent(viewport.transform, false);
        RectTransform cRect = contentObj.GetComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0, 1); cRect.anchorMax = new Vector2(1, 1);
        cRect.pivot = new Vector2(0.5f, 1);
        cRect.anchoredPosition = Vector2.zero;
        cRect.sizeDelta = new Vector2(0, 0);

        sr.content = cRect;
        sr.viewport = viewport.GetComponent<RectTransform>();
        return sv;
    }
}

// Extension methods for RectTransform
public static class SetupRectExtensions
{
    public static void SetAnchoredPos(this RectTransform rect, Vector2 pos, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
    }

    public static void SetAnchoredRect(this RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
