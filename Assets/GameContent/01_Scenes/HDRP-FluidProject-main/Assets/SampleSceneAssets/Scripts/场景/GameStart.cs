using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏启动引导器：自动加载UI等固定叠加场景
/// </summary>
public class GameStart : MonoBehaviour
{
    [Header("固定场景列表（按顺序加载）")]
    [Tooltip("把你要预先加载的场景名按顺序填进来，例如：UI, GlobalSystem, GameManager")]
    public string[] fixedScenes = new string[] { "UI", "GlobalSystem" };

    [Header("加载完毕后是否卸载启动场景本身？")]
    [Tooltip("建议勾选，启动场景通常只是个空壳，留着浪费内存")]
    public bool unloadBootSceneAfterLoad = true;

    [Header("加载面板UI（可选）")]
    public GameObject loadingPanel; // 如果需要显示加载转圈，拖进来

    private void Start()
    {
        // 开始加载固定场景
        StartCoroutine(LoadFixedScenesCoroutine());
    }

    private IEnumerator LoadFixedScenesCoroutine()
    {
        // 1. 显示加载界面（如果有）
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        // 2. 遍历加载所有固定场景（叠加模式）
        for (int i = 0; i < fixedScenes.Length; i++)
        {
            string sceneName = fixedScenes[i];

            // 参数校验：空字符串跳过
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning($"【GameStart】数组第 {i} 项为空，已跳过");
                continue;
            }

            // 校验场景是否存在于 Build Settings 中（防止打包后崩溃）
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"【GameStart】场景 '{sceneName}' 未添加到 Build Settings！请检查。");
                continue;
            }

            // 校验是否已经加载（防止重复）
            if (SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                Debug.Log($"【GameStart】场景 '{sceneName}' 已加载，跳过重复加载");
                continue;
            }

            Debug.Log($"【GameStart】正在加载固定场景：{sceneName} ({i + 1}/{fixedScenes.Length})");
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            // 等待该场景加载完成（防止加载顺序错乱）
            yield return op;
        }

        // 3. 【关键步骤】将最后一个成功加载的场景设为激活场景
        //    如果最后一个场景名无效，则用默认逻辑寻找备用
        string lastValidScene = GetLastValidSceneName();
        if (!string.IsNullOrEmpty(lastValidScene))
        {
            Scene targetScene = SceneManager.GetSceneByName(lastValidScene);
            if (targetScene.IsValid())
            {
                SceneManager.SetActiveScene(targetScene);
                Debug.Log($"【GameStart】激活场景已切换至：{lastValidScene}");
            }
        }
        else
        {
            Debug.LogWarning("【GameStart】没有有效的场景可设置为激活状态，请检查 fixedScenes 数组");
        }

        // 4. 隐藏加载界面
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        // 5. 卸载启动场景（释放内存）
        if (unloadBootSceneAfterLoad)
        {
            // 注意：不能卸载当前激活场景，所以先确保激活场景不是这个启动场景
            if (SceneManager.GetActiveScene().name != gameObject.scene.name)
            {
                AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(gameObject.scene);
                yield return unloadOp;
                Debug.Log($"【GameStart】启动场景 '{gameObject.scene.name}' 已卸载，内存释放");

                // 卸载后强制回收未使用资源（可选）
                Resources.UnloadUnusedAssets();
            }
            else
            {
                Debug.LogWarning("【GameStart】当前激活场景仍是启动场景，为避免崩溃，暂不卸载启动场景。请检查 fixedScenes 是否加载成功。");
            }
        }
    }

    /// <summary>
    /// 从数组中倒序查找最后一个成功加载的场景名
    /// </summary>
    private string GetLastValidSceneName()
    {
        for (int i = fixedScenes.Length - 1; i >= 0; i--)
        {
            string name = fixedScenes[i];
            if (!string.IsNullOrEmpty(name) && SceneManager.GetSceneByName(name).IsValid())
            {
                return name;
            }
        }
        return null;
    }
}