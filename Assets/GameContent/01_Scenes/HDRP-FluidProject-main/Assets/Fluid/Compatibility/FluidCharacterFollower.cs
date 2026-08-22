using UnityEngine;

/// <summary>
/// Keeps the fluid patch and its collision volume centered on the moving player.
/// The simulation remains in the same local grid while its world-space presentation follows.
/// </summary>
public sealed class FluidCharacterFollower : MonoBehaviour
{
    public Transform target;
    public Vector3 worldOffset;
    public bool followX = true;
    public bool followZ = true;
    public float followSpeed;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desired = target.position + worldOffset;
        Vector3 current = transform.position;

        if (!followX)
        {
            desired.x = current.x;
        }

        if (!followZ)
        {
            desired.z = current.z;
        }

        desired.y = current.y;

        if (followSpeed <= 0f)
        {
            transform.position = desired;
            return;
        }

        transform.position = Vector3.Lerp(
            current,
            desired,
            1f - Mathf.Exp(-followSpeed * Time.deltaTime));
    }
}
