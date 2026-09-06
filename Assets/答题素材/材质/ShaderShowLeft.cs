using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ShaderShowLeft : MonoBehaviour
{
    public Image image;
    public float duration = 1.5f;
    private Material mat;

    void Awake()
    {
        mat = image.material;
        // 初始：全部隐藏 _Cutoff=1
        mat.SetFloat("_Cutoff", 1);
    }

    //【出现：从左向右慢慢显示】 Cutoff：1 → 0
    public void PlayShowAnim()
    {
        StopAllCoroutines();
        StartCoroutine(ShowCoroutine());
    }

    IEnumerator ShowCoroutine()
    {
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            float val = Mathf.Lerp(1f, 0f, Mathf.Clamp01(t / duration));
            mat.SetFloat("_Cutoff", val);
            yield return null;
        }
        mat.SetFloat("_Cutoff", 0);
    }

    ////【消失：从右向左收回隐藏】 Cutoff：0 → 1
    //public void PlayHideAnim()
    //{
    //    StopAllCoroutines();
    //    StartCoroutine(HideCoroutine());
    //}

    //IEnumerator HideCoroutine()
    //{
    //    float t = 0;
    //    while (t < duration)
    //    {
    //        t += Time.deltaTime;
    //        float val = Mathf.Lerp(0f, 1f, Mathf.Clamp01(t / duration));
    //        mat.SetFloat("_Cutoff", val);
    //        yield return null;
    //    }
    //    mat.SetFloat("_Cutoff", 1);
    //}
}