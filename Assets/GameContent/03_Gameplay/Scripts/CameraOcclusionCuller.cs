using System.Collections.Generic;
using UnityEngine;

namespace XiYouJi.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(100)]
    public sealed class CameraOcclusionCuller : MonoBehaviour
    {
        public Transform target;

        [Header("Occlusion Detection")]
        public LayerMask occluderMask = ~0;

        public Vector3 targetViewOffset = new Vector3(0f, 1f, 0f);

        [Min(0.01f)]
        public float probeRadius = 0.22f;

        [Header("Visibility")]
        [Tooltip("Keeps a wall hidden briefly after it leaves the sight line to prevent LOD flicker.")]
        [Min(0f)]
        public float restoreDelay = 0.12f;

        [Tooltip("Hide every renderer in the hit LOD group so another LOD cannot pop into view.")]
        public bool cullWholeLodGroup = true;

        private const int HitBufferSize = 128;

        private readonly RaycastHit[] hitBuffer = new RaycastHit[HitBufferSize];
        private readonly HashSet<Renderer> blockedThisFrame = new HashSet<Renderer>();
        private readonly Dictionary<Renderer, bool> originalRenderingStates = new Dictionary<Renderer, bool>();
        private readonly Dictionary<Renderer, float> lastBlockedTimes = new Dictionary<Renderer, float>();
        private readonly Dictionary<Collider, Renderer[]> colliderRendererCache = new Dictionary<Collider, Renderer[]>();
        private readonly Dictionary<LODGroup, Renderer[]> lodRendererCache = new Dictionary<LODGroup, Renderer[]>();
        private readonly List<Renderer> restoreBuffer = new List<Renderer>();

        private static readonly Renderer[] NoRenderers = new Renderer[0];

        private Transform lastTarget;
        private Vector3 lastTargetPosition;
        private Vector3 lastCameraPosition;
        private Vector3 lastTargetViewOffset;
        private float lastProbeRadius;
        private int lastOccluderMask;
        private bool lastCullWholeLodGroup;
        private bool hasQueryState;

        public int HiddenRendererCount => originalRenderingStates.Count;
        public int LastHitCount { get; private set; }

        private void LateUpdate()
        {
            if (target == null)
            {
                LastHitCount = 0;
                hasQueryState = false;
                RestoreAllRenderers();
                return;
            }

            if (HasQueryChanged())
            {
                if (hasQueryState && lastCullWholeLodGroup != cullWholeLodGroup)
                {
                    colliderRendererCache.Clear();
                }

                blockedThisFrame.Clear();
                LastHitCount = 0;
                CollectBlockingRenderers();
                RememberQueryState();
            }

            if (blockedThisFrame.Count > 0 || originalRenderingStates.Count > 0)
            {
                ApplyVisibility();
            }
        }

        private bool HasQueryChanged()
        {
            return !hasQueryState ||
                target != lastTarget ||
                target.position != lastTargetPosition ||
                transform.position != lastCameraPosition ||
                targetViewOffset != lastTargetViewOffset ||
                !Mathf.Approximately(probeRadius, lastProbeRadius) ||
                occluderMask.value != lastOccluderMask ||
                cullWholeLodGroup != lastCullWholeLodGroup;
        }

        private void RememberQueryState()
        {
            lastTarget = target;
            lastTargetPosition = target.position;
            lastCameraPosition = transform.position;
            lastTargetViewOffset = targetViewOffset;
            lastProbeRadius = probeRadius;
            lastOccluderMask = occluderMask.value;
            lastCullWholeLodGroup = cullWholeLodGroup;
            hasQueryState = true;
        }

        private void CollectBlockingRenderers()
        {
            Vector3 origin = target.position + targetViewOffset;
            Vector3 toCamera = transform.position - origin;
            float distance = toCamera.magnitude;
            if (distance <= 0.01f)
            {
                return;
            }

            LastHitCount = Physics.SphereCastNonAlloc(
                origin,
                probeRadius,
                toCamera / distance,
                hitBuffer,
                distance,
                occluderMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < LastHitCount; i++)
            {
                Collider hitCollider = hitBuffer[i].collider;
                if (hitCollider == null)
                {
                    continue;
                }

                Transform hitTransform = hitCollider.transform;
                if (hitTransform == target || hitTransform.IsChildOf(target))
                {
                    continue;
                }

                Renderer[] renderers = GetRenderersForCollider(hitCollider);
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Renderer renderer = renderers[rendererIndex];
                    if (renderer != null)
                    {
                        blockedThisFrame.Add(renderer);
                    }
                }
            }
        }

        private Renderer[] GetRenderersForCollider(Collider hitCollider)
        {
            Renderer[] renderers;
            if (colliderRendererCache.TryGetValue(hitCollider, out renderers))
            {
                return renderers;
            }

            Transform hitTransform = hitCollider.transform;
            LODGroup lodGroup = cullWholeLodGroup
                ? hitTransform.GetComponentInParent<LODGroup>()
                : null;

            if (lodGroup != null)
            {
                renderers = GetLodRenderers(lodGroup);
            }
            else
            {
                Renderer renderer = hitTransform.GetComponent<Renderer>();
                if (renderer == null)
                {
                    renderer = hitTransform.GetComponentInParent<Renderer>();
                }

                renderers = renderer != null ? new[] { renderer } : NoRenderers;
            }

            colliderRendererCache[hitCollider] = renderers;
            return renderers;
        }

        private Renderer[] GetLodRenderers(LODGroup lodGroup)
        {
            Renderer[] cachedRenderers;
            if (lodRendererCache.TryGetValue(lodGroup, out cachedRenderers))
            {
                return cachedRenderers;
            }

            LOD[] levels = lodGroup.GetLODs();
            int rendererCount = 0;
            for (int levelIndex = 0; levelIndex < levels.Length; levelIndex++)
            {
                rendererCount += levels[levelIndex].renderers.Length;
            }

            Renderer[] renderers = new Renderer[rendererCount];
            int destinationIndex = 0;
            for (int levelIndex = 0; levelIndex < levels.Length; levelIndex++)
            {
                Renderer[] levelRenderers = levels[levelIndex].renderers;
                for (int rendererIndex = 0; rendererIndex < levelRenderers.Length; rendererIndex++)
                {
                    renderers[destinationIndex++] = levelRenderers[rendererIndex];
                }
            }

            lodRendererCache[lodGroup] = renderers;
            return renderers;
        }

        private void ApplyVisibility()
        {
            float now = Time.unscaledTime;

            foreach (Renderer renderer in blockedThisFrame)
            {
                if (!originalRenderingStates.ContainsKey(renderer))
                {
                    originalRenderingStates.Add(renderer, renderer.forceRenderingOff);
                }

                renderer.forceRenderingOff = true;
                lastBlockedTimes[renderer] = now;
            }

            restoreBuffer.Clear();
            foreach (KeyValuePair<Renderer, bool> entry in originalRenderingStates)
            {
                Renderer renderer = entry.Key;
                if (renderer == null)
                {
                    restoreBuffer.Add(renderer);
                    continue;
                }

                float lastBlockedTime;
                if (!lastBlockedTimes.TryGetValue(renderer, out lastBlockedTime) ||
                    now - lastBlockedTime >= restoreDelay)
                {
                    renderer.forceRenderingOff = entry.Value;
                    restoreBuffer.Add(renderer);
                }
            }

            for (int i = 0; i < restoreBuffer.Count; i++)
            {
                Renderer renderer = restoreBuffer[i];
                originalRenderingStates.Remove(renderer);
                lastBlockedTimes.Remove(renderer);
            }
        }

        private void OnDisable()
        {
            RestoreAllRenderers();
        }

        private void OnDestroy()
        {
            RestoreAllRenderers();
        }

        private void RestoreAllRenderers()
        {
            foreach (KeyValuePair<Renderer, bool> entry in originalRenderingStates)
            {
                if (entry.Key != null)
                {
                    entry.Key.forceRenderingOff = entry.Value;
                }
            }

            originalRenderingStates.Clear();
            lastBlockedTimes.Clear();
            blockedThisFrame.Clear();
            colliderRendererCache.Clear();
            lodRendererCache.Clear();
            restoreBuffer.Clear();
            hasQueryState = false;
        }

        private void OnValidate()
        {
            probeRadius = Mathf.Max(0.01f, probeRadius);
            restoreDelay = Mathf.Max(0f, restoreDelay);
        }
    }
}
