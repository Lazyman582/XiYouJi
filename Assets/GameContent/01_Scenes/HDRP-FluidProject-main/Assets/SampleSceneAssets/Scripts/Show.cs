using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Show : MonoBehaviour
{
    public float delay = 2f;   // 延迟秒数
    
    void Start()
    {
        // 启动协程，等待 delay 秒后调用显示
       
    }

    public void show(GameObject game) {

        StartCoroutine(ShowAfter(game));

    }
    public void hide(GameObject game)
    {

        StartCoroutine(HideAfter(game));

    }
    IEnumerator ShowAfter(GameObject game)
    {
        yield return new WaitForSeconds(delay);
        game.SetActive(true);  // 显示物体
    }
    IEnumerator HideAfter(GameObject game)
    {
        yield return new WaitForSeconds(delay);
        game.SetActive(false);  // 显示物体
    }
}