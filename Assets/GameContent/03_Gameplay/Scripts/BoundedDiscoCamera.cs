using UnityEngine;

namespace XiYouJi.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class BoundedDiscoCamera : MonoBehaviour
    {
        public Transform target;
        public CameraMovementBounds movementBounds;

        [Header("Follow")]
        [Min(0.02f)]
        public float smoothTime = 0.42f;

        [Min(0.1f)]
        public float maxFollowSpeed = 80f;

        public bool followTargetHeight = true;
        public bool snapOnStart;

        [SerializeField]
        private Vector3 offset;

        private Vector3 followVelocity;
        private Quaternion lockedRotation;
        private float fixedWorldHeight;
        private bool initialized;

        public Vector3 Offset => offset;

        private void Awake()
        {
            CaptureCurrentPose();
            if (snapOnStart)
            {
                SnapToTarget();
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            if (!initialized)
            {
                CaptureCurrentPose();
            }

            Vector3 desiredPosition = GetClampedDesiredPosition();
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref followVelocity,
                smoothTime,
                maxFollowSpeed,
                Time.deltaTime);

            // The scene's authored isometric angle never changes while following or clamping.
            transform.rotation = lockedRotation;
        }

        public void Configure(Transform followTarget, CameraMovementBounds bounds)
        {
            target = followTarget;
            movementBounds = bounds;
            CaptureCurrentPose();
        }

        public void CaptureCurrentPose()
        {
            if (target == null)
            {
                initialized = false;
                return;
            }

            offset = transform.position - target.position;
            lockedRotation = transform.rotation;
            fixedWorldHeight = transform.position.y;
            followVelocity = Vector3.zero;
            initialized = true;
        }

        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            if (!initialized)
            {
                CaptureCurrentPose();
            }

            transform.position = GetClampedDesiredPosition();
            transform.rotation = lockedRotation;
            followVelocity = Vector3.zero;
        }

        public Vector3 GetClampedDesiredPosition()
        {
            Vector3 desiredPosition = target.position + offset;
            if (!followTargetHeight)
            {
                desiredPosition.y = fixedWorldHeight;
            }

            if (movementBounds != null)
            {
                desiredPosition = movementBounds.ClampPosition(desiredPosition);
            }

            return desiredPosition;
        }

        private void OnValidate()
        {
            smoothTime = Mathf.Max(0.02f, smoothTime);
            maxFollowSpeed = Mathf.Max(0.1f, maxFollowSpeed);
        }
    }
}
