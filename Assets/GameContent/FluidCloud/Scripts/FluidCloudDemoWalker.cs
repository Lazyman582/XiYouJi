using UnityEngine;
using UnityEngine.AI;

namespace XiYouJi.VFX.FluidCloud
{
    [DisallowMultipleComponent]
    public sealed class FluidCloudDemoWalker : MonoBehaviour
    {
        public Camera referenceCamera;
        [Min(0.1f)] public float moveSpeed = 5f;
        [Min(0.1f)] public float rotationSharpness = 14f;
        [Min(1f)] public float movementBounds = 8f;
        public bool autoDemo = true;
        [Min(0f)] public float manualOverrideSeconds = 4f;

        public Vector3 Velocity { get; private set; }

        private Animator animator;
        private int walkStateHash;
        private float lastManualInputTime = float.NegativeInfinity;

        private void Awake()
        {
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour == this) continue;
                string typeName = behaviour.GetType().Name;
                if (typeName == "PointClickNavController" || typeName == "PlayerAnimationDriver")
                {
                    behaviour.enabled = false;
                }
            }

            animator = GetComponentInChildren<Animator>(true);
            if (animator != null) animator.applyRootMotion = false;
            walkStateHash = Animator.StringToHash("Walk");
        }

        private void Update()
        {
            Vector2 manualInput = ReadInput();
            Vector3 direction;

            if (manualInput.sqrMagnitude > 0.01f)
            {
                lastManualInputTime = Time.time;
                direction = CameraRelativeDirection(manualInput.normalized);
            }
            else if (autoDemo && Time.time - lastManualInputTime > manualOverrideSeconds)
            {
                float angle = Time.time * 0.42f;
                Vector3 target = new Vector3(Mathf.Sin(angle) * 6.2f, transform.position.y, Mathf.Cos(angle) * 6.2f);
                direction = target - transform.position;
                direction.y = 0f;
                direction = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector3.zero;
            }
            else
            {
                direction = Vector3.zero;
            }

            Velocity = direction * moveSpeed;
            transform.position += Velocity * Time.deltaTime;
            Vector3 clamped = transform.position;
            clamped.x = Mathf.Clamp(clamped.x, -movementBounds, movementBounds);
            clamped.z = Mathf.Clamp(clamped.z, -movementBounds, movementBounds);
            transform.position = clamped;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
                float blend = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, blend);
            }

            UpdateAnimation(direction.sqrMagnitude > 0.001f);
        }

        private static Vector2 ReadInput()
        {
            float horizontal = 0f;
            float vertical = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) vertical -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) vertical += 1f;
            return Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
        }

        private Vector3 CameraRelativeDirection(Vector2 input)
        {
            Camera cameraToUse = referenceCamera != null ? referenceCamera : Camera.main;
            if (cameraToUse == null) return new Vector3(input.x, 0f, input.y);

            Vector3 forward = cameraToUse.transform.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = cameraToUse.transform.right;
            right.y = 0f;
            right.Normalize();
            return (right * input.x + forward * input.y).normalized;
        }

        private void UpdateAnimation(bool moving)
        {
            if (animator == null) return;
            if (moving)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.shortNameHash != walkStateHash) animator.Play(walkStateHash, 0, 0f);
                animator.speed = Mathf.Clamp(moveSpeed / 5f, 0.75f, 1.3f);
            }
            else
            {
                animator.speed = 0f;
            }
        }

        private void OnDisable()
        {
            if (animator != null) animator.speed = 1f;
        }
    }
}
