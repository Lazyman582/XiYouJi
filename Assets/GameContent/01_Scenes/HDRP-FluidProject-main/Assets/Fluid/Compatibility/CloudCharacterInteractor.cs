using UnityEngine;

/// <summary>
/// Registers one character with the fluid simulation.
/// It owns only a rotated circular obstruction mask; the visible result comes from
/// the fluid solver, not from a separate TrailRenderer.
/// </summary>
[DisallowMultipleComponent]
public sealed class CloudCharacterInteractor : MonoBehaviour
{
    public FluidSimulationBuiltIn simulation;
    public Transform targetTransform;
    public float maskHeight = -0.84f;
    public bool pinMaskToCloudSurface = true;
    public float surfaceOffset = 0.02f;
    public float maskWidth = 1.35f;
    public float maskDepth = 1f;
    public float interactionRadius = 8f;
    public float interactionDensity = 0.018f;
    public float movementMultiplier = 1f;
    public float rotationMultiplier = 1f;
    public bool syncMaskRotation = true;

    public Vector2 CurrentVelocity { get; private set; }
    public float CurrentTurnRate { get; private set; }

    public bool ControlsTarget(Transform candidate)
    {
        return candidate != null && CharacterTransform == candidate;
    }

    private Transform mask;
    private Vector3 previousPosition;
    private float previousYaw;
    private bool hasPreviousSample;

    private Transform CharacterTransform
    {
        get { return targetTransform != null ? targetTransform : transform; }
    }

    private void OnEnable()
    {
        if (simulation == null)
        {
            simulation = FindObjectOfType<FluidSimulationBuiltIn>();
        }

        previousPosition = CharacterTransform.position;
        previousYaw = CharacterTransform.eulerAngles.y;
        hasPreviousSample = true;
        CurrentVelocity = Vector2.zero;
        CurrentTurnRate = 0f;

        if (simulation != null)
        {
            simulation.RegisterCharacter(this);
        }
    }

    private void OnDisable()
    {
        if (simulation != null)
        {
            simulation.UnregisterCharacter(this);
        }

        if (mask != null)
        {
            DestroyOwnedObject(mask.gameObject);
            mask = null;
        }
    }

    private void FixedUpdate()
    {
        if (simulation != null)
        {
            SampleMotion(Time.fixedDeltaTime, simulation.transform);
        }
    }

    private void LateUpdate()
    {
        if (simulation != null)
        {
            UpdateMask(simulation.transform);
        }
    }

    internal void SampleMotion(float deltaTime, Transform fluidRoot)
    {
        Transform character = CharacterTransform;
        if (!hasPreviousSample)
        {
            previousPosition = character.position;
            previousYaw = character.eulerAngles.y;
            hasPreviousSample = true;
            return;
        }

        float step = Mathf.Max(deltaTime, 0.0001f);
        Vector3 worldDelta = character.position - previousPosition;
        Vector3 localDelta = fluidRoot.InverseTransformDirection(worldDelta);
        Vector2 measuredVelocity = new Vector2(localDelta.x, localDelta.z) / step;
        float measuredTurnRate = Mathf.DeltaAngle(
            previousYaw,
            character.eulerAngles.y) / step;

        CurrentVelocity = Vector2.Lerp(
            CurrentVelocity,
            measuredVelocity * movementMultiplier,
            1f - Mathf.Exp(-12f * step));
        CurrentTurnRate = Mathf.Lerp(
            CurrentTurnRate,
            Mathf.Clamp(measuredTurnRate, -720f, 720f) * rotationMultiplier,
            1f - Mathf.Exp(-14f * step));

        previousPosition = character.position;
        previousYaw = character.eulerAngles.y;
    }

    internal void UpdateMask(Transform fluidRoot)
    {
        EnsureMask(fluidRoot);
        if (mask == null)
        {
            return;
        }

        Transform character = CharacterTransform;
        Vector3 localPosition = fluidRoot.InverseTransformPoint(character.position);
        float surfaceWorldY = fluidRoot.TransformPoint(new Vector3(0f, maskHeight, 0f)).y;

        Transform plane = fluidRoot.Find("Plane");
        if (plane != null)
        {
            float halfWorldSize = Mathf.Max(1f, plane.lossyScale.x * 5f) - 1f;
            localPosition.x = Mathf.Clamp(localPosition.x, -halfWorldSize, halfWorldSize);
            localPosition.z = Mathf.Clamp(localPosition.z, -halfWorldSize, halfWorldSize);
            if (pinMaskToCloudSurface)
            {
                surfaceWorldY = plane.position.y + surfaceOffset;
            }
        }

        Vector3 worldPosition = fluidRoot.TransformPoint(localPosition);
        worldPosition.y = surfaceWorldY;
        mask.position = worldPosition;
        if (syncMaskRotation)
        {
            mask.rotation = Quaternion.Euler(
                0f,
                character.eulerAngles.y,
                0f);
        }
    }

    private void EnsureMask(Transform fluidRoot)
    {
        if (mask != null)
        {
            return;
        }

        GameObject maskObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        maskObject.name = "Cloud Mask - " + CharacterTransform.name;
        maskObject.transform.SetParent(fluidRoot, false);
        maskObject.layer = 11;
        maskObject.transform.localScale = new Vector3(maskWidth, 0.02f, maskDepth);

        Collider collider = maskObject.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyOwnedObject(collider);
        }

        mask = maskObject.transform;
    }

    private static void DestroyOwnedObject(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(target);
        }
        else
        {
            Object.DestroyImmediate(target);
        }
    }
}
