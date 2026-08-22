using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class FluidCollisionMaskCamera : MonoBehaviour
{
    [SerializeField, Range(0, 31)] private int obstacleLayer = 11;
    [SerializeField] public bool includeDefaultLayer = true;

    private Camera targetCamera;

    private void OnEnable()
    {
        ApplySettings();
    }

    private void OnValidate()
    {
        ApplySettings();
    }

    private void ApplySettings()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        Shader replacementShader = Shader.Find("XiYouJi/Fluid/Collision Mask Built-In");
        if (replacementShader == null)
        {
            return;
        }

        targetCamera.clearFlags = CameraClearFlags.SolidColor;
        targetCamera.backgroundColor = Color.black;
        targetCamera.cullingMask = (1 << obstacleLayer) | (includeDefaultLayer ? 1 : 0);
        targetCamera.SetReplacementShader(replacementShader, string.Empty);
    }

    private void OnDisable()
    {
        if (targetCamera != null)
        {
            targetCamera.ResetReplacementShader();
        }
    }
}
