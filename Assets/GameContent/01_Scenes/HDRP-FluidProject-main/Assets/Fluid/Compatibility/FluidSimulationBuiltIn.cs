using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class FluidSimulationBuiltIn : MonoBehaviour
{
    public ComputeShader shader;

    [Header("Initial density")]
    public Texture2D densitySource;

    [Header("Initial velocity")]
    public Texture2D velocitySource;

    [Header("Continuous density")]
    public Texture2D continuousDensitySource;

    [Header("Continuous velocity")]
    public Texture2D continuousVelocitySource;

    public RenderTexture dynamicSourceRT;
    public RenderTexture inputRT;
    public RenderTexture outputRT;

    public bool isUseContinuousVelSource = true;
    public bool isUseContinuousDensSource = true;
    public bool isUseMouseClick = true;
    public bool isBorderless = true;
    public bool isCollision = true;
    public bool isLaunchfromObject;
    public int brushSize = 10;

    [Header("Startup")]
    public bool startFilled = true;
    [Range(0f, 1f)]
    public float initialFill = 1f;

    [Header("Player Motion")]
    public Transform interactionTarget;
    public bool syncPlayerMotion = true;
    public float movementImpulse = 1.8f;
    public float rotationImpulse = 0.65f;
    public float interactionRadius = 8f;
    public float interactionDensity = 0.02f;
    public float surfaceFlowScale = 1.4f;
    public float surfaceRotationScale = 0.9f;

    [Header("Character Registration")]
    public bool autoRegisterTaggedCharacters = true;
    public string interactionTag = "Player";
    public float characterScanInterval = 0.5f;

    [Header("Simulation resolution")]
    public int size = 1024;

    [Header("Pressure iterations")]
    public int projectIterations = 20;

    [Header("Diffusion iterations")]
    public int diffuseIterations = 20;

    [Header("Disturbance")]
    public float disturbance = 0.2f;
    public float mulVel;
    public float mulDens;

    [Header("Diffusion")]
    public float diff;
    public float dt;

    private Dictionary<string, ComputeBuffer> computeBufferMap =
        new Dictionary<string, ComputeBuffer>();

    private float[] continuousSourceDensity;
    private float[] continuousSourceVelocityX;
    private float[] continuousSourceVelocityY;
    private bool initialized;
    private bool hasStarted;
    private Vector3 previousInteractionPosition;
    private float previousInteractionYaw;
    private Vector2 interactionVelocity;
    private float interactionTurnRate;
    private bool hasInteractionState;
    private Vector2 surfaceFlowOffset;
    private float surfaceFlowRotation;
    private Renderer surfaceRenderer;
    private MaterialPropertyBlock surfaceProperties;
    private readonly List<CloudCharacterInteractor> registeredCharacters =
        new List<CloudCharacterInteractor>();
    private float nextCharacterScanTime;

    private void Start()
    {
        hasStarted = true;
        if (autoRegisterTaggedCharacters)
        {
            syncPlayerMotion = false;
            interactionTarget = null;
            DisableLegacySingleCharacterObjects();
            AutoRegisterTaggedCharacters();
        }
        TryInitialize();
    }

    private void Update()
    {
        if (!Application.isPlaying || !autoRegisterTaggedCharacters)
        {
            return;
        }

        if (Time.time >= nextCharacterScanTime)
        {
            nextCharacterScanTime = Time.time + Mathf.Max(0.1f, characterScanInterval);
            AutoRegisterTaggedCharacters();
        }
    }

    private void OnEnable()
    {
        if (Application.isPlaying && hasStarted && !initialized)
        {
            TryInitialize();
        }
    }

    private void FixedUpdate()
    {
        if (!initialized)
        {
            return;
        }

        UpdatePlayerMotion();
        SetShaderValues();
        ApplyRegisteredCharacterImpulses();
        ApplyPlayerImpulse();
        Simulate();
        UpdateSurfaceFlow();
    }

    private void OnDisable()
    {
        ReleaseBuffers();
    }

    private void OnDestroy()
    {
        ReleaseBuffers();
    }

    private void OnValidate()
    {
        size = Mathf.Max(32, size);
        projectIterations = Mathf.Max(1, projectIterations);
        diffuseIterations = Mathf.Max(1, diffuseIterations);
        brushSize = Mathf.Max(1, brushSize);
        dt = Mathf.Max(0.0001f, dt);
    }

    private void TryInitialize()
    {
        ReleaseBuffers();

        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.LogError("Fluid simulation requires Compute Shader support.", this);
            enabled = false;
            return;
        }

        if (shader == null ||
            densitySource == null ||
            velocitySource == null ||
            continuousDensitySource == null ||
            continuousVelocitySource == null ||
            inputRT == null ||
            outputRT == null)
        {
            Debug.LogError("Fluid simulation has one or more missing source assets.", this);
            enabled = false;
            return;
        }

        if (inputRT.width != size || inputRT.height != size ||
            outputRT.width != size || outputRT.height != size)
        {
            Debug.LogError(
                $"Fluid size ({size}) must match InputRT and OutputRT dimensions.",
                this);
            enabled = false;
            return;
        }

        try
        {
            PrepareRandomWriteTexture(inputRT);
            PrepareRandomWriteTexture(outputRT);
            SetShaderValues(true);

            float[] sourceDensity = FluidSimulationFuncLibrary.GetGrayscale(
                densitySource, size, "R", "G", mulDens);
            float[] sourceVelocityX = FluidSimulationFuncLibrary.GetGrayscale(
                velocitySource, size, "R", 0.5f, mulVel);
            float[] sourceVelocityY = FluidSimulationFuncLibrary.GetGrayscale(
                velocitySource, size, "G", 0.5f, mulVel);

            continuousSourceDensity = FluidSimulationFuncLibrary.GetGrayscale(
                continuousDensitySource, size, "R", "G", mulDens);
            continuousSourceVelocityX = FluidSimulationFuncLibrary.GetGrayscale(
                continuousVelocitySource, size, "R", 0.5f, mulVel);
            continuousSourceVelocityY = FluidSimulationFuncLibrary.GetGrayscale(
                continuousVelocitySource, size, "G", 0.5f, mulVel);

            int cellCount = size * size;
            AddBuffer("U0", sourceVelocityX);
            AddBuffer("V0", sourceVelocityY);
            AddBuffer("X0", sourceDensity);
            AddBuffer("U", new float[cellCount]);
            AddBuffer("V", new float[cellCount]);
            float[] initialDensity = new float[cellCount];
            if (startFilled)
            {
                float fill = Mathf.Clamp01(initialFill);
                for (int i = 0; i < initialDensity.Length; i++)
                {
                    initialDensity[i] = fill;
                }
            }
            AddBuffer("X", initialDensity);
            AddBuffer("C", new float[cellCount]);
            AddBuffer("DS", new float[cellCount]);

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.depthTextureMode |= DepthTextureMode.Depth;
            }

            initialized = true;
            ResetPlayerMotionState();
            CacheSurfaceRenderer();
            OutputDensity();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            ReleaseBuffers();
            enabled = false;
        }
    }

    private void Simulate()
    {
        VelocityStep();
        DensityStep();

        if (isCollision)
        {
            ApplyCollision();
        }

        OutputDensity();
    }

    private void DensityStep()
    {
        if (isUseContinuousDensSource)
        {
            computeBufferMap["X0"].SetData(continuousSourceDensity);
            RunKernel("AddRandomDensSource", "X0_SBuffer");
        }

        if (isLaunchfromObject && dynamicSourceRT != null)
        {
            FluidSimulationFuncLibrary.AddDynamicSource(
                shader,
                ref dynamicSourceRT,
                ref computeBufferMap);
        }

        RunKernel("AddSource", "X0_SBuffer", "X_XBuffer");

        for (int i = 0; i < diffuseIterations; i++)
        {
            RunKernel("Diffusion", "X0_X0Buffer", "X_XBuffer");
        }

        Swap("X", "X0");
        RunKernel("Advect", "X_DBuffer", "X0_D0Buffer", "U_UBuffer", "V_VBuffer");
        RunKernel("MoveBuffer", "X_XBuffer");
    }

    private void VelocityStep()
    {
        if (isUseContinuousVelSource)
        {
            computeBufferMap["U0"].SetData(continuousSourceVelocityX);
            computeBufferMap["V0"].SetData(continuousSourceVelocityY);
            RunKernel("AddRandomVelSource", "U0_SBuffer");
            RunKernel("AddRandomVelSource", "V0_SBuffer");
        }

        RunKernel("AddSource", "U0_SBuffer", "U_XBuffer");
        RunKernel("AddSource", "V0_SBuffer", "V_XBuffer");

        for (int i = 0; i < diffuseIterations; i++)
        {
            RunKernel("Diffusion", "U0_X0Buffer", "U_XBuffer");
            RunKernel("Diffusion", "V0_X0Buffer", "V_XBuffer");
        }

        ProjectVelocity();
        Swap("U", "U0");
        Swap("V", "V0");

        RunKernel("Advect", "U_DBuffer", "U0_D0Buffer", "U0_UBuffer", "V0_VBuffer");
        RunKernel("Advect", "V_DBuffer", "V0_D0Buffer", "U0_UBuffer", "V0_VBuffer");

        ProjectVelocity();
        RunKernel("MoveBuffer", "U_XBuffer");
        RunKernel("MoveBuffer", "V_XBuffer");
    }

    private void ProjectVelocity()
    {
        RunKernel("ProjectDiv", "U_UBuffer", "V_VBuffer", "U0_PBuffer", "V0_DivBuffer");

        for (int i = 0; i < projectIterations; i++)
        {
            RunKernel("ProjectP", "U0_PBuffer", "V0_DivBuffer");
        }

        RunKernel("ProjectVel", "U_UBuffer", "V_VBuffer", "U0_PBuffer");
    }

    private void ApplyCollision()
    {
        FluidSimulationFuncLibrary.CalculateCollision(
            shader,
            ref inputRT,
            ref outputRT,
            ref computeBufferMap);

        if (!isUseMouseClick || !Input.GetMouseButton(0))
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Vector3 viewport = mainCamera.ScreenToViewportPoint(Input.mousePosition);
        Vector2Int brushPosition = new Vector2Int(
            Mathf.Clamp(Mathf.RoundToInt(viewport.x * (size - 1)), 0, size - 1),
            Mathf.Clamp(Mathf.RoundToInt(viewport.y * (size - 1)), 0, size - 1));

        FluidSimulationFuncLibrary.CalculateBrush(
            shader,
            ref inputRT,
            brushPosition,
            brushSize);
    }

    private void ResetPlayerMotionState()
    {
        hasInteractionState = interactionTarget != null;
        if (!hasInteractionState)
        {
            previousInteractionPosition = Vector3.zero;
            previousInteractionYaw = 0f;
            interactionVelocity = Vector2.zero;
            interactionTurnRate = 0f;
            return;
        }

        previousInteractionPosition = interactionTarget.position;
        previousInteractionYaw = interactionTarget.eulerAngles.y;
        interactionVelocity = Vector2.zero;
        interactionTurnRate = 0f;
        surfaceFlowOffset = Vector2.zero;
        surfaceFlowRotation = 0f;
    }

    private void DisableLegacySingleCharacterObjects()
    {
        FluidCharacterFollower legacyFollower = GetComponent<FluidCharacterFollower>();
        if (legacyFollower != null)
        {
            legacyFollower.enabled = false;
        }

        Transform legacyMask = transform.Find("Character Cloud Mask");
        if (legacyMask != null)
        {
            legacyMask.gameObject.SetActive(false);
        }
    }

    private void AutoRegisterTaggedCharacters()
    {
        if (string.IsNullOrEmpty(interactionTag))
        {
            return;
        }

        GameObject[] characters;
        try
        {
            characters = GameObject.FindGameObjectsWithTag(interactionTag);
        }
        catch (UnityException)
        {
            return;
        }

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] == null || IsCharacterAlreadyRegistered(characters[i].transform))
            {
                continue;
            }

            CloudCharacterInteractor interactor =
                characters[i].AddComponent<CloudCharacterInteractor>();
            interactor.simulation = this;
        }
    }

    private bool IsCharacterAlreadyRegistered(Transform candidate)
    {
        for (int i = 0; i < registeredCharacters.Count; i++)
        {
            if (registeredCharacters[i] != null &&
                registeredCharacters[i].ControlsTarget(candidate))
            {
                return true;
            }
        }

        return false;
    }

    private void UpdatePlayerMotion()
    {
        if (!syncPlayerMotion || interactionTarget == null)
        {
            interactionVelocity = Vector2.Lerp(
                interactionVelocity,
                Vector2.zero,
                1f - Mathf.Exp(-8f * Time.fixedDeltaTime));
            interactionTurnRate = Mathf.Lerp(
                interactionTurnRate,
                0f,
                1f - Mathf.Exp(-8f * Time.fixedDeltaTime));
            return;
        }

        if (!hasInteractionState)
        {
            ResetPlayerMotionState();
            return;
        }

        float step = Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        Vector3 worldDelta = interactionTarget.position - previousInteractionPosition;
        Vector3 localDelta = transform.InverseTransformDirection(worldDelta);
        Vector2 measuredVelocity = new Vector2(localDelta.x, localDelta.z) / step;
        float measuredTurnRate = Mathf.DeltaAngle(
            previousInteractionYaw,
            interactionTarget.eulerAngles.y) / step;

        interactionVelocity = Vector2.Lerp(
            interactionVelocity,
            measuredVelocity,
            1f - Mathf.Exp(-12f * step));
        interactionTurnRate = Mathf.Lerp(
            interactionTurnRate,
            Mathf.Clamp(measuredTurnRate, -720f, 720f),
            1f - Mathf.Exp(-14f * step));

        previousInteractionPosition = interactionTarget.position;
        previousInteractionYaw = interactionTarget.eulerAngles.y;
    }

    private void ApplyPlayerImpulse()
    {
        if (!syncPlayerMotion || interactionTarget == null || !hasInteractionState)
        {
            return;
        }

        DispatchPlayerImpulse(
            interactionTarget.position,
            interactionVelocity,
            interactionTurnRate,
            interactionRadius,
            interactionDensity);
    }

    private void ApplyRegisteredCharacterImpulses()
    {
        for (int i = registeredCharacters.Count - 1; i >= 0; i--)
        {
            CloudCharacterInteractor character = registeredCharacters[i];
            if (character == null)
            {
                registeredCharacters.RemoveAt(i);
                continue;
            }

            DispatchPlayerImpulse(
                character.transform.position,
                character.CurrentVelocity,
                character.CurrentTurnRate,
                character.interactionRadius,
                character.interactionDensity);
        }
    }

    private void DispatchPlayerImpulse(
        Vector3 worldPosition,
        Vector2 velocity,
        float turnRate,
        float radiusWorld,
        float density)
    {
        if (shader == null || computeBufferMap.Count == 0)
        {
            return;
        }

        int kernel = shader.FindKernel("AddPlayerImpulse");
        float worldSize = GetSurfaceWorldSize();
        float velocityToGrid = 1f / Mathf.Max(worldSize, 1f);
        int radius = Mathf.Clamp(
            Mathf.RoundToInt(radiusWorld / Mathf.Max(worldSize, 1f) * size),
            2,
            Mathf.Max(2, size / 3));

        Vector2Int playerGridPosition = GetPlayerGridPosition(worldPosition, worldSize);
        shader.SetInt("playerPosX", playerGridPosition.x);
        shader.SetInt("playerPosY", playerGridPosition.y);
        shader.SetInt("playerRadius", radius);
        shader.SetFloat(
            "playerVelocityX",
            -velocity.x * velocityToGrid * movementImpulse);
        shader.SetFloat(
            "playerVelocityY",
            -velocity.y * velocityToGrid * movementImpulse);
        shader.SetFloat(
            "playerTurnRate",
            turnRate * Mathf.Deg2Rad * rotationImpulse);
        shader.SetFloat("playerDensity", density);
        shader.SetBuffer(kernel, "UBuffer", computeBufferMap["U"]);
        shader.SetBuffer(kernel, "VBuffer", computeBufferMap["V"]);
        shader.SetBuffer(kernel, "XBuffer", computeBufferMap["X"]);
        Dispatch(kernel);
    }

    private Vector2Int GetPlayerGridPosition(Vector3 worldPosition, float worldSize)
    {
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        float normalizedX = localPosition.x / Mathf.Max(worldSize, 1f) + 0.5f;
        float normalizedY = localPosition.z / Mathf.Max(worldSize, 1f) + 0.5f;
        return new Vector2Int(
            Mathf.Clamp(Mathf.RoundToInt(normalizedX * (size - 1)), 0, size - 1),
            Mathf.Clamp(Mathf.RoundToInt(normalizedY * (size - 1)), 0, size - 1));
    }

    public void RegisterCharacter(CloudCharacterInteractor character)
    {
        if (character != null && !registeredCharacters.Contains(character))
        {
            registeredCharacters.Add(character);
        }
    }

    public void UnregisterCharacter(CloudCharacterInteractor character)
    {
        if (character != null)
        {
            registeredCharacters.Remove(character);
        }
    }

    private void CacheSurfaceRenderer()
    {
        if (surfaceProperties == null)
        {
            surfaceProperties = new MaterialPropertyBlock();
        }

        if (surfaceRenderer != null)
        {
            return;
        }

        Transform plane = transform.Find("Plane");
        if (plane != null)
        {
            surfaceRenderer = plane.GetComponent<Renderer>();
        }

    }

    private void UpdateSurfaceFlow()
    {
        CacheSurfaceRenderer();
        if (surfaceRenderer == null)
        {
            return;
        }

        float step = Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        float worldSize = GetSurfaceWorldSize();
        Vector2 combinedVelocity = interactionVelocity;
        float combinedTurnRate = interactionTurnRate;
        for (int i = 0; i < registeredCharacters.Count; i++)
        {
            if (registeredCharacters[i] == null)
            {
                continue;
            }

            combinedVelocity += registeredCharacters[i].CurrentVelocity * 0.35f;
            combinedTurnRate += registeredCharacters[i].CurrentTurnRate * 0.35f;
        }

        surfaceFlowOffset +=
            -combinedVelocity * (step / Mathf.Max(worldSize, 1f)) * surfaceFlowScale;
        surfaceFlowRotation +=
            combinedTurnRate * Mathf.Deg2Rad * step * surfaceRotationScale;

        surfaceRenderer.GetPropertyBlock(surfaceProperties);
        surfaceProperties.SetVector(
            "_FlowOffset",
            new Vector4(surfaceFlowOffset.x, surfaceFlowOffset.y, 0f, 0f));
        surfaceProperties.SetFloat("_FlowRotation", surfaceFlowRotation);
        surfaceRenderer.SetPropertyBlock(surfaceProperties);
    }

    private float GetSurfaceWorldSize()
    {
        Transform plane = transform.Find("Plane");
        if (plane == null)
        {
            return 1f;
        }

        return Mathf.Max(1f, plane.lossyScale.x * 10f);
    }

    private void OutputDensity()
    {
        int kernel = shader.FindKernel("OutputRT");
        shader.SetBuffer(kernel, "XBuffer", computeBufferMap["X"]);
        shader.SetTexture(kernel, "outputTexture", outputRT);
        Dispatch(kernel);
    }

    private void RunKernel(string kernelName, params string[] bufferBindings)
    {
        FluidSimulationFuncLibrary.CalculateShader(
            shader,
            kernelName,
            bufferBindings,
            ref computeBufferMap);
    }

    private void SetShaderValues(bool force = false)
    {
        if (force || FluidSimulationFuncLibrary.diff != diff)
        {
            FluidSimulationFuncLibrary.diff = diff;
            shader.SetFloat("diff", diff);
        }

        if (force || FluidSimulationFuncLibrary.size != size)
        {
            FluidSimulationFuncLibrary.size = size;
            shader.SetInt("N", size);
        }

        if (force || FluidSimulationFuncLibrary.dt != dt)
        {
            FluidSimulationFuncLibrary.dt = dt;
            shader.SetFloat("dt", dt);
        }

        if (force || FluidSimulationFuncLibrary.disturbance != disturbance)
        {
            FluidSimulationFuncLibrary.disturbance = disturbance;
            shader.SetFloat("disturbance", disturbance);
        }

        shader.SetInt("isBorderless", isBorderless ? 1 : 0);
        shader.SetFloat("randomSeed", UnityEngine.Random.Range(-1f, 1f));
        shader.SetFloat("time", Time.time);
    }

    private void Dispatch(int kernel)
    {
        shader.GetKernelThreadGroupSizes(kernel, out uint threadX, out uint threadY, out _);
        int groupsX = Mathf.CeilToInt(size / (float)Mathf.Max(1, (int)threadX));
        int groupsY = Mathf.CeilToInt(size / (float)Mathf.Max(1, (int)threadY));
        shader.Dispatch(kernel, groupsX, groupsY, 1);
    }

    private void AddBuffer(string key, float[] values)
    {
        computeBufferMap.Add(key, FluidSimulationFuncLibrary.GetComputeBuffer(values));
    }

    private void Swap(string first, string second)
    {
        ComputeBuffer temporary = computeBufferMap[first];
        computeBufferMap[first] = computeBufferMap[second];
        computeBufferMap[second] = temporary;
    }

    private static void PrepareRandomWriteTexture(RenderTexture texture)
    {
        if (texture.IsCreated())
        {
            texture.Release();
        }

        texture.enableRandomWrite = true;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Create();
    }

    private void ReleaseBuffers()
    {
        foreach (ComputeBuffer buffer in computeBufferMap.Values)
        {
            buffer?.Release();
        }

        computeBufferMap.Clear();
        initialized = false;
    }
}
