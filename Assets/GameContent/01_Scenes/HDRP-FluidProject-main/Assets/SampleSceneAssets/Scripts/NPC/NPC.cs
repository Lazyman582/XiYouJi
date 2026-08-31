using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCInteract : MonoBehaviour
{
    [SerializeField] private DialogueDataContainer dialogueContainer; // 在 Inspector 中拖拽对话资产

    private void Start()
    {
        // 如果未手动拖拽，尝试从同物体获取（但 DialogueDataContainer 是 ScriptableObject，不是组件，所以大概率失败）
        // 这里保留兼容性，但强烈建议在 Inspector 中显式赋值
        if (dialogueContainer == null)
        {
            // 尝试从 Resources 加载？不推荐，最好强制要求拖拽
            Debug.LogWarning($"{gameObject.name} 缺少对话数据容器，请在 Inspector 中赋值。");
        }
    }
    public void SetDialogueContainer(DialogueDataContainer newContainer)
    {
        dialogueContainer = newContainer;
        Debug.Log($"{gameObject.name} 的对话已更新为: {newContainer.name}");
    }

    // 鼠标点击 NPC（需 Collider + 合适的物理设置）
    private void OnMouseDown()
    {
        // 1. 检查数据是否有效
        if (dialogueContainer == null)
        {
            Debug.LogWarning("NPC 没有对话数据容器，无法开始对话");
            return;
        }

        // 2. 获取全局管理器单例
        var controller = DialogueController.Instance;
        var ui = DialogueUIController.Instance;

        if (controller == null || ui == null)
        {
            Debug.LogError("对话系统未初始化，请确保场景中存在 DialogueController 和 DialogueUIController");
            return;
        }

        // 3. 启动对话：传入数据容器，控制器会自动重置索引
        controller.StartDialogue(dialogueContainer);

        // 4. 打开 UI 面板并显示第一条对话
        ui.OpenPanel();
        ui.ShowCurrentDialogue(); // 这个方法内部会调用 GetCurrentDialogue 并渲染
    }
}
