using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace XiYouJi.VFX.FluidCloud.Editor
{
    public static class FluidCloudPrototypeBuilder
    {
        private const string RootFolder = "Assets/GameContent/04_VFX/FluidCloud";
        private const string MaterialsFolder = RootFolder + "/Materials";
        private const string TexturesFolder = RootFolder + "/Textures";
        private const string ScenesFolder = RootFolder + "/Scenes";
        private const string ScenePath = ScenesFolder + "/FluidCloud_Prototype.unity";
        private const string RecoveryScenePath = ScenesFolder + "/PreFluidCloud_Recovery.unity";
        private const string PlayerPrefabPath = "Assets/GameContent/03_Gameplay/Prefabs/Player_ClickMove.prefab";
        private const string ComputePath = RootFolder + "/Shaders/FluidCloud2D.compute";
        private const string NoisePath = TexturesFolder + "/T_FluidCloudNoise3D.asset";

        [MenuItem("Tools/XiYouJi/Build Fluid Cloud Prototype")]
        public static void BuildPrototype()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Exit Play Mode before rebuilding the fluid cloud prototype.");
            }

            EnsureFolders();
            PreserveDirtyScene();

            ComputeShader solver = AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputePath);
            if (solver == null)
            {
                throw new InvalidOperationException("Missing compute shader: " + ComputePath);
            }

            Shader surfaceShader = RequireShader("XiYouJi/VFX/Fluid Cloud Surface");
            Shader volumeShader = RequireShader("XiYouJi/VFX/Fluid Cloud Volume");
            Shader standardShader = RequireShader("Standard");
            Texture3D noiseTexture = CreateOrLoadNoiseTexture();

            Material foundationMaterial = CreateOrLoadMaterial(
                MaterialsFolder + "/M_FluidCloudFoundation.mat",
                standardShader);
            foundationMaterial.SetColor("_Color", new Color(0.28f, 0.38f, 0.51f, 1f));
            foundationMaterial.SetFloat("_Glossiness", 0f);
            foundationMaterial.SetFloat("_Metallic", 0f);

            Material surfaceMaterial = CreateOrLoadMaterial(
                MaterialsFolder + "/M_FluidCloudSurface.mat",
                surfaceShader);
            ConfigureSurfaceMaterial(surfaceMaterial);

            Material volumeMaterial = CreateOrLoadMaterial(
                MaterialsFolder + "/M_FluidCloudVolume.mat",
                volumeShader);
            ConfigureVolumeMaterial(volumeMaterial, noiseTexture);
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigureEnvironment();

            GameObject player = CreatePlayer(scene, out FluidCloudInteractor interactor);
            Camera camera = CreateCamera(player.transform);
            FluidCloudDemoWalker walker = player.GetComponent<FluidCloudDemoWalker>();
            walker.referenceCamera = camera;

            CreateSun();
            CreateFoundation(foundationMaterial);
            CreateFluidCloudRig(solver, interactor, surfaceMaterial, volumeMaterial);
            CreateDemoUi();

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Failed to save fluid cloud scene at " + ScenePath);
            }

            Selection.activeGameObject = player;
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null) sceneView.FrameSelected();

            AssetDatabase.SaveAssets();
            Debug.Log("Fluid Cloud Prototype built successfully: " + ScenePath);
        }

        private static void PreserveDirtyScene()
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isDirty)
            {
                return;
            }

            bool saved = EditorSceneManager.SaveScene(activeScene, RecoveryScenePath, true);
            if (!saved)
            {
                throw new InvalidOperationException(
                    "The active scene has unsaved changes and could not be preserved at " + RecoveryScenePath);
            }
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

        private static GameObject CreatePlayer(Scene scene, out FluidCloudInteractor interactor)
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

            player.name = "FluidCloud_DemoPlayer";
            player.transform.position = Vector3.zero;
            player.transform.rotation = Quaternion.identity;

            NavMeshAgent agent = player.GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            MonoBehaviour[] behaviours = player.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null) continue;
                string typeName = behaviour.GetType().Name;
                if (typeName == "PointClickNavController" || typeName == "PlayerAnimationDriver")
                {
                    behaviour.enabled = false;
                }
            }

            player.AddComponent<FluidCloudDemoWalker>();
            interactor = player.AddComponent<FluidCloudInteractor>();
            interactor.source = player.transform;
            interactor.navMeshAgent = agent;
            interactor.radius = 1.15f;
            interactor.velocityStrength = 1f;
            interactor.clearanceStrength = 1f;
            return player;
        }

        private static Camera CreateCamera(Transform target)
        {
            GameObject cameraObject = new GameObject("FluidCloud_Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = target.position + new Vector3(0f, 8.5f, -15.5f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.48f, 0.59f, 0.72f, 1f);
            camera.fieldOfView = 52f;
            camera.nearClipPlane = 0.15f;
            camera.farClipPlane = 160f;
            camera.allowHDR = true;
            camera.renderingPath = RenderingPath.Forward;

            FluidCloudDemoCamera follow = cameraObject.AddComponent<FluidCloudDemoCamera>();
            follow.target = target;
            cameraObject.transform.LookAt(target.position + follow.lookOffset);
            return camera;
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
            DestroyCollider(foundation);
        }

        private static void CreateFluidCloudRig(
            ComputeShader solver,
            FluidCloudInteractor interactor,
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
            field.velocityHalfLife = 0.9f;
            field.densityHalfLife = 3.4f;
            field.vorticityStrength = 3.2f;
            field.velocityInjection = 3.8f;

            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Plane);
            surface.name = "FluidCloud_Surface_Mobile";
            surface.transform.SetParent(root.transform, false);
            surface.transform.position = new Vector3(0f, 0.1f, 0f);
            surface.transform.localScale = new Vector3(40f, 1f, 40f);
            ConfigureRenderer(surface.GetComponent<MeshRenderer>(), surfaceMaterial);
            DestroyCollider(surface);

            GameObject volume = GameObject.CreatePrimitive(PrimitiveType.Cube);
            volume.name = "FluidCloud_Volume_PC";
            volume.transform.SetParent(root.transform, false);
            volume.transform.position = new Vector3(0f, 1.0f, 0f);
            volume.transform.localScale = new Vector3(24f, 3.2f, 24f);
            ConfigureRenderer(volume.GetComponent<MeshRenderer>(), volumeMaterial);
            DestroyCollider(volume);
            volume.AddComponent<FluidCloudVolumeGate>();
        }

        private static void ConfigureRenderer(MeshRenderer renderer, Material material)
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

        private static void CreateDemoUi()
        {
            GameObject ui = new GameObject("FluidCloud_DemoUI");
            ui.AddComponent<FluidCloudDemoUI>();
        }

        private static Material CreateOrLoadMaterial(string path, Shader shader)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            return material;
        }

        private static void ConfigureSurfaceMaterial(Material material)
        {
            material.SetColor("_TopColor", new Color(0.88f, 0.94f, 1f, 1f));
            material.SetColor("_ShadowColor", new Color(0.32f, 0.43f, 0.57f, 1f));
            material.SetFloat("_Opacity", 0.76f);
            material.SetFloat("_BaseCoverage", 0.48f);
            material.SetFloat("_NoiseScale", 0.082f);
            material.SetFloat("_Threshold", 0.36f);
            material.SetFloat("_Softness", 0.3f);
            material.SetVector("_Wind", new Vector4(0.016f, 0.006f, 0f, 0f));
            material.SetFloat("_FlowDistortion", 0.08f);
            material.SetFloat("_FlowDisplacement", 0.035f);
            material.SetFloat("_ClearStrength", 0.97f);
            material.SetFloat("_HeightAmplitude", 0.16f);
            material.SetFloat("_DistanceFadeStart", 60f);
            material.SetFloat("_DistanceFadeEnd", 120f);
            EditorUtility.SetDirty(material);
        }

        private static void ConfigureVolumeMaterial(Material material, Texture3D noiseTexture)
        {
            material.SetTexture("_NoiseTex3D", noiseTexture);
            material.SetColor("_TopColor", new Color(0.91f, 0.96f, 1f, 1f));
            material.SetColor("_ShadowColor", new Color(0.29f, 0.4f, 0.55f, 1f));
            material.SetFloat("_Density", 3.1f);
            material.SetFloat("_Threshold", 0.38f);
            material.SetFloat("_Softness", 0.24f);
            material.SetFloat("_NoiseScale", 1.8f);
            material.SetVector("_Wind", new Vector4(0.018f, 0.004f, 0.009f, 0f));
            material.SetFloat("_FlowDistortion", 0.045f);
            material.SetFloat("_ClearStrength", 0.96f);
            material.SetFloat("_StepCount", 22f);
            EditorUtility.SetDirty(material);
        }

        private static Texture3D CreateOrLoadNoiseTexture()
        {
            Texture3D texture = AssetDatabase.LoadAssetAtPath<Texture3D>(NoisePath);
            if (texture != null) return texture;

            const int size = 48;
            Color[] colors = new Color[size * size * size];
            int index = 0;
            for (int z = 0; z < size; z++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        Vector3 uv = new Vector3(x, y, z) / size;
                        float coarse = Worley(uv, 4, 17);
                        float medium = Worley(uv, 8, 43);
                        float detail = Worley(uv, 13, 89);
                        float density = Mathf.Clamp01(coarse * 0.58f + medium * 0.3f + detail * 0.12f);
                        density = Mathf.SmoothStep(0f, 1f, density);
                        colors[index++] = new Color(density, medium, detail, 1f);
                    }
                }
            }

            texture = new Texture3D(size, size, size, TextureFormat.RGBA32, true)
            {
                name = "T_FluidCloudNoise3D",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 0
            };
            texture.SetPixels(colors);
            texture.Apply(true, false);
            AssetDatabase.CreateAsset(texture, NoisePath);
            return texture;
        }

        private static float Worley(Vector3 uv, int cellCount, int seed)
        {
            Vector3 position = uv * cellCount;
            int cellX = Mathf.FloorToInt(position.x);
            int cellY = Mathf.FloorToInt(position.y);
            int cellZ = Mathf.FloorToInt(position.z);
            float minimumDistanceSquared = float.MaxValue;

            for (int offsetZ = -1; offsetZ <= 1; offsetZ++)
            {
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        int candidateX = cellX + offsetX;
                        int candidateY = cellY + offsetY;
                        int candidateZ = cellZ + offsetZ;
                        int wrappedX = PositiveModulo(candidateX, cellCount);
                        int wrappedY = PositiveModulo(candidateY, cellCount);
                        int wrappedZ = PositiveModulo(candidateZ, cellCount);
                        Vector3 random = Hash3(wrappedX, wrappedY, wrappedZ, seed);
                        Vector3 featurePoint = new Vector3(candidateX, candidateY, candidateZ) + random;
                        float distanceSquared = (featurePoint - position).sqrMagnitude;
                        minimumDistanceSquared = Mathf.Min(minimumDistanceSquared, distanceSquared);
                    }
                }
            }

            float normalizedDistance = Mathf.Clamp01(Mathf.Sqrt(minimumDistanceSquared) / 1.05f);
            return 1f - normalizedDistance;
        }

        private static Vector3 Hash3(int x, int y, int z, int seed)
        {
            return new Vector3(
                Hash01(x, y, z, seed),
                Hash01(x, y, z, seed + 1013),
                Hash01(x, y, z, seed + 2027));
        }

        private static float Hash01(int x, int y, int z, int seed)
        {
            unchecked
            {
                uint hash = (uint)(x * 374761393 + y * 668265263 + z * 2147483647 + seed * 1274126177);
                hash = (hash ^ (hash >> 13)) * 1274126177u;
                hash ^= hash >> 16;
                return (hash & 0x00FFFFFFu) / 16777215f;
            }
        }

        private static int PositiveModulo(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }

        private static Shader RequireShader(string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                throw new InvalidOperationException("Missing shader: " + shaderName);
            }
            return shader;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "GameContent");
            EnsureFolder("Assets/GameContent", "04_VFX");
            EnsureFolder("Assets/GameContent/04_VFX", "FluidCloud");
            EnsureFolder(RootFolder, "Materials");
            EnsureFolder(RootFolder, "Textures");
            EnsureFolder(RootFolder, "Scenes");
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
