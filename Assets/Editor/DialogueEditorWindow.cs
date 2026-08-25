using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class DialogueEditorWindow : EditorWindow
{
    private DialogueBlackboard targetBlackboard;
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

        // 选择黑板
        EditorGUILayout.BeginHorizontal();
        targetBlackboard = (DialogueBlackboard)EditorGUILayout.ObjectField("目标黑板", targetBlackboard, typeof(DialogueBlackboard), true);
        if (GUILayout.Button("从场景加载", GUILayout.Width(100)))
        {
            if (Selection.activeGameObject != null)
                targetBlackboard = Selection.activeGameObject.GetComponent<DialogueBlackboard>();
            if (targetBlackboard == null)
                EditorUtility.DisplayDialog("提示", "请选中场景中挂载DialogueBlackboard的游戏对象", "确定");
            else
                LoadFromBlackboard();
        }
        EditorGUILayout.EndHorizontal();

        if (targetBlackboard == null)
        {
            EditorGUILayout.HelpBox("请选择或加载一个DialogueBlackboard", MessageType.Info);
            return;
        }

        // 工具栏
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
        if (GUILayout.Button("保存到黑板"))
        {
            SaveToBlackboard();
        }
        EditorGUILayout.EndHorizontal();

        // 列表显示
        EditorGUILayout.Space();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        for (int i = 0; i < editingList.Count; i++)
        {
            DialogueData data = editingList[i];
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            // 序号和说话人
            GUILayout.Label($"#{i}  {data.speaker}", GUILayout.Width(120));
            // 内容预览
            string preview = data.content.Length > 30 ? data.content.Substring(0, 30) + "..." : data.content;
            GUILayout.Label(preview, GUILayout.MinWidth(150));
            GUILayout.FlexibleSpace();

            // 操作按钮
            if (GUILayout.Button("↑", GUILayout.Width(25)) && i > 0)
            {
                (editingList[i], editingList[i - 1]) = (editingList[i - 1], editingList[i]);
                selectedIndex = i - 1;
            }
            if (GUILayout.Button("↓", GUILayout.Width(25)) && i < editingList.Count - 1)
            {
                (editingList[i], editingList[i + 1]) = (editingList[i + 1], editingList[i]);
                selectedIndex = i + 1;
            }
            if (GUILayout.Button("编辑", GUILayout.Width(50)))
            {
                selectedIndex = i;
            }
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                if (EditorUtility.DisplayDialog("删除", $"删除第 {i} 条对话？", "确定", "取消"))
                {
                    editingList.RemoveAt(i);
                    if (selectedIndex >= editingList.Count) selectedIndex = editingList.Count - 1;
                    if (selectedIndex == i) selectedIndex = -1;
                }
            }
            EditorGUILayout.EndHorizontal();

            // 显示详细信息（如果选中）
            if (selectedIndex == i)
            {
                EditorGUILayout.LabelField("详细信息", EditorStyles.miniLabel);
                data.speaker = EditorGUILayout.TextField("说话人", data.speaker);
                data.content = EditorGUILayout.TextArea(data.content, GUILayout.Height(60));
                data.nextIndex = EditorGUILayout.IntField("下一个索引 ( -1 表示顺序推进 )", data.nextIndex);

                // 选项编辑
                showOptions = EditorGUILayout.Foldout(showOptions, "选项 (分支)");
                if (showOptions)
                {
                    if (data.choices == null) data.choices = new List<DialogueChoice>();
                    for (int c = 0; c < data.choices.Count; c++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        data.choices[c].choiceText = EditorGUILayout.TextField(data.choices[c].choiceText, GUILayout.Width(200));
                        data.choices[c].targetIndex = EditorGUILayout.IntField("目标", data.choices[c].targetIndex);
                        if (GUILayout.Button("删除选项", GUILayout.Width(80)))
                        {
                            data.choices.RemoveAt(c);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    if (GUILayout.Button("添加选项"))
                    {
                        data.choices.Add(new DialogueChoice { choiceText = "新选项", targetIndex = -1 });
                    }
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        EditorGUILayout.EndScrollView();
    }

    private void LoadFromBlackboard()
    {
        if (targetBlackboard == null) return;
        // 复制数据，避免直接引用
        editingList.Clear();
        foreach (var item in targetBlackboard.dialoguePieces)
        {
            DialogueData copy = new DialogueData();
            copy.speaker = item.speaker;
            copy.content = item.content;
            copy.portrait = item.portrait;
            copy.nextIndex = item.nextIndex;
            copy.choices = new List<DialogueChoice>();
            if (item.choices != null)
            {
                foreach (var ch in item.choices)
                    copy.choices.Add(new DialogueChoice { choiceText = ch.choiceText, targetIndex = ch.targetIndex });
            }
            editingList.Add(copy);
        }
        selectedIndex = -1;
        EditorUtility.SetDirty(this);
    }

    private void SaveToBlackboard()
    {
        if (targetBlackboard == null) return;
        // 直接替换列表（注意：由于是 public 字段，可以这样操作）
        targetBlackboard.dialoguePieces = new List<DialogueData>(editingList);
        // 不需要再调用 BuildIndex，因为现在直接使用 List
        EditorUtility.SetDirty(targetBlackboard);
        Debug.Log($"对话已保存，共 {editingList.Count} 条");
        // 刷新 Inspector 显示
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }
}