using UnityEngine;
using UnityEngine.AI;

namespace XiYouJi.Gameplay
{
    /// <summary>Plays idle/walk from actual navigation movement without root-motion drift.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class NavMeshLocomotionAnimator : MonoBehaviour
    {
        public Animator animator;
        public NavMeshAgent agent;
        [Min(0.01f)] public float movementThreshold = 0.05f;
        [Min(0f)] public float speedDamping = 0.12f;

        private static readonly int IsMoving = Animator.StringToHash("IsMoving");
        private static readonly int WalkSpeed = Animator.StringToHash("WalkSpeed");

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (animator != null) animator.applyRootMotion = false;
        }

        private void Update()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            Vector3 velocity = agent != null && agent.enabled && agent.isOnNavMesh ? agent.velocity : Vector3.zero;
            velocity.y = 0f;
            bool moving = velocity.sqrMagnitude > movementThreshold * movementThreshold;
            animator.SetBool(IsMoving, moving);
            float speed = moving ? Mathf.Clamp(velocity.magnitude / Mathf.Max(0.01f, agent.speed), 0.65f, 1.35f) : 1f;
            animator.SetFloat(WalkSpeed, speed, speedDamping, Time.deltaTime);
        }

        private void OnDisable()
        {
            if (animator != null && animator.runtimeAnimatorController != null)
                animator.SetBool(IsMoving, false);
        }
    }
}
