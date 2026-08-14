using UnityEngine;
using UnityEngine.AI;

namespace XiYouJi.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private string walkStateName = "Walk";
        [SerializeField] private float movementThreshold = 0.05f;
        [SerializeField] private bool pauseWhenStopped = true;

        private int walkStateHash;

        private void Awake()
        {
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            walkStateHash = Animator.StringToHash(walkStateName);
        }

        private void Update()
        {
            if (animator == null)
            {
                return;
            }

            bool isMoving = IsMoving();
            if (isMoving)
            {
                if (!IsWalkStateActive())
                {
                    animator.Play(walkStateHash, 0, 0f);
                }

                animator.speed = GetAnimationSpeed();
            }
            else if (pauseWhenStopped)
            {
                animator.speed = 0f;
            }
        }

        private bool IsMoving()
        {
            if (agent == null)
            {
                return false;
            }

            if (agent.velocity.sqrMagnitude > movementThreshold * movementThreshold)
            {
                return true;
            }

            return agent.hasPath
                && !agent.pathPending
                && agent.remainingDistance > agent.stoppingDistance + movementThreshold;
        }

        private bool IsWalkStateActive()
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            return state.shortNameHash == walkStateHash;
        }

        private float GetAnimationSpeed()
        {
            if (agent == null || agent.speed <= 0.01f)
            {
                return 1f;
            }

            return Mathf.Clamp(agent.velocity.magnitude / agent.speed, 0.75f, 1.25f);
        }

        private void OnDisable()
        {
            if (animator != null)
            {
                animator.speed = 1f;
            }
        }
    }
}
