using System;
using UnityEngine;

namespace XiYouJi.VFX.FluidCloud
{
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    public sealed class FluidCloudField : MonoBehaviour
    {
        private static readonly int VelocityGlobalId = Shader.PropertyToID("_FluidCloudVelocityTex");
        private static readonly int DensityGlobalId = Shader.PropertyToID("_FluidCloudDensityTex");
        private static readonly int CenterSizeGlobalId = Shader.PropertyToID("_FluidCloudCenterSize");
        private static readonly int ActiveGlobalId = Shader.PropertyToID("_FluidCloudActive");
        private static readonly int InteractorGlobalId = Shader.PropertyToID("_FluidCloudInteractor");

        private static readonly int ResolutionId = Shader.PropertyToID("_Resolution");
        private static readonly int DeltaTimeId = Shader.PropertyToID("_DeltaTime");
        private static readonly int WorldSizeId = Shader.PropertyToID("_WorldSize");
        private static readonly int CenterDeltaUvId = Shader.PropertyToID("_CenterDeltaUV");
        private static readonly int VelocityHalfLifeId = Shader.PropertyToID("_VelocityHalfLife");
        private static readonly int DensityHalfLifeId = Shader.PropertyToID("_DensityHalfLife");
        private static readonly int VorticityStrengthId = Shader.PropertyToID("_VorticityStrength");
        private static readonly int FieldCenterId = Shader.PropertyToID("_FieldCenter");
        private static readonly int SegmentStartId = Shader.PropertyToID("_SegmentStart");
        private static readonly int SegmentEndId = Shader.PropertyToID("_SegmentEnd");
        private static readonly int InjectionVelocityId = Shader.PropertyToID("_InjectionVelocity");
        private static readonly int WakeRadiusId = Shader.PropertyToID("_WakeRadius");
        private static readonly int VelocityInjectionId = Shader.PropertyToID("_VelocityInjection");
        private static readonly int DensityInjectionId = Shader.PropertyToID("_DensityInjection");
        private static readonly int StampStrengthId = Shader.PropertyToID("_StampStrength");
        private static readonly int ReadTextureId = Shader.PropertyToID("_ReadTexture");
        private static readonly int VelocityTextureId = Shader.PropertyToID("_VelocityTexture");
        private static readonly int PressureTextureId = Shader.PropertyToID("_PressureTexture");
        private static readonly int DivergenceTextureId = Shader.PropertyToID("_DivergenceTexture");
        private static readonly int CurlTextureId = Shader.PropertyToID("_CurlTexture");
        private static readonly int WriteTextureId = Shader.PropertyToID("_WriteTexture");
        private static readonly int VelocityRwId = Shader.PropertyToID("_VelocityRW");
        private static readonly int DensityRwId = Shader.PropertyToID("_DensityRW");

        [Header("References")]
        public ComputeShader solver;
        public FluidCloudInteractor interactor;

        [Header("Grid")]
        [Range(64, 256)] public int resolution = 128;
        [Min(8f)] public float worldSize = 24f;
        [Range(10f, 60f)] public float updateRate = 30f;
        [Range(2, 24)] public int pressureIterations = 8;

        [Header("Simulation")]
        [Min(0.05f)] public float velocityHalfLife = 0.9f;
        [Min(0.1f)] public float densityHalfLife = 3.2f;
        [Range(0f, 12f)] public float vorticityStrength = 3.2f;
        [Min(0f)] public float velocityInjection = 3.8f;
        [Range(0f, 1f)] public float densityInjection = 1f;
        [Min(0f)] public float minimumSpeed = 0.12f;
        [Min(0.1f)] public float fullStrengthSpeed = 5f;

        public RenderTexture VelocityTexture => velocityRead;
        public RenderTexture DensityTexture => densityRead;
        public bool IsRunning { get; private set; }

        private RenderTexture velocityRead;
        private RenderTexture velocityWrite;
        private RenderTexture densityRead;
        private RenderTexture densityWrite;
        private RenderTexture pressureRead;
        private RenderTexture pressureWrite;
        private RenderTexture divergence;
        private RenderTexture curl;

        private int clearKernel;
        private int advectVelocityKernel;
        private int injectVelocityKernel;
        private int curlKernel;
        private int vorticityKernel;
        private int divergenceKernel;
        private int pressureKernel;
        private int projectKernel;
        private int advectDensityKernel;
        private int injectDensityKernel;
        private int threadGroups;

        private Vector2 currentCenter;
        private Vector2 lastStampedPosition;
        private bool trackingInitialized;
        private float updateAccumulator;

        private void OnEnable()
        {
            Shader.SetGlobalFloat(ActiveGlobalId, 0f);
            if (Application.isPlaying)
            {
                Initialize();
            }
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying || !IsRunning)
            {
                return;
            }

            if (interactor == null)
            {
                PublishGlobals();
                return;
            }

            Vector2 targetPosition = interactor.PositionXZ;
            if (!trackingInitialized)
            {
                currentCenter = SnapCenter(targetPosition);
                lastStampedPosition = targetPosition;
                trackingInitialized = true;
                PublishGlobals();
                return;
            }

            updateAccumulator += Time.deltaTime;
            float interval = 1f / Mathf.Max(1f, updateRate);
            if (updateAccumulator < interval)
            {
                PublishGlobals();
                return;
            }

            float simulationDelta = Mathf.Min(updateAccumulator, 0.1f);
            updateAccumulator = 0f;

            Vector2 segment = targetPosition - lastStampedPosition;
            if (segment.magnitude > worldSize * 0.4f)
            {
                ClearAllTextures();
                currentCenter = SnapCenter(targetPosition);
                lastStampedPosition = targetPosition;
                PublishGlobals();
                return;
            }

            Vector2 previousCenter = currentCenter;
            currentCenter = SnapCenter(targetPosition);
            Vector2 centerDeltaUv = (currentCenter - previousCenter) / worldSize;

            float speed = Mathf.Max(interactor.VelocityXZ.magnitude, segment.magnitude / Mathf.Max(simulationDelta, 0.0001f));
            float stampStrength = speed < minimumSpeed
                ? 0f
                : Mathf.Clamp01(speed / Mathf.Max(minimumSpeed, fullStrengthSpeed));

            SetCommonParameters(simulationDelta, centerDeltaUv, targetPosition, stampStrength);
            Simulate(stampStrength);

            lastStampedPosition = targetPosition;
            PublishGlobals();
        }

        private void OnDisable()
        {
            Shader.SetGlobalFloat(ActiveGlobalId, 0f);
            ReleaseResources();
        }

        private void OnDestroy()
        {
            ReleaseResources();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && IsRunning)
            {
                ClearAllTextures();
                trackingInitialized = false;
            }
        }

        private void Initialize()
        {
            ReleaseResources();

            if (!SystemInfo.supportsComputeShaders)
            {
                Debug.LogWarning("FluidCloudField disabled because this device does not support compute shaders.", this);
                return;
            }

            if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
            {
                Debug.LogWarning("FluidCloudField disabled because ARGBHalf random-write textures are unavailable.", this);
                return;
            }

            if (solver == null)
            {
                Debug.LogError("FluidCloudField requires a FluidCloud2D compute shader reference.", this);
                return;
            }

            if (interactor == null)
            {
                interactor = FindObjectOfType<FluidCloudInteractor>();
            }

            try
            {
                clearKernel = solver.FindKernel("ClearTexture");
                advectVelocityKernel = solver.FindKernel("AdvectVelocity");
                injectVelocityKernel = solver.FindKernel("InjectVelocity");
                curlKernel = solver.FindKernel("ComputeCurl");
                vorticityKernel = solver.FindKernel("ApplyVorticity");
                divergenceKernel = solver.FindKernel("ComputeDivergence");
                pressureKernel = solver.FindKernel("JacobiPressure");
                projectKernel = solver.FindKernel("ProjectVelocity");
                advectDensityKernel = solver.FindKernel("AdvectDensity");
                injectDensityKernel = solver.FindKernel("InjectDensity");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                return;
            }

            resolution = Mathf.Clamp(resolution, 64, 256);
            threadGroups = Mathf.CeilToInt(resolution / 8f);
            velocityRead = CreateTexture("Fluid Cloud Velocity A");
            velocityWrite = CreateTexture("Fluid Cloud Velocity B");
            densityRead = CreateTexture("Fluid Cloud Density A");
            densityWrite = CreateTexture("Fluid Cloud Density B");
            pressureRead = CreateTexture("Fluid Cloud Pressure A");
            pressureWrite = CreateTexture("Fluid Cloud Pressure B");
            divergence = CreateTexture("Fluid Cloud Divergence");
            curl = CreateTexture("Fluid Cloud Curl");

            solver.SetInt(ResolutionId, resolution);
            solver.SetFloat(WorldSizeId, worldSize);
            ClearAllTextures();
            trackingInitialized = false;
            updateAccumulator = 0f;
            IsRunning = true;
            Shader.SetGlobalFloat(ActiveGlobalId, 1f);
        }

        private void Simulate(float stampStrength)
        {
            solver.SetTexture(advectVelocityKernel, ReadTextureId, velocityRead);
            solver.SetTexture(advectVelocityKernel, WriteTextureId, velocityWrite);
            Dispatch(advectVelocityKernel);
            Swap(ref velocityRead, ref velocityWrite);

            if (stampStrength > 0f)
            {
                solver.SetTexture(injectVelocityKernel, VelocityRwId, velocityRead);
                Dispatch(injectVelocityKernel);
            }

            solver.SetTexture(curlKernel, VelocityTextureId, velocityRead);
            solver.SetTexture(curlKernel, WriteTextureId, curl);
            Dispatch(curlKernel);

            solver.SetTexture(vorticityKernel, CurlTextureId, curl);
            solver.SetTexture(vorticityKernel, VelocityRwId, velocityRead);
            Dispatch(vorticityKernel);

            solver.SetTexture(divergenceKernel, VelocityTextureId, velocityRead);
            solver.SetTexture(divergenceKernel, WriteTextureId, divergence);
            Dispatch(divergenceKernel);

            ClearTexture(pressureRead);
            ClearTexture(pressureWrite);
            for (int i = 0; i < pressureIterations; i++)
            {
                solver.SetTexture(pressureKernel, PressureTextureId, pressureRead);
                solver.SetTexture(pressureKernel, DivergenceTextureId, divergence);
                solver.SetTexture(pressureKernel, WriteTextureId, pressureWrite);
                Dispatch(pressureKernel);
                Swap(ref pressureRead, ref pressureWrite);
            }

            solver.SetTexture(projectKernel, VelocityTextureId, velocityRead);
            solver.SetTexture(projectKernel, PressureTextureId, pressureRead);
            solver.SetTexture(projectKernel, WriteTextureId, velocityWrite);
            Dispatch(projectKernel);
            Swap(ref velocityRead, ref velocityWrite);

            solver.SetTexture(advectDensityKernel, ReadTextureId, densityRead);
            solver.SetTexture(advectDensityKernel, VelocityTextureId, velocityRead);
            solver.SetTexture(advectDensityKernel, WriteTextureId, densityWrite);
            Dispatch(advectDensityKernel);
            Swap(ref densityRead, ref densityWrite);

            if (stampStrength > 0f)
            {
                solver.SetTexture(injectDensityKernel, DensityRwId, densityRead);
                Dispatch(injectDensityKernel);
            }
        }

        private void SetCommonParameters(
            float simulationDelta,
            Vector2 centerDeltaUv,
            Vector2 targetPosition,
            float stampStrength)
        {
            solver.SetInt(ResolutionId, resolution);
            solver.SetFloat(DeltaTimeId, simulationDelta);
            solver.SetFloat(WorldSizeId, worldSize);
            solver.SetVector(CenterDeltaUvId, new Vector4(centerDeltaUv.x, centerDeltaUv.y, 0f, 0f));
            solver.SetFloat(VelocityHalfLifeId, velocityHalfLife);
            solver.SetFloat(DensityHalfLifeId, densityHalfLife);
            solver.SetFloat(VorticityStrengthId, vorticityStrength);
            solver.SetVector(FieldCenterId, new Vector4(currentCenter.x, currentCenter.y, 0f, 0f));
            solver.SetVector(SegmentStartId, new Vector4(lastStampedPosition.x, lastStampedPosition.y, 0f, 0f));
            solver.SetVector(SegmentEndId, new Vector4(targetPosition.x, targetPosition.y, 0f, 0f));
            solver.SetVector(
                InjectionVelocityId,
                new Vector4(interactor.VelocityXZ.x, interactor.VelocityXZ.y, 0f, 0f));
            solver.SetFloat(WakeRadiusId, interactor.radius);
            solver.SetFloat(VelocityInjectionId, velocityInjection * interactor.velocityStrength);
            solver.SetFloat(DensityInjectionId, densityInjection * interactor.clearanceStrength);
            solver.SetFloat(StampStrengthId, stampStrength);
        }

        private RenderTexture CreateTexture(string textureName)
        {
            RenderTexture texture = new RenderTexture(
                resolution,
                resolution,
                0,
                RenderTextureFormat.ARGBHalf,
                RenderTextureReadWrite.Linear)
            {
                name = textureName,
                enableRandomWrite = true,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false,
                antiAliasing = 1,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.Create();
            return texture;
        }

        private void ClearAllTextures()
        {
            if (solver == null || velocityRead == null)
            {
                return;
            }

            ClearTexture(velocityRead);
            ClearTexture(velocityWrite);
            ClearTexture(densityRead);
            ClearTexture(densityWrite);
            ClearTexture(pressureRead);
            ClearTexture(pressureWrite);
            ClearTexture(divergence);
            ClearTexture(curl);
        }

        private void ClearTexture(RenderTexture texture)
        {
            solver.SetTexture(clearKernel, WriteTextureId, texture);
            Dispatch(clearKernel);
        }

        private void Dispatch(int kernel)
        {
            solver.Dispatch(kernel, threadGroups, threadGroups, 1);
        }

        private void PublishGlobals()
        {
            if (!IsRunning || velocityRead == null || densityRead == null)
            {
                return;
            }

            Shader.SetGlobalTexture(VelocityGlobalId, velocityRead);
            Shader.SetGlobalTexture(DensityGlobalId, densityRead);
            Shader.SetGlobalVector(
                CenterSizeGlobalId,
                new Vector4(currentCenter.x, currentCenter.y, worldSize, 1f / worldSize));
            if (interactor != null)
            {
                Shader.SetGlobalVector(
                    InteractorGlobalId,
                    new Vector4(interactor.PositionXZ.x, interactor.PositionXZ.y, interactor.radius, 1f));
            }
            Shader.SetGlobalFloat(ActiveGlobalId, 1f);
        }

        private Vector2 SnapCenter(Vector2 position)
        {
            float worldTexel = worldSize / Mathf.Max(1, resolution);
            return new Vector2(
                Mathf.Round(position.x / worldTexel) * worldTexel,
                Mathf.Round(position.y / worldTexel) * worldTexel);
        }

        private void ReleaseResources()
        {
            IsRunning = false;
            ReleaseTexture(ref velocityRead);
            ReleaseTexture(ref velocityWrite);
            ReleaseTexture(ref densityRead);
            ReleaseTexture(ref densityWrite);
            ReleaseTexture(ref pressureRead);
            ReleaseTexture(ref pressureWrite);
            ReleaseTexture(ref divergence);
            ReleaseTexture(ref curl);
        }

        private static void ReleaseTexture(ref RenderTexture texture)
        {
            if (texture == null)
            {
                return;
            }

            if (texture.IsCreated())
            {
                texture.Release();
            }

            if (Application.isPlaying)
            {
                Destroy(texture);
            }
            else
            {
                DestroyImmediate(texture);
            }
            texture = null;
        }

        private static void Swap(ref RenderTexture first, ref RenderTexture second)
        {
            RenderTexture temporary = first;
            first = second;
            second = temporary;
        }
    }
}
