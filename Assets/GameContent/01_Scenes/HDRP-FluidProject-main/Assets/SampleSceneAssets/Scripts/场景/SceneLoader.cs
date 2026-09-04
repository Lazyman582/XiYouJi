using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.Timeline.AnimationPlayableAsset;


public static class SceneTransitionData
{
    private static Dictionary<string, object> _params = new Dictionary<string, object>();

    public static void SetParam(string key, object value)
    {
        _params[key] = value;
    }

    public static T GetParam<T>(string key, T defaultValue )
    {
        Debug.LogError(_params["playerID"]);
        if (_params.TryGetValue(key, out object val) && val is T tVal)
       
            return tVal;
        else
        return defaultValue;
    }

    public static bool HasParam(string key) => _params.ContainsKey(key);

    public static void ClearParam(string key)
    {
        if (_params.ContainsKey(key))
            _params.Remove(key);
    }

    public static void ClearAll()
    {
        _params.Clear();
    }
}
public class SceneLoader : MonoBehaviour
{
    [Header("加载设置")]
    [Tooltip("是否异步加载（推荐开启，防止卡顿）")]
    public bool useAsyncLoad = true;

    [Header("加载提示UI（可选）")]
    public GameObject loadingPanel; // 可拖拽一个加载中的转圈UI

    [Header("加载模式")]
    public LoadSceneMode loadMode = LoadSceneMode.Additive;

    // --------------------------------------------------
    // 1. 基础加载（无参）
    // --------------------------------------------------
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("【场景管理】场景名称为空！");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"【场景管理】场景 '{sceneName}' 不存在或未添加到 Build Settings！");
            return;
        }

        if (useAsyncLoad)
        {
            StartCoroutine(LoadSceneAsyncCoroutine(sceneName));
        }
        else
        {
            // 同步加载
            SceneManager.LoadScene(sceneName, loadMode);

            // 如果是叠加模式，同步加载后也要立即切换激活场景
            if (loadMode == LoadSceneMode.Additive)
            {
                Scene loadedScene = SceneManager.GetSceneByName(sceneName);
                if (loadedScene.IsValid())
                {
                    SceneManager.SetActiveScene(loadedScene);
                    Debug.Log($"【场景管理】✅ 同步叠加加载完成，激活场景已切换至：{sceneName}");
                }
            }
        }
    }

    // --------------------------------------------------
    // 2. 带参加载（按钮可用，但只支持一个字符串参数）
    // --------------------------------------------------
    public void LoadScene(string sceneName, string paramKey, string paramValue)
    {
        SceneTransitionData.SetParam(paramKey, paramValue);
        LoadScene(sceneName);
    }

    // 重载：如果需要传递多个参数，可以传JSON字符串，目标场景解析
    public void LoadSceneWithJson(string sceneName, string jsonParams)
    {
        // 例如 jsonParams = "{\"level\":3,\"mode\":\"hard\"}"
        // 在目标场景用 JsonUtility 或 LitJson 解析
        SceneTransitionData.SetParam("_json", jsonParams);
        LoadScene(sceneName);
    }

    // --------------------------------------------------
    // 3. 切换场景（加载新场景，并自动卸载旧场景）
    // --------------------------------------------------
    public void SwitchToScene(string sceneName)
    {
        StartCoroutine(SwitchSceneCoroutine(sceneName));
    }

    private IEnumerator SwitchSceneCoroutine(string sceneName)
    {
        // 加载新场景（异步）
        yield return LoadSceneAsyncCoroutine(sceneName);

        // 卸载当前激活场景（如果它不是新场景）
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName != sceneName)
        {
            UnloadBaseScene(currentSceneName, sceneName);
        }
    }

    // --------------------------------------------------
    // 4. 异步加载协程（内部使用，可等待）
    // --------------------------------------------------
    private IEnumerator LoadSceneAsyncCoroutine(string sceneName)
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        Debug.Log($"【场景管理】开始异步加载：{sceneName}，模式：{loadMode}");

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, loadMode);
        operation.allowSceneActivation = false;

        // 等待加载到 90%
        while (operation.progress < 0.9f)
            yield return null;

        // 这里可以放过渡动画等待
        yield return new WaitForSeconds(0.5f);

        // 允许场景激活（Unity 内部激活）
        operation.allowSceneActivation = true;

        // 如果是叠加（Additive）模式，需要手动把激活场景切到新场景
        if (loadMode == LoadSceneMode.Additive)
        {
            // 等待一帧，确保场景完全加载并注册到 SceneManager
            yield return null;

            Scene loadedScene = SceneManager.GetSceneByName(sceneName);
            if (loadedScene.IsValid() && loadedScene.isLoaded)
            {
                SceneManager.SetActiveScene(loadedScene);
                Debug.Log($"【场景管理】✅ 叠加加载完成，激活场景已自动切换至：{sceneName}");
            }
            else
            {
                Debug.LogWarning($"【场景管理】场景 {sceneName} 加载后无效，无法切换激活场景");
            }
        }
        else
        {
            Debug.Log($"【场景管理】Single 模式加载完成，当前激活场景：{SceneManager.GetActiveScene().name}");
        }

        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    // --------------------------------------------------
    // 5. 卸载场景
    // --------------------------------------------------
    public void UnloadBaseScene(string baseSceneName, string newActiveSceneName)
    {
        // 1. 获取场景引用并校验
        Scene baseScene = SceneManager.GetSceneByName(baseSceneName);
        Scene targetActiveScene = SceneManager.GetSceneByName(newActiveSceneName);

        if (!baseScene.isLoaded)
        {
            Debug.LogWarning($"【场景管理】要卸载的场景 {baseSceneName} 未加载，跳过操作。");
            return;
        }

        if (!targetActiveScene.isLoaded)
        {
            Debug.LogError($"【场景管理】目标激活场景 {newActiveSceneName} 未加载，无法转移激活权限！请先加载该场景。");
            return;
        }

        // 2. 【关键步骤】将激活场景切换给目标场景，防止实例化报错
        SceneManager.SetActiveScene(targetActiveScene);
        Debug.Log($"【场景管理】激活场景已切换至：{newActiveSceneName}");

        // 3. 异步卸载基础场景（异步能避免瞬间卡顿，且保证资源释放彻底）
        AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(baseSceneName);

        // 可选：如果你想在卸载完成后执行某些逻辑（如释放内存），开启协程监听
        if (unloadOp != null)
        {
            StartCoroutine(OnBaseSceneUnloaded(unloadOp, baseSceneName));
        }
    }

    // 重载：自动寻找可用的激活场景
    public void UnloadBaseScene(string baseSceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.isLoaded && s.name != baseSceneName)
            {
                UnloadBaseScene(baseSceneName, s.name);
                return;
            }
        }
        Debug.LogError($"【场景管理】找不到除 {baseSceneName} 之外的其他已加载场景，无法安全卸载。");
    }

    private IEnumerator OnBaseSceneUnloaded(AsyncOperation op, string sceneName)
    {
        yield return op;

        // 卸载完成后，强烈建议主动释放一次未使用的资源，节省内存
        Resources.UnloadUnusedAssets();
        Debug.Log($"【场景管理】场景 {sceneName} 已彻底卸载并清理内存。");
    }
}