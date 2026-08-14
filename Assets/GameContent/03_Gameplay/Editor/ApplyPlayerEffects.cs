using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using XiYouJi.Gameplay;

namespace XiYouJi.EditorTools
{
    public static class ApplyPlayerEffects
    {
        private const string PrefabPath = "Assets/GameContent/03_Gameplay/Prefabs/Player_ClickMove.prefab";
        private const string OutlineMaterialPath = "Assets/GameContent/03_Gameplay/Materials/MOD_PlayerOutline.mat";
        private const string RingMaterialPath = "Assets/GameContent/03_Gameplay/Materials/MOD_PlayerFootRing.mat";

        [MenuItem("XiYouJi/Apply Player Outline And Foot Ring")]
        public static void Apply()
        {
            var outlineShader = Shader.Find("XiYouJi/PlayerOutline");
            var ringShader = Shader.Find("XiYouJi/PlayerFootRing");
            if (outlineShader == null || ringShader == null)
            {
                throw new System.InvalidOperationException("Player effect shaders were not found. Wait for shader import, then run the menu item again.");
            }

            EnsureMaterialsFolder();
            var outlineMaterial = LoadOrCreateMaterial(OutlineMaterialPath, outlineShader);
            outlineMaterial.SetColor("_OutlineColor", new Color(0.05f, 0.75f, 1f, 1f));
            outlineMaterial.SetFloat("_OutlineWidth", 0.03f);
            EditorUtility.SetDirty(outlineMaterial);

            var ringMaterial = LoadOrCreateMaterial(RingMaterialPath, ringShader);
            ringMaterial.SetColor("_Color", new Color(0.05f, 0.8f, 1f, 1f));
            ringMaterial.SetFloat("_Radius", 0.72f);
            ringMaterial.SetFloat("_Thickness", 0.07f);
            ringMaterial.SetFloat("_EdgeSoftness", 0.025f);
            ringMaterial.SetFloat("_Alpha", 0.8f);
            EditorUtility.SetDirty(ringMaterial);

            var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                RemoveChild(prefabRoot.transform, "MOD_Outline");
                RemoveChild(prefabRoot.transform, "Foot_Ring");

                var model = prefabRoot.transform.Find("MOD_PlayerModel");
                if (model == null)
                {
                    throw new System.InvalidOperationException("MOD_PlayerModel was not found in the player prefab.");
                }

                var source = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (source == null || source.sharedMesh == null)
                {
                    throw new System.InvalidOperationException("A skinned mesh renderer was not found in MOD_PlayerModel.");
                }

                var outlineObject = new GameObject("MOD_Outline");
                outlineObject.transform.SetParent(source.transform.parent, false);
                outlineObject.transform.localPosition = source.transform.localPosition;
                outlineObject.transform.localRotation = source.transform.localRotation;
                outlineObject.transform.localScale = source.transform.localScale;

                var outlineRenderer = outlineObject.AddComponent<SkinnedMeshRenderer>();
                outlineRenderer.sharedMesh = source.sharedMesh;
                outlineRenderer.bones = source.bones;
                outlineRenderer.rootBone = source.rootBone;
                outlineRenderer.localBounds = source.localBounds;
                outlineRenderer.sharedMaterials = new[] { outlineMaterial };
                outlineRenderer.updateWhenOffscreen = true;
                outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
                outlineRenderer.receiveShadows = false;
                outlineRenderer.lightProbeUsage = LightProbeUsage.Off;
                outlineRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

                var ringObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                ringObject.name = "Foot_Ring";
                ringObject.transform.SetParent(prefabRoot.transform, false);
                ringObject.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                ringObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                ringObject.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

                var collider = ringObject.GetComponent<Collider>();
                if (collider != null)
                {
                    Object.DestroyImmediate(collider);
                }

                var ringRenderer = ringObject.GetComponent<MeshRenderer>();
                ringRenderer.sharedMaterial = ringMaterial;
                ringRenderer.shadowCastingMode = ShadowCastingMode.Off;
                ringRenderer.receiveShadows = false;
                ringRenderer.lightProbeUsage = LightProbeUsage.Off;
                ringRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                ringObject.AddComponent<PlayerFootRingPulse>();

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[XiYouJi] Player outline and foot ring applied to Player_ClickMove prefab.");
        }

        private static Material LoadOrCreateMaterial(string path, Shader shader)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            return material;
        }

        private static void EnsureMaterialsFolder()
        {
            const string parent = "Assets/GameContent/03_Gameplay";
            const string folder = "Assets/GameContent/03_Gameplay/Materials";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder(parent, "Materials");
            }
        }

        private static void RemoveChild(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }
}
