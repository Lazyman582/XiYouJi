using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SetupMODPlayerAnimation
{
    private const string FbxPath = "Assets/MOD/20260814143029_f10db1c4.fbx";
    private const string PrefabPath = "Assets/GameContent/03_Gameplay/Prefabs/Player_ClickMove.prefab";
    private const string ControllerPath = "Assets/GameContent/03_Gameplay/Animations/MOD_Player.controller";

    [MenuItem("XiYouJi/Setup MOD Player Animation")]
    public static void Setup()
    {
        var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError("MOD animation setup: FBX importer not found at " + FbxPath);
            return;
        }

        var importedDefinitions = importer.clipAnimations;
        if (importedDefinitions == null || importedDefinitions.Length == 0)
        {
            importedDefinitions = importer.defaultClipAnimations;
        }

        if (importedDefinitions == null || importedDefinitions.Length == 0)
        {
            Debug.LogError("MOD animation setup: no animation takes were found in " + FbxPath);
            return;
        }

        for (var i = 0; i < importedDefinitions.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(importedDefinitions[i].name))
            {
                importedDefinitions[i].name = i == 0 ? "MOD_Idle" : "MOD_Clip_" + i;
            }

            importedDefinitions[i].loopTime = true;
            importedDefinitions[i].loopPose = true;
        }

        if (string.IsNullOrWhiteSpace(importedDefinitions[0].name))
        {
            importedDefinitions[0].name = "MOD_Idle";
        }

        importer.clipAnimations = importedDefinitions;
        importer.SaveAndReimport();

        var clips = AssetDatabase.LoadAllAssetsAtPath(FbxPath)
            .OfType<AnimationClip>()
            .Where(c => c != null && c.length > 0.001f && !c.name.StartsWith("__preview__"))
            .ToArray();

        if (clips.Length == 0)
        {
            Debug.LogError("MOD animation setup: FBX reimported, but no AnimationClip sub-assets were available.");
            return;
        }

        EnsureFolder("Assets/GameContent/03_Gameplay/Animations");

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        var stateMachine = controller.layers[0].stateMachine;
        AnimatorState defaultState = null;

        for (var i = 0; i < clips.Length; i++)
        {
            var stateName = i == 0 ? "Walk" : "MOD_" + Sanitize(clips[i].name);
            var existing = stateMachine.states.FirstOrDefault(s => s.state.name == stateName);
            var state = existing.state != null ? existing.state : stateMachine.AddState(stateName);
            state.motion = clips[i];
            state.writeDefaultValues = false;

            if (i == 0)
            {
                defaultState = state;
            }

            Debug.Log("MOD animation clip " + i + ": " + clips[i].name + " (" + clips[i].length.ToString("0.###") + "s)");
        }

        if (defaultState != null)
        {
            stateMachine.defaultState = defaultState;
        }

        AssetDatabase.SaveAssets();

        var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var model = prefabRoot.transform.Find("MOD_PlayerModel");
            if (model == null)
            {
                Debug.LogError("MOD animation setup: MOD_PlayerModel was not found in " + PrefabPath);
                return;
            }

            var animator = model.GetComponent<Animator>();
            if (animator == null)
            {
                animator = model.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var avatar = AssetDatabase.LoadAllAssetsAtPath(FbxPath).OfType<Avatar>().FirstOrDefault();
            if (avatar != null)
            {
                animator.avatar = avatar;
            }

            EditorUtility.SetDirty(model.gameObject);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("MOD animation setup complete. Controller: " + ControllerPath + "; default state: " + (defaultState != null ? defaultState.name : "<none>"));
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        var parts = path.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static string Sanitize(string value)
    {
        var chars = value.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
        var result = new string(chars).Trim('_');
        return string.IsNullOrEmpty(result) ? "Clip" : result;
    }
}
