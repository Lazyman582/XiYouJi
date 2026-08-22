using UnityEngine;

/// <summary>
/// Moves only the circular collision mask to the player.
/// The fluid plane stays in world space, so the player leaves a wake instead of carrying it.
/// </summary>
public sealed class FluidCharacterMaskFollower : MonoBehaviour
{
    public Transform target;
    public Transform fluidRoot;
    public float localHeight = -0.84f;
    public bool clampToFluidPlane = true;

    private void LateUpdate()
    {
        if (target == null || fluidRoot == null)
        {
            return;
        }

        Vector3 localPosition = fluidRoot.InverseTransformPoint(target.position);
        localPosition.y = localHeight;

        if (clampToFluidPlane)
        {
            Transform plane = fluidRoot.Find("Plane");
            if (plane != null)
            {
                float halfWorldSize = Mathf.Max(1f, plane.lossyScale.x * 5f) - 1f;
                localPosition.x = Mathf.Clamp(localPosition.x, -halfWorldSize, halfWorldSize);
                localPosition.z = Mathf.Clamp(localPosition.z, -halfWorldSize, halfWorldSize);
            }
        }

        transform.localPosition = localPosition;
        transform.localRotation = Quaternion.identity;
    }
}
