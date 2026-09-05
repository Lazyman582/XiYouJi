using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class IntroductionController : MonoBehaviour
{
    public Button[] tabButtons;
    public GameObject[] contentPanels;
    private int currentTab = -1;

    private Color activeTabColor = new Color(0.7f, 0.5f, 0.2f, 0.95f);
    private Color inactiveTabColor = new Color(0.3f, 0.3f, 0.35f, 0.9f);

    private void Start()
    {
        if (tabButtons != null)
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int idx = i;
                if (tabButtons[i] != null)
                    tabButtons[i].onClick.AddListener(() => SwitchTab(idx));
            }
        }
        SwitchTab(0);
    }

    public void SwitchTab(int index)
    {
        if (contentPanels == null || index < 0 || index >= contentPanels.Length) return;

        if (currentTab >= 0 && currentTab != index &&
            currentTab < contentPanels.Length && contentPanels[currentTab] != null)
        {
            GameObject oldContent = contentPanels[currentTab];
            CanvasGroup oldCg = oldContent.GetComponent<CanvasGroup>();
            if (oldCg == null) oldCg = oldContent.AddComponent<CanvasGroup>();
            oldCg.DOFade(0f, 0.2f).OnComplete(() => oldContent.SetActive(false));
        }

        if (tabButtons != null)
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] == null) continue;
                Image tabImg = tabButtons[i].GetComponent<Image>();
                if (tabImg == null) continue;
                if (i == index)
                {
                    tabImg.DOColor(activeTabColor, 0.3f);
                    tabButtons[i].transform.DOScale(new Vector3(1.05f, 1.05f, 1f), 0.3f);
                }
                else
                {
                    tabImg.DOColor(inactiveTabColor, 0.3f);
                    tabButtons[i].transform.DOScale(Vector3.one, 0.3f);
                }
            }
        }

        currentTab = index;

        GameObject newContent = contentPanels[index];
        if (newContent != null)
        {
            newContent.SetActive(true);
            CanvasGroup newCg = newContent.GetComponent<CanvasGroup>();
            if (newCg == null) newCg = newContent.AddComponent<CanvasGroup>();
            newCg.alpha = 0f;
            newCg.DOFade(1f, 0.3f).SetDelay(0.15f);
        }
    }
}
