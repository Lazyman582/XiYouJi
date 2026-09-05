using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CoverController : MonoBehaviour
{
    public GameObject titleText;
    public GameObject subtitleText;
    public GameObject enterButton;

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
    }
}