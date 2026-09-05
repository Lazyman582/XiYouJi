using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SimpleFade : MonoBehaviour
{
    private Image image;

    void OnEnable()
    {
        if (image == null) image = GetComponent<Image>();
        StartCoroutine(FadeCoroutine());
    }

    IEnumerator FadeCoroutine()
    {
        Color c = image.color;
        float t = 0;

        // µ­³ö£¨±äºÚ£©
        while (t < 1f)
        {
            t += Time.deltaTime / 0.5f;
            c.a = t;
            image.color = c;
            yield return null;
        }

        // Í£Áô 0.3 Ãë
        yield return new WaitForSeconds(0.3f);

        // µ­Èë£¨»Ö¸´Í¸Ã÷£©
        t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime / 0.5f;
            c.a = t;
            image.color = c;
            yield return null;
        }

        gameObject.SetActive(false);
    }
}