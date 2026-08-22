using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using XiYouJi.Gameplay;

namespace XiYouJi.VFX.FluidCloud.Editor
{
    public static class FluidCloudSceneBuilder
    {
        private const string RootFolder = "Assets/GameContent/04_VFX/FluidCloud";
        private const string MaterialsFolder = RootFolder + "/Materials";
        private const string TexturesFolder = RootFolder + "/Textures";
        private const string ScenePath = "Assets/GameContent/01_Scenes/cloud.unity";
        private const string PlayerPrefabPath = "Assets/GameContent/03_Gameplay/Prefabs/Player_ClickMove.prefab";
        private const string ComputePath = RootFolder + "/Shaders/FluidCloud2D.compute";
        private const string NoisePath = TexturesFolder + "/T_FluidCloudNoise3D.asset";

        private static readonly Vector3 CameraOffset = new Vector3(-19.973343f, 14.2849455f, 0.17424583f);
        private static readonly Vector3 CameraEuler = new Vector3(35.6f, 90.5f, 1.53f);

        [MenuItem("Tools/XiYouJi/Build Fluid Cloud Scene")]
        public static void BuildScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Exit Play Mode before rebuilding the fluid cloud scene.");
            }

            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.isDirty)
            {
                throw new InvalidOperationException("Save the active scene before rebuilding the fluid cloud scene.");
            }

            EnsureFolders();

            ComputeShader solver = AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputePath);
            if (solver == null)
            {
                throw new InvalidOperationException("Missing compute shader: " + ComputePath);
            }

            Texture3D noiseTexture = AssetDatabase.LoadAssetAtPath<Texture3D>(NoisePath);
            if (noiseTexture == null)
            {
                throw new InvalidOperationException(
                    "Missing generated 3D noise. Run the previous builder once or restore " + NoisePath);
            }

            Material foundationMaterial = RequireMaterial(MaterialsFolder + "/M_FluidCloudFoundation.mat");
            Material surfaceMaterial = RequireMaterial(MaterialsFolder + "/M_FluidCloudSurface.mat");
            Material volumeMaterial = RequireMaterial(MaterialsFolder + "/M_FluidCloudVolume.mat");
            volumeMaterial.SetTexture("_NoiseTex3D", noiseTexture);
            EditorUtility.SetDirty(volumeMaterial);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigureEnvironment();

            GameObject player = CreatePlayer(
                scene,
                out NavMeshAgent agent,
                out PointClickNavController clickController,
                out FluidCloudInteractor interactor);
            PolygonWalkableArea walkableArea = CreateNavigation(foundationMaterial);
            CameraMovementBounds cameraBounds = CreateCameraBounds();
            Camera camera = CreateCamera(player.transform, cameraBounds);

            clickController.inputCamera = camera;
            clickController.walkableArea = walkableArea;
            clickController.keepAgentInsidePolygon = true;
            clickController.pathValidationSpacing = 0.25f;
            clickController.rotationSharpness = 14f;

            interactor.source = player.transform;
            interactor.navMeshAgent = agent;

            CreateSun();
            CreateFoundation(foundationMaterial);
            CreateFluidCloudRig(solver, interactor, player.transform, surfaceMaterial, volumeMaterial);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Failed to save fluid cloud scene at " + ScenePath);
            }

            BakeNavigation();
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            if (triangulation.vertices == null || triangulation.vertices.Length == 0)
            {
                throw new InvalidOperationException("Fluid cloud NavMesh bake produced no walkable vertices.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("Failed to save the baked fluid cloud NavMesh.");
            }

            AddSceneToBuildSettings(ScenePath);
            Selection.activeGameObject = player;
            SceneView.lastActiveSceneView?.FrameSelected();
            AssetDatabase.SaveAssets();
            Debug.Log("Fluid Cloud gameplay scene built successfully: " + ScenePath);
        }

        private static GameObject CreatePlayer(
            Scene scene,
            out NavMeshAgent agent,
            out PointClickNavController clickController,
            out FluidCloudInteractor interactor)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("Missing player prefab: " + PlayerPrefabPath);
            }

            GameObject player = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (player == null)
            {
                throw new InvalidOperationException("Could not instantiate the player prefab.");
            }

            player.name = "Player_ClickMove";
            player.transform.position = Vector3.zero;
            player.transform.rotation = Quaternion.identity;

            agent = player.GetComponent<NavMeshAgent>();
            clickController = player.GetComponent<PointClickNavController>();
            PlayerAnimationDriver animationDriver = player.GetComponent<PlayerAnimationDriver>();
            if (agent == null || clickController == null || animationDriver == null)
            {
                throw new InvalidOperationException("Player_ClickMove prefab is missing its standard gameplay components.");
            }

            agent.enabled = true;
            clickController.enabled = true;
            animationDriver.enabled = true;

            interactor = player.GetComponent<FluidCloudInteractor>();
            if (interactor == null)
            {
                interactor = player.AddComponent<FluidCloudInteractor>();
            }
            interactor.radius = 1.15f;
            interactor.velocityStrength = 1f;
            interactor.clearanceStrength = 1f;
            return player;
        }

        private static PolygonWalkableArea CreateNavigation(Material groundMaterial)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Cloud Walkable Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(6f, 1f, 6f);
            MeshRenderer groundRenderer = ground.GetComponent<MeshRenderer>();
            groundRenderer.sharedMaterial = groundMaterial;
            groundRenderer.shadowCastingMode = ShadowCastingMode.Off;
            groundRenderer.receiveShadows = false;

            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(ground);
            GameObjectUtility.SetStaticEditorFlags(ground, flags | StaticEditorFlags.NavigationStatic);
            int walkableAreaIndex = NavMesh.GetAreaFromName("Walkable");
            if (walkableAreaIndex >= 0)
            {
                GameObjectUtility.SetNavMeshArea(ground, walkableAreaIndex);
            }

            GameObject polygonObject = new GameObject("Player Walkable Polygon");
            PolygonWalkableArea polygon = polygonObject.AddComponent<PolygonWalkableArea>();
            polygon.SetRectangle(new Vector2(28f, 28f));
            return polygon;
        }

        private static CameraMovementBounds CreateCameraBounds()
        {
            GameObject boundsObject = new GameObject("Camera Movement Bounds");
            boundsObject.transform.position = new Vector3(-11.973343f, 0f, -1.725754f);
            CameraMovementBounds bounds = boundsObject.AddComponent<CameraMovementBounds>();
            bounds.WorldSize = new Vector2(28.3f, 54.8f);
            return bounds;
        }

        private static Camera CreateCamera(Transform player, CameraMovementBounds bounds)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = player.position + CameraOffset;
            cameraObject.transform.rotation = Quaternion.Euler(CameraEuler);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(0.19215687f, 0.3019608f, 0.4745098f, 1f);
            camera.fieldOfView = 44f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            camera.renderingPath = RenderingPath.UsePlayerSettings;
            cameraObject.AddComponent<AudioListener>();

            BoundedDiscoCamera follow = cameraObject.AddComponent<BoundedDiscoCamera>();
            follow.target = player;
            follow.movementBounds = bounds;
            follow.smoothTime = 0.42f;
            follow.maxFollowSpeed = 80f;
            follow.followTargetHeight = true;
            follow.snapOnStart = false;
            follow.CaptureCurrentPose();
            return camera;
        }

        private static void ConfigureEnvironment()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.59f, 0.69f, 0.8f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.51f, 0.64f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.27f, 0.37f, 1f);
            RenderSettings.ambientIntensity = 0.92f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.55f, 0.65f, 0.76f, 1f);
            RenderSettings.fogDensity = 0.015f;
        }

        private static void CreateSun()
        {
            GameObject sunObject = new GameObject("FluidCloud_Sun");
            sunObject.transform.rotation = Quaternion.Euler(38f, -32f, 0f);
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.87f, 0.72f, 1f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.None;
            RenderSettings.sun = sun;
        }

        private static void CreateFoundation(Material material)
        {
            GameObject foundation = GameObject.CreatePrimitive(PrimitiveType.Plane);
            foundation.name = "FluidCloud_Foundation";
            foundation.transform.position = new Vector3(0f, -0.62f, 0f);
            foundation.transform.localScale = new Vector3(50f, 1f, 50f);
            foundation.GetComponent<MeshRenderer>().sharedMaterial = material;
            Collider collider = foundation.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        }

        private static void CreateFluidCloudRig(
            ComputeShader solver,
            FluidCloudInteractor interactor,
            Transform player,
            Material surfaceMaterial,
            Material volumeMaterial)
        {
            GameObject root = new GameObject("FluidCloud_Rig");
            FluidCloudField field = root.AddComponent<FluidCloudField>();
            field.solver = solver;
            field.interactor = interactor;
            field.resolution = 128;
            field.worldSize = 24f;
            field.updateRate = 30f;
            field.pressureIterations = 8;

            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Plane);
            surface.name = "FluidCloud_Surface_Mobile";
            surface.transform.SetParent(root.transform, false);
            surface.transform.position = new Vector3(0f, 0.1f, 0f);
            surface.transform.localScale = new Vector3(40f, 1f, 40f);
            ConfigureCloudRenderer(surface.GetComponent<MeshRenderer>(), surfaceMaterial);
            DestroyCollider(surface);

            GameObject volume = GameObject.CreatePrimitive(PrimitiveType.Cube);
            volume.name = "FluidCloud_Volume_PC";
            volume.transform.SetParent(root.transform, false);
            volume.transform.position = new Vector3(0f, 1f, 0f);
            volume.transform.localScale = new Vector3(24f, 3.2f, 24f);
            ConfigureCloudRenderer(volume.GetComponent<MeshRenderer>(), volumeMaterial);
            DestroyCollider(volume);
            volume.AddComponent<FluidCloudVolumeGate>();
            FluidCloudVolumeFollower follower = volume.AddComponent<FluidCloudVolumeFollower>();
            follower.target = player;
            follower.followHeight = false;
        }

        private static void ConfigureCloudRenderer(MeshRenderer renderer, Material material)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static void DestroyCollider(GameObject gameObject)
        {
            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        }

        private static void BakeNavigation()
        {
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    scenes[i].enabled = true;
                    EditorBuildSettings.scenes = scenes.ToArray();
                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static Material RequireMaterial(string path)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                throw new InvalidOperationException("Missing material: " + path);
            }
            return material;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "GameContent");
            EnsureFolder("Assets/GameContent", "01_Scenes");
            EnsureFolder("Assets/GameContent", "04_VFX");
            EnsureFolder("Assets/GameContent/04_VFX", "FluidCloud");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
