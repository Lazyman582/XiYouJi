using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneLoadButton : MonoBehaviour
{
    [Header("目标场景")]
    public string sceneName;

    [Header("要传递的参数（键值对）")]
    public string paramKey;
    public int paramValue;

    // 按钮 OnClick 绑定此方法
    public void LoadSceneWithParam()
    {
        // 1. 存参
        SceneTransitionData.SetParam(paramKey, paramValue);
        Debug.LogError(paramKey);
     
        // 2. 加载场景（使用已有的 SceneLoader）
        SceneLoader loader = FindObjectOfType<SceneLoader>();
        if (loader != null)
        {
         
            loader.LoadScene(sceneName);
        }
    }
}