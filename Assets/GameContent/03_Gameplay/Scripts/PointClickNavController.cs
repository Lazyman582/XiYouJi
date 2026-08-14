using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

namespace XiYouJi.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class PointClickNavController : MonoBehaviour
    {
        public Camera inputCamera;
        public LayerMask raycastMask = ~0;

        [Header("Click To Move")]
        [Min(1f)]
        public float maxRayDistance = 600f;

        [Min(0.1f)]
        public float navMeshSampleRadius = 4f;

        [Min(0.1f)]
        public float spawnSnapRadius = 12f;

        [Header("Walkable Polygon")]
        public PolygonWalkableArea walkableArea;

        [Min(0.05f)]
        public float pathValidationSpacing = 0.25f;

        public bool keepAgentInsidePolygon = true;

        [Header("Facing")]
        [Min(0.1f)]
        public float rotationSharpness = 14f;

        private NavMeshAgent agent;
        private bool warnedAboutMissingNavMesh;
        private bool hasLastAllowedPosition;
        private Vector3 lastAllowedPosition;

        public NavMeshAgent Agent => agent;
        public Vector3 LastDestination { get; private set; }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.updateRotation = false;
            ResolveWalkableArea();
            if (inputCamera == null)
            {
                inputCamera = Camera.main;
            }
        }

        private void Start()
        {
            if (EnsureAgentIsOnNavMesh())
            {
                RememberAllowedPosition();
            }
        }

        private void Update()
        {
            if (inputCamera == null)
            {
                inputCamera = Camera.main;
            }

            if (Input.GetMouseButtonDown(0) && !IsPointerOverUi())
            {
                TryMoveToScreen(Input.mousePosition);
            }

            UpdateFacingDirection();
            EnforceWalkableArea();
        }

        public bool TryMoveToScreen(Vector2 screenPosition)
        {
            if (inputCamera == null)
            {
                return false;
            }

            Ray ray = inputCamera.ScreenPointToRay(screenPosition);
            RaycastHit[] hits = Physics.RaycastAll(
                ray,
                maxRayDistance,
                raycastMask,
                QueryTriggerInteraction.Ignore);

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                Transform hitTransform = hits[i].collider.transform;
                if (hitTransform == transform || hitTransform.IsChildOf(transform))
                {
                    continue;
                }

                if (TryMoveToWorld(hits[i].point))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryMoveToWorld(Vector3 worldPosition)
        {
            ResolveWalkableArea();
            if (!EnsureAgentIsOnNavMesh())
            {
                return false;
            }

            if (walkableArea != null && !walkableArea.Contains(worldPosition))
            {
                return false;
            }

            NavMeshHit navHit;
            if (!NavMesh.SamplePosition(worldPosition, out navHit, navMeshSampleRadius, agent.areaMask))
            {
                return false;
            }

            if (walkableArea != null && !walkableArea.Contains(navHit.position))
            {
                return false;
            }

            NavMeshPath path = new NavMeshPath();
            if (!agent.CalculatePath(navHit.position, path)
                || path.status != NavMeshPathStatus.PathComplete)
            {
                return false;
            }

            if (walkableArea != null
                && !walkableArea.ContainsPath(agent.nextPosition, path.corners, pathValidationSpacing))
            {
                return false;
            }

            bool accepted = agent.SetPath(path);

            if (accepted)
            {
                LastDestination = navHit.position;
                warnedAboutMissingNavMesh = false;
            }

            return accepted;
        }

        public bool EnsureAgentIsOnNavMesh()
        {
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }

            if (agent.isOnNavMesh)
            {
                return true;
            }

            NavMeshHit navHit;
            if (NavMesh.SamplePosition(transform.position, out navHit, spawnSnapRadius, agent.areaMask))
            {
                return agent.Warp(navHit.position);
            }

            if (!warnedAboutMissingNavMesh)
            {
                Debug.LogWarning("PointClickNavController could not find a NavMesh near " + name + ".", this);
                warnedAboutMissingNavMesh = true;
            }

            return false;
        }

        private void UpdateFacingDirection()
        {
            if (agent == null || !agent.isOnNavMesh)
            {
                return;
            }

            Vector3 direction = agent.desiredVelocity;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0025f)
            {
                return;
            }

            Quaternion desiredRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            float blend = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, blend);
        }

        private void ResolveWalkableArea()
        {
            if (walkableArea == null)
            {
                walkableArea = FindObjectOfType<PolygonWalkableArea>();
            }
        }

        private void RememberAllowedPosition()
        {
            if (walkableArea == null || walkableArea.Contains(agent.nextPosition))
            {
                lastAllowedPosition = agent.nextPosition;
                hasLastAllowedPosition = true;
            }
        }

        private void EnforceWalkableArea()
        {
            if (!keepAgentInsidePolygon
                || walkableArea == null
                || agent == null
                || !agent.isOnNavMesh)
            {
                return;
            }

            if (walkableArea.Contains(agent.nextPosition))
            {
                RememberAllowedPosition();
                return;
            }

            agent.ResetPath();
            if (hasLastAllowedPosition)
            {
                agent.Warp(lastAllowedPosition);
                transform.position = lastAllowedPosition;
            }
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private void OnValidate()
        {
            maxRayDistance = Mathf.Max(1f, maxRayDistance);
            navMeshSampleRadius = Mathf.Max(0.1f, navMeshSampleRadius);
            spawnSnapRadius = Mathf.Max(0.1f, spawnSnapRadius);
            pathValidationSpacing = Mathf.Max(0.05f, pathValidationSpacing);
            rotationSharpness = Mathf.Max(0.1f, rotationSharpness);
        }
    }
}
