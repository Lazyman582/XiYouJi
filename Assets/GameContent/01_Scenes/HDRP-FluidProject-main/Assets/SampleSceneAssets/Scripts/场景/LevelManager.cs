using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("生成配置")]
    [SerializeField] private GameObject[] enemyPrefabs; // 按ID索引
    [Header("生成容器")]
    [SerializeField] private Transform spawnContainer;

    void Start()
    {
        // 1. 读取参数（假设传入了 "enemyID"）
        int enemyID = SceneTransitionData.GetParam<int>("playerID", 0);

        // 2. 根据参数生成物体
        if (enemyID >= 0 && enemyID < enemyPrefabs.Length)
        {
            Instantiate(enemyPrefabs[enemyID], spawnContainer.position, spawnContainer.rotation, spawnContainer);
            Debug.Log($"生成敌人 ID：{enemyID}");
        }
        else
        {
            Debug.LogWarning($"无效的敌人ID：{enemyID}");
        }

        // 3. 清理参数（防止残留）
        SceneTransitionData.ClearParam("enemyID");
    }
}