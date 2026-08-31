using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.Timeline.AnimationPlayableAsset;


/// <summary>
/// 场景管理工具 - 支持按钮调用含参方法
/// </summary>
public class SceneLoader : MonoBehaviour
{
    [Header("加载设置")]
    [Tooltip("是否异步加载（推荐开启，防止卡顿）")]
    public bool useAsyncLoad = true;

    [Header("加载提示UI（可选）")]
    public GameObject loadingPanel; // 可拖拽一个加载中的转圈UI

    public LoadSceneMode loadMode = LoadSceneMode.Additive;
    /// <summary>
    /// 【核心方法】对外公开，供按钮调用的场景跳转方法
    /// 参数：sceneName - 目标场景名称（必须与Build Settings中的名称一致）
    /// </summary>
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

    /// <summary>
    /// 异步加载协程（附带显示加载面板）
    /// </summary>
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

        // ---------- 【关键新增】加载完成后，自动切换激活场景 ----------
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
        // 如果是 Single 模式，Unity 会自动切换激活场景，无需手动处理
        else
        {
            Debug.Log($"【场景管理】Single 模式加载完成，当前激活场景：{SceneManager.GetActiveScene().name}");
        }

        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

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

        // 2. 【关键步骤】将激活场景切换给场景2或场景3，防止实例化报错
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

    private IEnumerator OnBaseSceneUnloaded(AsyncOperation op, string sceneName)
    {
        yield return op;

        // 卸载完成后，强烈建议主动释放一次未使用的资源，节省内存
        Resources.UnloadUnusedAssets();
        Debug.Log($"【场景管理】场景 {sceneName} 已彻底卸载并清理内存。");
    }

    // ------------------- 超便捷重载方法 -------------------
    /// <summary>
    /// 重载：如果你懒得敲新激活场景名，默认把当前激活场景切换给“第一个找到的已加载场景”。
    /// 但不推荐，还是显式指定最安全。
    /// </summary>
    public void UnloadBaseScene(string baseSceneName)
    {
        // 尝试寻找一个不是基础场景的已加载场景作为新激活场景
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
}
