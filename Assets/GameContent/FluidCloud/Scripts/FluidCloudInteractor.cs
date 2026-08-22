using UnityEngine;
using UnityEngine.AI;

namespace XiYouJi.VFX.FluidCloud
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class FluidCloudInteractor : MonoBehaviour
    {
        public Transform source;
        public NavMeshAgent navMeshAgent;
        [Min(0.05f)] public float radius = 1.1f;
        [Min(0f)] public float velocityStrength = 1f;
        [Range(0f, 1f)] public float clearanceStrength = 1f;
        [Min(1f)] public float maximumTrackedSpeed = 20f;

        public Vector2 PositionXZ { get; private set; }
        public Vector2 VelocityXZ { get; private set; }

        private Vector3 previousPosition;

        private void OnEnable()
        {
            if (source == null)
            {
                source = transform;
            }

            if (navMeshAgent == null)
            {
                navMeshAgent = GetComponent<NavMeshAgent>();
            }

            previousPosition = source.position;
            PositionXZ = new Vector2(previousPosition.x, previousPosition.z);
            VelocityXZ = Vector2.zero;
        }

        private void Update()
        {
            if (source == null)
            {
                return;
            }

            Vector3 currentPosition = source.position;
            Vector3 velocity;
            if (navMeshAgent != null)
            {
                velocity = navMeshAgent.enabled && navMeshAgent.isOnNavMesh
                    ? navMeshAgent.velocity
                    : Vector3.zero;
            }
            else
            {
                float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
                velocity = (currentPosition - previousPosition) / deltaTime;
            }

            Vector2 horizontalVelocity = new Vector2(velocity.x, velocity.z);
            VelocityXZ = Vector2.ClampMagnitude(horizontalVelocity, maximumTrackedSpeed);
            PositionXZ = new Vector2(currentPosition.x, currentPosition.z);
            previousPosition = currentPosition;
        }
    }
}
