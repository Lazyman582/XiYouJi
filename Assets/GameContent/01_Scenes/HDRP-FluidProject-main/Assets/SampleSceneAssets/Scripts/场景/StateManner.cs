using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateManager : MonoBehaviour
{
    public static StateManager Instance { get; private set; }

    [Header("状态组配置（索引对应 enemyID）")]
    [Tooltip("0号索引放石头，1号索引放桃树...")]
    public GameObject[] stateGroups;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // 启动时默认隐藏所有，或者根据需求显示默认状态
        // 这里为了演示，默认全部关闭
        ApplyState(-1);
    }

    // 核心方法：根据ID切换显隐
    public void ApplyState(int stateID)
    {
        // 1. 先全部隐藏
        foreach (GameObject group in stateGroups)
        {
            if (group != null)
                group.SetActive(false);
        }

        // 2. 再显示指定的那一组（如果ID有效）
        if (stateID >= 0 && stateID < stateGroups.Length && stateGroups[stateID] != null)
        {
            stateGroups[stateID].SetActive(true);
            Debug.Log($"【状态管理器】已切换至组 ID: {stateID}");
        }
        else
        {
            Debug.Log($"【状态管理器】无有效状态，全部隐藏");
        }
    }
}