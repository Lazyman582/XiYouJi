using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonCooldown : MonoBehaviour
{
    public Button targetButton;          // 要控制的按钮
    public float cooldownTime = 0.15f;   // 冷却时间（秒）

    private bool isOnCooldown = false;

    void Start()
    {
        // 为按钮的点击事件添加监听
        targetButton.onClick.AddListener(OnButtonClick);
    }

    public void OnButtonClick()
    {
        // 如果正在冷却，则忽略此次点击
        if (isOnCooldown) return;

        // --- 在这里执行你的按钮逻辑 ---
        Debug.Log("按钮被点击了！");
        // 例如：加载场景、播放音效等
        // -----------------------------

        // 触发冷却
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        isOnCooldown = true;
        targetButton.interactable = false;  // 禁用按钮

        // 等待指定的冷却时间
        yield return new WaitForSeconds(cooldownTime);

        targetButton.interactable = true;   // 重新启用按钮
        isOnCooldown = false;
    }
}
