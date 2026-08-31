using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class DialogueEditorWindow : EditorWindow
{
    private DialogueDataContainer targetContainer;  // 改为引用容器资产
    private Vector2 scrollPos;
    private List<DialogueData> editingList = new List<DialogueData>();
    private int selectedIndex = -1;
    private bool showOptions = false;

    [MenuItem("Window/Dialogue Editor")]
    public static void ShowWindow()
    {
        GetWindow<DialogueEditorWindow>("对话编辑器");
    }

    private void OnGUI()
    {
        GUILayout.Label("对话编辑器", EditorStyles.boldLabel);

        // 选择容器资产（改为 ObjectField 接受 ScriptableObject）
        EditorGUILayout.BeginHorizontal();
        targetContainer = (DialogueDataContainer)EditorGUILayout.ObjectField("对话数据", targetContainer, typeof(DialogueDataContainer), false);
        if (GUILayout.Button("从黑板加载", GUILayout.Width(100)))
        {
            // 如果选中场景中的黑板，可以尝试从它加载容器
            if (Selection.activeGameObject != null)
            {
                var board = Selection.activeGameObject.GetComponent<DialogueBlackboard>();
                if (board != null && board.currentDialogue != null)
                {
                    targetContainer = board.currentDialogue;
                    LoadFromContainer();
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        if (targetContainer == null)
        {
            EditorGUILayout.HelpBox("请选择或创建一个对话数据容器资产", MessageType.Info);
            if (GUILayout.Button("创建新对话数据容器"))
            {
                // 弹出保存对话框创建新资产
                string path = EditorUtility.SaveFilePanelInProject("创建对话数据", "NewDialogue", "asset", "请选择保存位置");
                if (!string.IsNullOrEmpty(path))
                {
                    DialogueDataContainer newContainer = ScriptableObject.CreateInstance<DialogueDataContainer>();
                    AssetDatabase.CreateAsset(newContainer, path);
                    AssetDatabase.SaveAssets();
                    targetContainer = newContainer;
                    LoadFromContainer();
                }
            }
            return;
        }

        // 工具栏（添加、清空、保存）
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("添加新对话"))
        {
            DialogueData newData = new DialogueData();
            newData.speaker = "NPC";
            newData.content = "新对话内容";
            newData.nextIndex = -1;
            newData.choices = new List<DialogueChoice>();
            editingList.Add(newData);
            selectedIndex = editingList.Count - 1;
        }
        if (GUILayout.Button("清空列表"))
        {
            if (EditorUtility.DisplayDialog("确认", "清空所有对话？", "确定", "取消"))
            {
                editingList.Clear();
                selectedIndex = -1;
            }
        }
        if (GUILayout.Button("保存到资产"))
        {
            SaveToContainer();
        }
        EditorGUILayout.EndHorizontal();

        // 列表显示（与原来基本相同）
        // ...（省略，同原代码，使用 editingList）
        // 注意：在原代码中显示详细信息时，需要访问 data.choices 等，保持不变

        // 底部的提示
        EditorGUILayout.HelpBox($"当前编辑：{targetContainer.name}，共 {editingList.Count} 条对话", MessageType.Info);
    }

    private void LoadFromContainer()
    {
        if (targetContainer == null) return;
        editingList.Clear();
        foreach (var item in targetContainer.dialoguePieces)
        {
            // 深拷贝
            DialogueData copy = new DialogueData
            {
                speaker = item.speaker,
                content = item.content,
                portrait = item.portrait,
                nextIndex = item.nextIndex,
                choices = new List<DialogueChoice>()
            };
            if (item.choices != null)
            {
                foreach (var ch in item.choices)
                    copy.choices.Add(new DialogueChoice { choiceText = ch.choiceText, targetIndex = ch.targetIndex });
            }
            editingList.Add(copy);
        }
        selectedIndex = -1;
    }

    private void SaveToContainer()
    {
        if (targetContainer == null) return;
        // 直接替换列表（注意：由于是 ScriptableObject，需要标记为脏）
        targetContainer.dialoguePieces = new List<DialogueData>(editingList);
        EditorUtility.SetDirty(targetContainer);
        AssetDatabase.SaveAssets();
        Debug.Log($"对话已保存到 {targetContainer.name}，共 {editingList.Count} 条");
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }
}