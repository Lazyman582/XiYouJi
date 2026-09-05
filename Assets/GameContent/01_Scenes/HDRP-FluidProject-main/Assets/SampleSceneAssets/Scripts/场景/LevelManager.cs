using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
  

    void Start()
    {
        // 读取参数（注意这里 key 要与按钮传的一致，比如 "enemyID"）
        int stateID = SceneTransitionData.GetParam<int>("playerID", 0);

        // 【核心改动】不实例化，直接通知管理器启动！
        if (StateManager.Instance != null)
        {
            StateManager.Instance.ApplyState(stateID);
        }
        else
        {
            Debug.LogError("场景中缺少 SceneStateManager，请检查！");
        }

        // 清理参数，防止下次进入残留
        SceneTransitionData.ClearParam("enemyID");
    }
}