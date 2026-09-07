using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CoverController : MonoBehaviour
{
    public GameObject titleText;
    public GameObject subtitleText;
    public GameObject enterButton;
    public GameObject[] menuButtons;

    private void Start()
    {
        if (enterButton != null)
        {
            Button btn = enterButton.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => {
                if (StartSceneManager.Instance != null) StartSceneManager.Instance.ShowMainMenu();
            });
        }
    }

    private void OnEnable()
    {
        PlayEnterAnimation();
    }

    public void PlayEnterAnimation()
    {
        if (titleText != null)
        {
            Vector3 origPos = titleText.transform.localPosition;
            titleText.transform.localPosition = origPos + new Vector3(0, 50, 0);
            titleText.transform.DOLocalMoveY(origPos.y, 0.8f).SetEase(Ease.OutBack);
            CanvasGroup titleCg = titleText.GetComponent<CanvasGroup>();
            if (titleCg == null) titleCg = titleText.AddComponent<CanvasGroup>();
            titleCg.alpha = 0;
            titleCg.DOFade(1f, 0.6f).SetDelay(0.2f);
        }

        if (subtitleText != null)
        {
            CanvasGroup subCg = subtitleText.GetComponent<CanvasGroup>();
            if (subCg == null) subCg = subtitleText.AddComponent<CanvasGroup>();
            subCg.alpha = 0;
            subCg.DOFade(1f, 0.6f).SetDelay(0.5f);
        }

        if (enterButton != null)
        {
            CanvasGroup btnCg = enterButton.GetComponent<CanvasGroup>();
            if (btnCg == null) btnCg = enterButton.AddComponent<CanvasGroup>();
            btnCg.alpha = 0;
            btnCg.DOFade(1f, 0.5f).SetDelay(0.8f);
            enterButton.transform.localScale = Vector3.zero;
            enterButton.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetDelay(0.8f);
        }

        // 设置 / 制作名单 / 退出 三个按钮：依次弹出，跟随进入体验按钮之后
        if (menuButtons != null)
        {
            for (int i = 0; i < menuButtons.Length; i++)
            {
                GameObject menuBtn = menuButtons[i];
                if (menuBtn == null) continue;
                CanvasGroup menuCg = menuBtn.GetComponent<CanvasGroup>();
                if (menuCg == null) menuCg = menuBtn.AddComponent<CanvasGroup>();
                float delay = 0.95f + 0.12f * i;
                menuCg.alpha = 0;
                menuCg.DOFade(1f, 0.5f).SetDelay(delay);
                menuBtn.transform.localScale = Vector3.zero;
                menuBtn.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetDelay(delay);
            }
        }
    }
}