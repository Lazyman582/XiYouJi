using UnityEngine;

namespace XiYouJi.VFX.FluidCloud
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class FluidCloudDemoCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 8.5f, -15.5f);
        public Vector3 lookOffset = new Vector3(0f, 1.2f, 0.6f);
        [Min(0.01f)] public float smoothTime = 0.18f;

        private Vector3 velocity;

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref velocity,
                smoothTime,
                Mathf.Infinity,
                Time.deltaTime);
            transform.LookAt(target.position + lookOffset, Vector3.up);
        }
    }
}
