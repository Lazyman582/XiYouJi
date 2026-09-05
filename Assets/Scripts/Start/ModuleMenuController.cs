using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ModuleMenuController : MonoBehaviour
{
    public GameObject introButton;
    public GameObject experienceButton;
    public GameObject projectButton;

    private void Start()
    {
        // The first two buttons use persistent scene events. Only the placeholder
        // project button is registered here so navigation is never invoked twice.
        if (projectButton != null)
        {
            Button btn = projectButton.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(ShowProjectPlaceholder);
        }
    }

    public void ShowProjectPlaceholder()
    {
        Debug.Log("项目策划模块 - 待实现");
    }

    private void OnEnable()
    {
        PlayEnterAnimation();
    }

    private void PlayEnterAnimation()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("ModuleBtn_"))
            {
                child.localScale = Vector3.zero;
                int index = 0;
                int.TryParse(child.name.Substring(child.name.IndexOf('_') + 1), out index);
                child.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetDelay(0.1f * index + 0.2f);
            }
        }
    }
}
