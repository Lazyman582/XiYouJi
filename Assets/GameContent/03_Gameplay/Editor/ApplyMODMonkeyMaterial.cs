using UnityEditor;
using UnityEngine;

public static class ApplyMODMonkeyMaterial
{
    private const string PrefabPath = "Assets/GameContent/03_Gameplay/Prefabs/Player_ClickMove.prefab";
    private const string MaterialPath = "Assets/MOD/monkeytexture/MOD_Monkey.mat";
    private const string AlbedoPath = "Assets/MOD/monkeytexture/b7d82a9d9e94dd990018244f5552f25b.png";
    private const string NormalPath = "Assets/MOD/monkeytexture/e6de39fe5176122ee34d49fe2eba0c29.jpg";
    private const string MetallicPath = "Assets/MOD/monkeytexture/2d4627e78ce5ae34806ff7a28ff4cff5.png";
    private const string OcclusionPath = "Assets/MOD/monkeytexture/007bdbe2dc0c82e338893d42b5aa81bb.jpg";

    [MenuItem("XiYouJi/Apply MOD Monkey Material")]
    public static void Apply()
    {
        ConfigureTexture(NormalPath, TextureImporterType.NormalMap, false);
        ConfigureTexture(MetallicPath, TextureImporterType.Default, false);
        ConfigureTexture(OcclusionPath, TextureImporterType.Default, false);
        ConfigureTexture(AlbedoPath, TextureImporterType.Default, true);

        var shader = Shader.Find("Standard");
        if (shader == null)
        {
            Debug.LogError("MOD material setup: Standard shader was not found.");
            return;
        }

        var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoPath);
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);
        var metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(MetallicPath);
        var occlusion = AssetDatabase.LoadAssetAtPath<Texture2D>(OcclusionPath);

        if (albedo == null || normal == null || metallic == null || occlusion == null)
        {
            Debug.LogError("MOD material setup: one or more monkeytexture assets could not be loaded.");
            return;
        }

        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader)
            {
                name = "MOD_Monkey"
            };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        material.shader = shader;
        material.SetColor("_Color", Color.white);
        material.SetTexture("_MainTex", albedo);
        material.SetTexture("_BumpMap", normal);
        material.SetFloat("_BumpScale", 1f);
        material.EnableKeyword("_NORMALMAP");
        material.SetTexture("_MetallicGlossMap", metallic);
        material.SetFloat("_Metallic", 0.05f);
        material.SetFloat("_Glossiness", 0.35f);
        material.EnableKeyword("_METALLICGLOSSMAP");
        material.SetTexture("_OcclusionMap", occlusion);
        material.SetFloat("_OcclusionStrength", 0.8f);
        material.EnableKeyword("_OCCLUSIONMAP");
        EditorUtility.SetDirty(material);

        var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var renderers = prefabRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length == 0)
            {
                Debug.LogError("MOD material setup: no SkinnedMeshRenderer was found in " + PrefabPath);
                return;
            }

            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    materials = new[] { material };
                }
                else
                {
                    for (var i = 0; i < materials.Length; i++)
                    {
                        materials[i] = material;
                    }
                }

                renderer.sharedMaterials = materials;
                EditorUtility.SetDirty(renderer);
                Debug.Log("MOD material applied to " + renderer.name);
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("MOD monkey material setup complete: " + MaterialPath);
    }

    private static void ConfigureTexture(string path, TextureImporterType textureType, bool sRgb)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning("MOD material setup: texture importer not found for " + path);
            return;
        }

        importer.textureType = textureType;
        importer.sRGBTexture = sRgb;
        importer.SaveAndReimport();
    }
}
