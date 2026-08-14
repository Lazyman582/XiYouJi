using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AI;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using XiYouJi.Gameplay;

namespace XiYouJi.GameplayEditor
{
    [CustomEditor(typeof(PolygonWalkableArea))]
    public sealed class WalkablePolygonEditor : Editor
    {
        private const float VertexHandleScale = 0.075f;

        private SerializedProperty pointsProperty;
        private SerializedProperty toleranceProperty;
        private SerializedProperty showProperty;
        private ReorderableList pointsList;

        private PolygonWalkableArea Area => (PolygonWalkableArea)target;

        private void OnEnable()
        {
            pointsProperty = serializedObject.FindProperty("localPoints");
            toleranceProperty = serializedObject.FindProperty("boundaryTolerance");
            showProperty = serializedObject.FindProperty("showWhenNotSelected");

            pointsList = new ReorderableList(serializedObject, pointsProperty, true, true, true, true);
            pointsList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "顶点顺序（X / Z）");
            pointsList.drawElementCallback = DrawPointElement;
            pointsList.onAddCallback = AddPoint;
            pointsList.onCanRemoveCallback = list => list.count > 3;
            pointsList.onRemoveCallback = RemovePoint;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "绿色内部是玩家可行走范围。选中本物体后可在 Scene 视图直接拖动编号顶点；按住 Shift 左键可在鼠标位置插入新顶点。凹多边形同样支持。",
                MessageType.Info);

            pointsList.DoLayoutList();
            EditorGUILayout.PropertyField(toleranceProperty, new GUIContent("边界容差"));
            EditorGUILayout.PropertyField(showProperty, new GUIContent("未选中时仍显示"));

            if (pointsProperty.arraySize < 3)
            {
                EditorGUILayout.HelpBox("至少需要 3 个顶点；不足时范围限制会暂时关闭。", MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("按相机边界重置"))
                {
                    WalkablePolygonTools.ResetFromCameraBounds(Area);
                }

                if (GUILayout.Button("按 NavMesh 外框重置"))
                {
                    WalkablePolygonTools.ResetFromNavMeshBounds(Area);
                }
            }

            if (GUILayout.Button("绑定到当前场景的点击移动角色"))
            {
                WalkablePolygonTools.BindControllersInActiveScene(Area);
            }

            if (GUILayout.Button("烘焙当前场景旧版 NavMesh"))
            {
                WalkablePolygonTools.BakeCurrentScene();
            }
        }

        private void OnSceneGUI()
        {
            PolygonWalkableArea area = Area;
            if (area.PointCount < 1)
            {
                return;
            }

            DrawFilledPolygon(area);
            DrawVertexHandles(area);
            HandleShiftClickInsert(area);
        }

        private void DrawPointElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty point = pointsProperty.GetArrayElementAtIndex(index);
            rect.y += 1f;
            rect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(rect, point, new GUIContent((index + 1).ToString()));
        }

        private void AddPoint(ReorderableList list)
        {
            serializedObject.Update();
            int count = pointsProperty.arraySize;
            int insertIndex = list.index >= 0 ? list.index + 1 : count;
            pointsProperty.InsertArrayElementAtIndex(insertIndex);

            Vector2 point;
            if (count >= 2)
            {
                int previous = Mathf.Clamp(insertIndex - 1, 0, count - 1);
                int next = insertIndex % count;
                Vector2 a = pointsProperty.GetArrayElementAtIndex(previous).vector2Value;
                Vector2 b = pointsProperty.GetArrayElementAtIndex(next).vector2Value;
                point = (a + b) * 0.5f;
            }
            else
            {
                point = Vector2.zero;
            }

            pointsProperty.GetArrayElementAtIndex(insertIndex).vector2Value = point;
            serializedObject.ApplyModifiedProperties();
            list.index = insertIndex;
            SceneView.RepaintAll();
        }

        private void RemovePoint(ReorderableList list)
        {
            if (pointsProperty.arraySize <= 3)
            {
                return;
            }

            ReorderableList.defaultBehaviours.DoRemoveButton(list);
            serializedObject.ApplyModifiedProperties();
            SceneView.RepaintAll();
        }

        private static void DrawFilledPolygon(PolygonWalkableArea area)
        {
            List<int> triangles = Triangulate(area);
            Handles.color = new Color(0.12f, 0.95f, 0.28f, 0.09f);
            for (int i = 0; i + 2 < triangles.Count; i += 3)
            {
                Handles.DrawAAConvexPolygon(
                    area.GetWorldPoint(triangles[i]) + Vector3.up * 0.06f,
                    area.GetWorldPoint(triangles[i + 1]) + Vector3.up * 0.06f,
                    area.GetWorldPoint(triangles[i + 2]) + Vector3.up * 0.06f);
            }

            Vector3[] outline = new Vector3[area.PointCount + 1];
            for (int i = 0; i < area.PointCount; i++)
            {
                outline[i] = area.GetWorldPoint(i) + Vector3.up * 0.08f;
            }

            outline[area.PointCount] = outline[0];
            Handles.color = new Color(1f, 0.72f, 0.08f, 1f);
            Handles.DrawAAPolyLine(4f, outline);
        }

        private void DrawVertexHandles(PolygonWalkableArea area)
        {
            for (int i = 0; i < area.PointCount; i++)
            {
                Vector3 world = area.GetWorldPoint(i);
                float size = HandleUtility.GetHandleSize(world) * VertexHandleScale;
                Handles.color = Color.yellow;

                EditorGUI.BeginChangeCheck();
                var fmh_177_21_639223142471388854 = Quaternion.identity; Vector3 moved = Handles.FreeMoveHandle(
                    world,
                    size,
                    Vector3.zero,
                    Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(area, "Move walkable polygon vertex");
                    Vector3 local = area.transform.InverseTransformPoint(moved);
                    area.SetLocalPoint(i, new Vector2(local.x, local.z));
                    EditorUtility.SetDirty(area);
                }

                Handles.Label(
                    world + Vector3.up * size * 1.7f,
                    (i + 1).ToString(),
                    EditorStyles.whiteBoldLabel);
            }
        }

        private static void HandleShiftClickInsert(PolygonWalkableArea area)
        {
            Event current = Event.current;
            if (!current.shift || current.alt || current.control || current.command)
            {
                return;
            }

            if (current.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                return;
            }

            if (current.type != EventType.MouseDown || current.button != 0)
            {
                return;
            }

            Ray ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
            Vector3 worldPoint;
            if (Physics.Raycast(ray, out RaycastHit hit, 2000f, ~0, QueryTriggerInteraction.Ignore))
            {
                worldPoint = hit.point;
            }
            else
            {
                Plane plane = new Plane(Vector3.up, area.transform.position);
                if (!plane.Raycast(ray, out float enter))
                {
                    return;
                }

                worldPoint = ray.GetPoint(enter);
            }

            int insertIndex = FindNearestEdgeEndIndex(area, worldPoint);
            Vector3 local = area.transform.InverseTransformPoint(worldPoint);
            Undo.RecordObject(area, "Insert walkable polygon vertex");
            area.InsertLocalPoint(insertIndex, new Vector2(local.x, local.z));
            EditorUtility.SetDirty(area);
            current.Use();
        }

        private static int FindNearestEdgeEndIndex(PolygonWalkableArea area, Vector3 worldPoint)
        {
            int bestIndex = area.PointCount;
            float bestSqrDistance = float.PositiveInfinity;
            Vector2 point = new Vector2(worldPoint.x, worldPoint.z);

            for (int i = 0; i < area.PointCount; i++)
            {
                Vector3 worldA = area.GetWorldPoint(i);
                Vector3 worldB = area.GetWorldPoint((i + 1) % area.PointCount);
                Vector2 a = new Vector2(worldA.x, worldA.z);
                Vector2 b = new Vector2(worldB.x, worldB.z);
                Vector2 segment = b - a;
                float denominator = segment.sqrMagnitude;
                float t = denominator <= 0.000001f
                    ? 0f
                    : Mathf.Clamp01(Vector2.Dot(point - a, segment) / denominator);
                Vector2 closest = a + segment * t;
                float sqrDistance = (closest - point).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    bestIndex = i + 1;
                }
            }

            return bestIndex;
        }

        private static List<int> Triangulate(PolygonWalkableArea area)
        {
            List<int> result = new List<int>();
            int count = area.PointCount;
            if (count < 3)
            {
                return result;
            }

            List<int> remaining = new List<int>(count);
            float signedArea = 0f;
            for (int i = 0; i < count; i++)
            {
                Vector2 a = area.GetLocalPoint(i);
                Vector2 b = area.GetLocalPoint((i + 1) % count);
                signedArea += a.x * b.y - b.x * a.y;
            }

            if (signedArea > 0f)
            {
                for (int i = 0; i < count; i++)
                {
                    remaining.Add(i);
                }
            }
            else
            {
                for (int i = count - 1; i >= 0; i--)
                {
                    remaining.Add(i);
                }
            }

            int guard = count * count;
            while (remaining.Count > 2 && guard-- > 0)
            {
                bool clipped = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    int previousIndex = remaining[(i - 1 + remaining.Count) % remaining.Count];
                    int currentIndex = remaining[i];
                    int nextIndex = remaining[(i + 1) % remaining.Count];
                    Vector2 a = area.GetLocalPoint(previousIndex);
                    Vector2 b = area.GetLocalPoint(currentIndex);
                    Vector2 c = area.GetLocalPoint(nextIndex);

                    if (Cross(b - a, c - b) <= 0.000001f
                        || ContainsAnyPoint(area, remaining, previousIndex, currentIndex, nextIndex, a, b, c))
                    {
                        continue;
                    }

                    result.Add(previousIndex);
                    result.Add(currentIndex);
                    result.Add(nextIndex);
                    remaining.RemoveAt(i);
                    clipped = true;
                    break;
                }

                if (!clipped)
                {
                    result.Clear();
                    break;
                }
            }

            return result;
        }

        private static bool ContainsAnyPoint(
            PolygonWalkableArea area,
            List<int> indices,
            int triangleA,
            int triangleB,
            int triangleC,
            Vector2 a,
            Vector2 b,
            Vector2 c)
        {
            for (int i = 0; i < indices.Count; i++)
            {
                int index = indices[i];
                if (index == triangleA || index == triangleB || index == triangleC)
                {
                    continue;
                }

                Vector2 point = area.GetLocalPoint(index);
                if (Cross(b - a, point - a) >= 0f
                    && Cross(c - b, point - b) >= 0f
                    && Cross(a - c, point - c) >= 0f)
                {
                    return true;
                }
            }

            return false;
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }
    }

    public static class WalkablePolygonTools
    {
        private const string PolygonObjectName = "Player Walkable Polygon";

        [MenuItem("Tools/XiYouJi/Navigation/Create or Select Walkable Polygon")]
        public static void CreateOrSelectWalkablePolygon()
        {
            PolygonWalkableArea area = FindInActiveScene<PolygonWalkableArea>();
            if (area == null)
            {
                GameObject gameObject = new GameObject(PolygonObjectName);
                Undo.RegisterCreatedObjectUndo(gameObject, "Create walkable polygon");
                area = Undo.AddComponent<PolygonWalkableArea>(gameObject);

                CameraMovementBounds cameraBounds = FindInActiveScene<CameraMovementBounds>();
                if (cameraBounds != null)
                {
                    ResetFromCameraBounds(area);
                }
                else
                {
                    ResetFromNavMeshBounds(area);
                }
            }

            BindControllersInActiveScene(area);
            Selection.activeGameObject = area.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        [MenuItem("Tools/XiYouJi/Navigation/Mark Selected Not Walkable")]
        public static void MarkSelectedNotWalkable()
        {
            MarkSelectedNavigationArea("Not Walkable");
        }

        [MenuItem("Tools/XiYouJi/Navigation/Mark Selected Walkable")]
        public static void MarkSelectedWalkable()
        {
            MarkSelectedNavigationArea("Walkable");
        }

        [MenuItem("Tools/XiYouJi/Navigation/Bake Current Scene NavMesh")]
        public static void BakeCurrentScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before baking the NavMesh.");
                return;
            }

            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("XiYouJi: baked the legacy NavMesh for " + SceneManager.GetActiveScene().name + ".");
        }

        public static void ResetFromCameraBounds(PolygonWalkableArea area)
        {
            CameraMovementBounds bounds = FindInActiveScene<CameraMovementBounds>();
            if (bounds == null)
            {
                Debug.LogWarning("No CameraMovementBounds exists in the active scene.", area);
                return;
            }

            Undo.RecordObjects(new Object[] { area, area.transform }, "Reset walkable polygon from camera bounds");
            area.transform.SetPositionAndRotation(bounds.transform.position, Quaternion.identity);
            area.transform.localScale = Vector3.one;
            area.SetRectangle(bounds.WorldSize);
            EditorUtility.SetDirty(area);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            SceneView.RepaintAll();
        }

        public static void ResetFromNavMeshBounds(PolygonWalkableArea area)
        {
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            if (triangulation.vertices == null || triangulation.vertices.Length == 0)
            {
                Debug.LogWarning("The active scene does not contain a baked NavMesh.", area);
                return;
            }

            Bounds bounds = new Bounds(triangulation.vertices[0], Vector3.zero);
            for (int i = 1; i < triangulation.vertices.Length; i++)
            {
                bounds.Encapsulate(triangulation.vertices[i]);
            }

            Undo.RecordObjects(new Object[] { area, area.transform }, "Reset walkable polygon from NavMesh bounds");
            area.transform.SetPositionAndRotation(
                new Vector3(bounds.center.x, area.transform.position.y, bounds.center.z),
                Quaternion.identity);
            area.transform.localScale = Vector3.one;
            area.SetRectangle(new Vector2(bounds.size.x, bounds.size.z));
            EditorUtility.SetDirty(area);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            SceneView.RepaintAll();
        }

        public static void BindControllersInActiveScene(PolygonWalkableArea area)
        {
            PointClickNavController[] controllers = Resources.FindObjectsOfTypeAll<PointClickNavController>();
            int boundCount = 0;
            for (int i = 0; i < controllers.Length; i++)
            {
                PointClickNavController controller = controllers[i];
                if (controller.gameObject.scene != SceneManager.GetActiveScene())
                {
                    continue;
                }

                Undo.RecordObject(controller, "Bind walkable polygon");
                controller.walkableArea = area;
                EditorUtility.SetDirty(controller);
                boundCount++;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("XiYouJi: bound walkable polygon to " + boundCount + " click-move controller(s).");
        }

        private static void MarkSelectedNavigationArea(string areaName)
        {
            int areaIndex = NavMesh.GetAreaFromName(areaName);
            if (areaIndex < 0)
            {
                Debug.LogError("NavMesh area does not exist: " + areaName);
                return;
            }

            HashSet<GameObject> targets = new HashSet<GameObject>();
            GameObject[] selected = Selection.gameObjects;
            for (int i = 0; i < selected.Length; i++)
            {
                Transform[] hierarchy = selected[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < hierarchy.Length; j++)
                {
                    GameObject candidate = hierarchy[j].gameObject;
                    if (candidate.GetComponent<Renderer>() != null
                        || candidate.GetComponent<Collider>() != null
                        || candidate.GetComponent<Terrain>() != null)
                    {
                        targets.Add(candidate);
                    }
                }
            }

            foreach (GameObject target in targets)
            {
                Undo.RecordObject(target, "Set NavMesh area");
                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(target);
                GameObjectUtility.SetStaticEditorFlags(target, flags | StaticEditorFlags.NavigationStatic);
                GameObjectUtility.SetNavMeshArea(target, areaIndex);
                EditorUtility.SetDirty(target);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("XiYouJi: marked " + targets.Count + " object(s) as " + areaName + ". Bake the current scene NavMesh to apply it.");
        }

        private static T FindInActiveScene<T>() where T : Component
        {
            T[] components = Resources.FindObjectsOfTypeAll<T>();
            Scene activeScene = SceneManager.GetActiveScene();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i].gameObject.scene == activeScene)
                {
                    return components[i];
                }
            }

            return null;
        }
    }
}
