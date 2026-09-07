using UnityEngine;
using UnityEngine.UI;

public class UISwitcher : MonoBehaviour
{
    [Header("主菜单界面")]
    public GameObject panelNewUI;
    [Header("答题界面根节点")]
    public GameObject panelQuizRoot;

    [Header("按钮引用")]
    public Button btnGoQuiz;
    public Button btnBack;

    [Header("答题管理器")]
    public QuizManager quizManager;

    void Awake()
    {
        BindButtonEvents();
    }

    void Start()
    {
        // Start is kept as a fallback for projects using custom Enter Play Mode
        // options. BindButtonEvents is idempotent, so this cannot double-register.
        BindButtonEvents();
    }

    void BindButtonEvents()
    {
        if (btnGoQuiz != null)
        {
            btnGoQuiz.onClick.RemoveListener(OnGoQuiz);
            btnGoQuiz.onClick.AddListener(OnGoQuiz);
        }

        BindBackButtonEvent();
    }

    void BindBackButtonEvent()
    {
        if (btnBack != null)
        {
            btnBack.onClick.RemoveListener(OnBackToNewUI);
            btnBack.onClick.AddListener(OnBackToNewUI);
        }
    }

    void OnDestroy()
    {
        if (btnGoQuiz != null)
            btnGoQuiz.onClick.RemoveListener(OnGoQuiz);

        if (btnBack != null)
            btnBack.onClick.RemoveListener(OnBackToNewUI);
    }

    /// <summary>从主菜单直接进入答题界面。</summary>
    void OnGoQuiz()
    {
        if (panelNewUI != null)
            panelNewUI.SetActive(false);

        if (panelQuizRoot != null)
            panelQuizRoot.SetActive(true);

        // 跳过已删除的“背景”页，直接显示“答题界面/答题”。
        if (quizManager != null)
            quizManager.OnClickEnterQuiz();
        else
            Debug.LogError("UISwitcher: QuizManager is not assigned.", this);

        // btnBack belongs to the initially inactive quiz hierarchy. Rebind after
        // that hierarchy is activated so Unity cannot discard the runtime call.
        BindBackButtonEvent();
    }

    /// <summary>返回主菜单并清空本次答题状态。</summary>
    void OnBackToNewUI()
    {
        // 先复位再关闭，避免动画或协程把下次进入时的界面状态改乱。
        if (quizManager != null)
            quizManager.ResetQuizState();

        if (panelQuizRoot != null)
            panelQuizRoot.SetActive(false);

        if (panelNewUI != null)
            panelNewUI.SetActive(true);
    }
}
