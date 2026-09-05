using UnityEngine;

public class StartSceneEntry : MonoBehaviour
{
    private void Awake()
    {
        DG.Tweening.DOTween.Init();
        if (GetComponent<StartSceneManager>() == null)
            gameObject.AddComponent<StartSceneManager>();
    }
}
