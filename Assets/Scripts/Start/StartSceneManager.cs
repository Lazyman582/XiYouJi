using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class StartSceneManager : MonoBehaviour
{
    public static StartSceneManager Instance { get; private set; }

    public GameObject coverPanel;
    public GameObject mainMenuPanel;
    public GameObject introductionPanel;
    public GameObject experienceSelectPanel;
    public GameObject settingsPanel;
    public GameObject creditsPanel;
    public GameObject backButton;

    private GameObject currentPanel;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }
    }

    private void Start() { ShowCover(); }

    public void ShowCover()
    {
        SwitchPanel(coverPanel);
        if (backButton != null) backButton.SetActive(false);
    }

    public void ShowMainMenu()
    {
        SwitchPanel(mainMenuPanel);
        if (backButton != null) backButton.SetActive(true);
    }

    public void ShowIntroduction()
    {
        SwitchPanel(introductionPanel);
        if (backButton != null) backButton.SetActive(true);
    }

    public void ShowExperienceSelect()
    {
        SwitchPanel(experienceSelectPanel);
        if (backButton != null) backButton.SetActive(true);
    }

    public void ShowSettings()
    {
        SwitchPanel(settingsPanel);
        // 设置界面自带“返回主界面”按钮，隐藏全局返回键避免逻辑冲突
        if (backButton != null) backButton.SetActive(false);
    }

    public void ShowCredits()
    {
        SwitchPanel(creditsPanel);
        if (backButton != null) backButton.SetActive(false);
    }

    // 退出游戏；编辑器下退出 Play 模式，方便开发期使用
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void GoBack()
    {
        if (currentPanel == introductionPanel || currentPanel == experienceSelectPanel)
            ShowMainMenu();
        else if (currentPanel == mainMenuPanel)
            ShowCover();
    }

    private void SwitchPanel(GameObject targetPanel)
    {
        if (currentPanel != null)
        {
            CanvasGroup oldCg = currentPanel.GetComponent<CanvasGroup>();
            if (oldCg == null) oldCg = currentPanel.AddComponent<CanvasGroup>();
            oldCg.interactable = false;
            oldCg.blocksRaycasts = false;
            oldCg.DOFade(0f, 0.3f).SetEase(Ease.InQuad);
            GameObject old = currentPanel;
            old.transform.DOScale(new Vector3(0.95f, 0.95f, 1f), 0.3f).SetEase(Ease.InQuad)
                .OnComplete(() => { old.SetActive(false); old.transform.localScale = Vector3.one; });
        }

        currentPanel = targetPanel;
        if (targetPanel != null)
        {
            targetPanel.SetActive(true);
            CanvasGroup newCg = targetPanel.GetComponent<CanvasGroup>();
            if (newCg == null) newCg = targetPanel.AddComponent<CanvasGroup>();
            newCg.alpha = 0f;
            newCg.interactable = true;
            newCg.blocksRaycasts = true;
            targetPanel.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
            newCg.DOFade(1f, 0.4f).SetEase(Ease.OutQuad).SetDelay(0.15f);
            targetPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetDelay(0.15f);
        }
    }
}
