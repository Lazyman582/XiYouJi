using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealisticForestMaterials
{
    /// <summary>
    /// A GPU 3D density-field fog volume for the built-in render pipeline.
    /// Density is advected through a voxel grid by a wind field and deflected
    /// around the tree capsule colliders found in the active scene.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Realistic Forest/Fluid Volumetric Fog")]
    public sealed class FluidFogVolume : MonoBehaviour
    {
        [Header("Simulation volume")]
        public Vector3 volumeCenter = new Vector3(0f, 22f, 0f);
        public Vector3 volumeSize = new Vector3(250f, 70f, 230f);
        [Range(32, 160)] public int gridX = 96;
        [Range(16, 80)] public int gridY = 40;
        [Range(32, 160)] public int gridZ = 96;

        [Header("Wind and fluid density")]
        public Vector3 wind = new Vector3(1.8f, 0.12f, 0.7f);
        [Range(0.25f, 3f)] public float simulationSpeed = 1f;
        [Range(0.001f, 0.25f)] public float baseDensity = 0.032f;
        [Range(0.001f, 0.5f)] public float dissipation = 0.028f;
        [Range(-5f, 5f)] public float buoyancy = 0.16f;
        public float fogHeight = -1f;
        [Range(0.001f, 1f)] public float heightFalloff = 0.085f;
        [Range(0.001f, 0.1f)] public float noiseScale = 0.032f;
        [Range(0f, 1f)] public float noiseStrength = 0.38f;

        [Header("Rendering")]
        public Color fogColor = new Color(0.56f, 0.66f, 0.70f, 1f);
        [Range(0.05f, 5f)] public float densityScale = 1.35f;
        [Range(4, 64)] public int raymarchSteps = 28;
        [Range(1, 4)] public int downsample = 2;
        [Range(-0.85f, 0.85f)] public float anisotropy = 0.48f;
        [Range(0f, 3f)] public float sunScatter = 1.1f;
        [Range(0f, 1f)] public float ambientScatter = 0.38f;
        public Light sunLight;

        [Header("Scene obstacles")]
        [Range(0.25f, 3f)] public float obstacleRadiusScale = 0.8f;
        [Range(4, 64)] public int obstacleGridResolution = 24;

        [Header("Assets")]
        public ComputeShader fluidCompute;
        public Shader volumeShader;

        private Material compositeMaterial;
        [SerializeField] private GameObject volumeObject;
        private MeshRenderer volumeRenderer;
        private RenderTexture densityA;
        private RenderTexture densityB;
        private ComputeBuffer obstacleCenters;
        private ComputeBuffer obstacleHeights;
        private int clearKernel;
        private int advectKernel;
        private bool readIsA = true;
        private bool initialized;
        private float simulationAccumulator;
        private float lastRealtime;
        private float simulationTime;
        private float obstacleRefreshTimer;
        private Camera targetCamera;

        private readonly List<Vector4> centerData = new List<Vector4>();
        private readonly List<Vector4> heightData = new List<Vector4>();

        private Vector3 VolumeMin
        {
            get { return volumeCenter - volumeSize * 0.5f; }
        }

        private void OnEnable()
        {
            targetCamera = GetComponent<Camera>();
            targetCamera.depthTextureMode |= DepthTextureMode.Depth;
            if (volumeShader == null)
                volumeShader = Shader.Find("Hidden/RealisticForest/FluidFogVolumeWorld");
            if (sunLight == null)
                FindSun();

            EnsureResources();
            EnsureVolumeObject();
            RebuildObstacles();
            LinkSunShafts();
            lastRealtime = Time.realtimeSinceStartup;
        }

        private void OnDisable()
        {
            if (volumeRenderer != null)
                volumeRenderer.enabled = false;
            ReleaseResources();
        }

        private void OnDestroy()
        {
            ReleaseResources();
        }

        private void OnValidate()
        {
            gridX = Mathf.Clamp(gridX, 32, 160);
            gridY = Mathf.Clamp(gridY, 16, 80);
            gridZ = Mathf.Clamp(gridZ, 32, 160);
            volumeSize.x = Mathf.Max(1f, volumeSize.x);
            volumeSize.y = Mathf.Max(1f, volumeSize.y);
            volumeSize.z = Mathf.Max(1f, volumeSize.z);
            densityScale = Mathf.Max(0.05f, densityScale);
            raymarchSteps = Mathf.Clamp(raymarchSteps, 4, 64);
            downsample = Mathf.Clamp(downsample, 1, 4);
            if (volumeObject != null)
            {
                volumeObject.transform.position = volumeCenter;
                volumeObject.transform.localScale = volumeSize;
            }
        }

        private void Update()
        {
            if (!enabled)
                return;

            EnsureResources();
            if (!initialized)
                return;

            float now = Time.realtimeSinceStartup;
            float delta = Mathf.Clamp(now - lastRealtime, 0f, 0.08f);
            lastRealtime = now;
            if (delta <= 0f)
                delta = 1f / 60f;

            simulationAccumulator += delta * simulationSpeed;
            obstacleRefreshTimer += delta;
            if (obstacleRefreshTimer > 1.5f)
            {
                obstacleRefreshTimer = 0f;
                RebuildObstacles();
            }

            const float fixedStep = 1f / 30f;
            int steps = 0;
            while (simulationAccumulator >= fixedStep && steps < 3)
            {
                DispatchAdvection(fixedStep);
                simulationAccumulator -= fixedStep;
                steps++;
            }

            // The fog is rendered by the world-space volume mesh, so keep its
            // material bound to the current density buffer and sun settings.
            UpdateCompositeMaterial();
        }

        private void FindSun()
        {
            foreach (var light in FindObjectsOfType<Light>())
            {
                if (light.type == LightType.Directional)
                {
                    sunLight = light;
                    return;
                }
            }
        }

        private void EnsureResources()
        {
            if (fluidCompute == null || !SystemInfo.supportsComputeShaders)
                return;

            if (!initialized || densityA == null || densityB == null ||
                densityA.width != gridX || densityA.height != gridY || densityA.volumeDepth != gridZ)
            {
                ReleaseResources();
                clearKernel = fluidCompute.FindKernel("Clear");
                advectKernel = fluidCompute.FindKernel("Advect");
                densityA = CreateDensityTexture("FluidFogDensityA");
                densityB = CreateDensityTexture("FluidFogDensityB");
                CreateObstacleBuffers();
                ClearDensity(densityA);
                ClearDensity(densityB);
                readIsA = true;
                initialized = true;
            }

            if (compositeMaterial == null && volumeShader != null)
            {
                compositeMaterial = new Material(volumeShader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            EnsureVolumeObject();
        }

        private void EnsureVolumeObject()
        {
            if (volumeObject == null)
            {
                volumeObject = GameObject.Find("Fluid Fog Volume (World Space)");
                if (volumeObject == null)
                {
                    volumeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    volumeObject.name = "Fluid Fog Volume (World Space)";
                    volumeObject.hideFlags = HideFlags.None;
                }

                if (volumeObject.scene != gameObject.scene && gameObject.scene.IsValid() && gameObject.scene.isLoaded)
                    SceneManager.MoveGameObjectToScene(volumeObject, gameObject.scene);

                var collider = volumeObject.GetComponent<Collider>();
                if (collider != null)
                {
                    if (Application.isPlaying)
                        Destroy(collider);
                    else
                        DestroyImmediate(collider);
                }
            }

            volumeRenderer = volumeObject.GetComponent<MeshRenderer>();
            if (volumeRenderer == null)
                volumeRenderer = volumeObject.AddComponent<MeshRenderer>();

            volumeObject.transform.position = volumeCenter;
            volumeObject.transform.localScale = volumeSize;
            volumeRenderer.sharedMaterial = compositeMaterial;
            volumeRenderer.enabled = enabled && gameObject.activeInHierarchy;
        }

        private RenderTexture CreateDensityTexture(string textureName)
        {
            var texture = new RenderTexture(gridX, gridY, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear)
            {
                name = textureName,
                dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
                volumeDepth = gridZ,
                enableRandomWrite = true,
                useMipMap = false,
                autoGenerateMips = false,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.Create();
            return texture;
        }

        private void CreateObstacleBuffers()
        {
            ReleaseBuffer(ref obstacleCenters);
            ReleaseBuffer(ref obstacleHeights);
            obstacleCenters = new ComputeBuffer(1, 16);
            obstacleHeights = new ComputeBuffer(1, 16);
            obstacleCenters.SetData(new[] { Vector4.zero });
            obstacleHeights.SetData(new[] { Vector4.zero });
        }

        private void RebuildObstacles()
        {
            if (obstacleCenters == null || obstacleHeights == null)
                return;

            centerData.Clear();
            heightData.Clear();
            string scenePath = gameObject.scene.path;
            var colliders = FindObjectsOfType<CapsuleCollider>(true);
            var renderers = FindObjectsOfType<Renderer>(true);
            var occupiedCells = new HashSet<Vector3Int>();
            Vector3 min = VolumeMin;
            Vector3 max = volumeCenter + volumeSize * 0.5f;

            foreach (var capsule in colliders)
            {
                if (capsule == null || capsule.gameObject.scene.path != scenePath)
                    continue;

                Bounds bounds = capsule.bounds;
                if (bounds.max.x < min.x || bounds.min.x > max.x ||
                    bounds.max.y < min.y || bounds.min.y > max.y ||
                    bounds.max.z < min.z || bounds.min.z > max.z)
                    continue;

                Vector3 extents = bounds.extents;
                float radius = Mathf.Max(extents.x, extents.z) * 1.15f;
                if (radius < 0.08f)
                    continue;

                centerData.Add(new Vector4(bounds.center.x, bounds.center.y, bounds.center.z, radius));
                heightData.Add(new Vector4(bounds.center.y, Mathf.Max(extents.y, radius * 2f), 0f, 0f));
            }

            // Tree instances in this package use UnityEngine.Tree and usually have no
            // collider. Voxelize their renderer bounds into a coarse obstacle field so
            // the simulated fog is visibly deflected by the whole grove.
            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer.gameObject.scene.path != scenePath)
                    continue;
                if (volumeObject != null && renderer.gameObject == volumeObject)
                    continue;
                if (renderer is ParticleSystemRenderer)
                    continue;

                Bounds bounds = renderer.bounds;
                if (bounds.max.x < min.x || bounds.min.x > max.x ||
                    bounds.max.y < min.y || bounds.min.y > max.y ||
                    bounds.max.z < min.z || bounds.min.z > max.z)
                    continue;

                Vector3Int cell = new Vector3Int(
                    Mathf.FloorToInt((bounds.center.x - min.x) / volumeSize.x * obstacleGridResolution),
                    Mathf.FloorToInt((bounds.center.y - min.y) / volumeSize.y * obstacleGridResolution),
                    Mathf.FloorToInt((bounds.center.z - min.z) / volumeSize.z * obstacleGridResolution));
                cell.x = Mathf.Clamp(cell.x, 0, obstacleGridResolution - 1);
                cell.y = Mathf.Clamp(cell.y, 0, obstacleGridResolution - 1);
                cell.z = Mathf.Clamp(cell.z, 0, obstacleGridResolution - 1);
                if (!occupiedCells.Add(cell))
                    continue;

                float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) * obstacleRadiusScale;
                if (radius < 0.08f)
                    continue;

                centerData.Add(new Vector4(bounds.center.x, bounds.center.y, bounds.center.z, radius));
                heightData.Add(new Vector4(bounds.center.y, Mathf.Max(bounds.extents.y, radius * 2f), 0f, 0f));
            }

            if (centerData.Count == 0)
            {
                centerData.Add(Vector4.zero);
                heightData.Add(Vector4.zero);
            }

            RecreateObstacleBuffer(ref obstacleCenters, centerData);
            RecreateObstacleBuffer(ref obstacleHeights, heightData);
        }

        private static void RecreateObstacleBuffer(ref ComputeBuffer buffer, List<Vector4> data)
        {
            ReleaseBuffer(ref buffer);
            buffer = new ComputeBuffer(data.Count, 16);
            buffer.SetData(data);
        }

        private void ClearDensity(RenderTexture texture)
        {
            fluidCompute.SetInts("_GridSize", gridX, gridY, gridZ);
            fluidCompute.SetTexture(clearKernel, "_DensityWrite", texture);
            fluidCompute.Dispatch(clearKernel, Mathf.CeilToInt(gridX / 8f), Mathf.CeilToInt(gridY / 8f), Mathf.CeilToInt(gridZ / 8f));
        }

        private void DispatchAdvection(float delta)
        {
            RenderTexture read = readIsA ? densityA : densityB;
            RenderTexture write = readIsA ? densityB : densityA;
            Vector3 min = VolumeMin;

            fluidCompute.SetInts("_GridSize", gridX, gridY, gridZ);
            fluidCompute.SetVector("_WorldMin", min);
            fluidCompute.SetVector("_WorldSize", volumeSize);
            fluidCompute.SetVector("_Wind", wind);
            fluidCompute.SetFloat("_DeltaTime", delta);
            fluidCompute.SetFloat("_SimTime", simulationTime);
            fluidCompute.SetFloat("_BaseDensity", baseDensity);
            fluidCompute.SetFloat("_Dissipation", dissipation);
            fluidCompute.SetFloat("_Buoyancy", buoyancy);
            fluidCompute.SetFloat("_FogHeight", fogHeight);
            fluidCompute.SetFloat("_HeightFalloff", heightFalloff);
            fluidCompute.SetFloat("_NoiseScale", noiseScale);
            fluidCompute.SetFloat("_NoiseStrength", noiseStrength);
            fluidCompute.SetInt("_ObstacleCount", centerData.Count);
            fluidCompute.SetBuffer(advectKernel, "_ObstacleCenters", obstacleCenters);
            fluidCompute.SetBuffer(advectKernel, "_ObstacleHeights", obstacleHeights);
            fluidCompute.SetTexture(advectKernel, "_DensityRead", read);
            fluidCompute.SetTexture(advectKernel, "_DensityWrite", write);
            fluidCompute.Dispatch(advectKernel, Mathf.CeilToInt(gridX / 8f), Mathf.CeilToInt(gridY / 8f), Mathf.CeilToInt(gridZ / 8f));

            readIsA = !readIsA;
            simulationTime += delta;
        }

        private void UpdateCompositeMaterial()
        {
            if (compositeMaterial == null)
                return;

            EnsureVolumeObject();

            Vector3 min = VolumeMin;
            Vector3 max = volumeCenter + volumeSize * 0.5f;
            compositeMaterial.SetTexture("_DensityTex", readIsA ? densityA : densityB);
            compositeMaterial.SetVector("_VolumeMin", min);
            compositeMaterial.SetVector("_VolumeMax", max);
            compositeMaterial.SetColor("_FogColor", fogColor);
            compositeMaterial.SetFloat("_DensityScale", densityScale);
            compositeMaterial.SetFloat("_RaymarchSteps", raymarchSteps);
            compositeMaterial.SetFloat("_Anisotropy", anisotropy);
            compositeMaterial.SetFloat("_SunScatter", sunScatter);
            compositeMaterial.SetFloat("_AmbientScatter", ambientScatter);
            compositeMaterial.SetFloat("_SimTime", simulationTime);

            if (sunLight != null)
            {
                compositeMaterial.SetVector("_SunDirection", -sunLight.transform.forward);
                Color lightColor = sunLight.color * sunLight.intensity;
                // Preserve the directional light's intensity while preventing a
                // strongly tinted legacy light from turning participating media
                // magenta/green after the old image effects are applied.
                float luminance = lightColor.r * 0.2126f + lightColor.g * 0.7152f + lightColor.b * 0.0722f;
                Color balancedLight = Color.Lerp(new Color(luminance, luminance, luminance, 1f), lightColor, 0.22f);
                compositeMaterial.SetColor("_SunColor", balancedLight);
            }
            else
            {
                compositeMaterial.SetVector("_SunDirection", Vector3.down);
                compositeMaterial.SetColor("_SunColor", Color.white);
            }

            LinkSunShafts();
        }

        private void LinkSunShafts()
        {
            if (targetCamera == null || sunLight == null)
                return;

            var behaviours = targetCamera.GetComponents<MonoBehaviour>();
            foreach (var behaviour in behaviours)
            {
                if (behaviour == null || behaviour.GetType().Name != "SunShafts")
                    continue;

                FieldInfo sunTransformField = behaviour.GetType().GetField(
                    "sunTransform", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (sunTransformField != null && sunTransformField.FieldType == typeof(Transform))
                    sunTransformField.SetValue(behaviour, sunLight.transform);
            }
        }

        private void ReleaseResources()
        {
            initialized = false;
            ReleaseTexture(ref densityA);
            ReleaseTexture(ref densityB);
            ReleaseBuffer(ref obstacleCenters);
            ReleaseBuffer(ref obstacleHeights);
            if (compositeMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(compositeMaterial);
                else
                    DestroyImmediate(compositeMaterial);
                compositeMaterial = null;
            }
        }

        private static void ReleaseTexture(ref RenderTexture texture)
        {
            if (texture == null)
                return;
            texture.Release();
            if (Application.isPlaying)
                Destroy(texture);
            else
                DestroyImmediate(texture);
            texture = null;
        }

        private static void ReleaseBuffer(ref ComputeBuffer buffer)
        {
            if (buffer == null)
                return;
            buffer.Release();
            buffer = null;
        }
    }
}
