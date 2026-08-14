using System.Collections.Generic;
using UnityEngine;

namespace XiYouJi.Forest
{
    /// <summary>
    /// Adds a lightweight, distance-cropped wind motion to the standalone Tree
    /// objects in the NatureStarterKit scene. Only nearby trees are updated;
    /// distant trees remain completely untouched until the camera approaches.
    /// </summary>
    public sealed class NearbyTreeWindController : MonoBehaviour
    {
        [Header("Distance culling")]
        [Min(1f)] public float activeRadius = 85f;
        [Min(1f)] public float releaseRadius = 105f;
        [Min(0.05f)] public float distanceCheckInterval = 0.15f;

        [Header("Leaf motion")]
        [Range(0f, 12f)] public float maxSwayAngle = 3.2f;
        [Range(0f, 1f)] public float maxSwayPosition = 0.12f;
        [Range(0.05f, 2f)] public float windSpeed = 0.55f;
        public Vector3 windDirection = new Vector3(0.75f, 0f, 0.35f);

        private sealed class TreeState
        {
            public Transform transform;
            public Quaternion baseRotation;
            public Vector3 basePosition;
            public float phase;
            public float scale;
            public bool active;
        }

        private readonly List<TreeState> treeStates = new List<TreeState>();
        private Transform trackedCamera;
        private float nextDistanceCheck;
        private Vector3 normalizedWind;

        private void Awake()
        {
            trackedCamera = Camera.main == null ? null : Camera.main.transform;
            normalizedWind = windDirection.sqrMagnitude < 0.001f ? Vector3.right : windDirection.normalized;

            Tree[] trees = GetComponentsInChildren<Tree>(true);
            treeStates.Capacity = trees.Length;
            for (int i = 0; i < trees.Length; i++)
            {
                Transform tree = trees[i].transform;
                treeStates.Add(new TreeState
                {
                    transform = tree,
                    baseRotation = tree.localRotation,
                    basePosition = tree.localPosition,
                    phase = Hash01(tree.GetInstanceID()) * Mathf.PI * 2f,
                    scale = Mathf.Lerp(0.82f, 1.18f, Hash01(tree.GetInstanceID() * 31))
                });
            }
        }

        private void OnEnable()
        {
            nextDistanceCheck = 0f;
        }

        private void LateUpdate()
        {
            if (trackedCamera == null)
            {
                Camera main = Camera.main;
                if (main != null) trackedCamera = main.transform;
            }

            if (trackedCamera != null && Time.unscaledTime >= nextDistanceCheck)
            {
                UpdateActiveTrees(trackedCamera.position);
                nextDistanceCheck = Time.unscaledTime + distanceCheckInterval;
            }

            float time = Time.unscaledTime * windSpeed;
            for (int i = 0; i < treeStates.Count; i++)
            {
                TreeState state = treeStates[i];
                if (!state.active || state.transform == null) continue;

                float wave = time * state.scale + state.phase;
                float primary = Mathf.Sin(wave);
                float secondary = Mathf.Sin(wave * 0.63f + state.phase * 1.7f);
                float sway = (primary * 0.68f + secondary * 0.32f) * state.scale;
                float side = Mathf.Sin(wave * 0.82f + 1.3f) * maxSwayPosition * 0.35f;

                Vector3 axis = new Vector3(-normalizedWind.z, 0f, normalizedWind.x);
                state.transform.localRotation = state.baseRotation * Quaternion.Euler(
                    sway * maxSwayAngle * 0.52f,
                    side * maxSwayAngle * 0.18f,
                    sway * maxSwayAngle);
                state.transform.localPosition = state.basePosition +
                    normalizedWind * (sway * maxSwayPosition * 0.55f) +
                    axis * (side * maxSwayPosition);
            }
        }

        private void UpdateActiveTrees(Vector3 cameraPosition)
        {
            float activeRadiusSqr = activeRadius * activeRadius;
            float releaseRadiusSqr = Mathf.Max(activeRadius, releaseRadius);
            releaseRadiusSqr *= releaseRadiusSqr;

            for (int i = 0; i < treeStates.Count; i++)
            {
                TreeState state = treeStates[i];
                if (state.transform == null) continue;

                Vector3 delta = state.transform.position - cameraPosition;
                delta.y = 0f;
                float distanceSqr = delta.sqrMagnitude;
                bool shouldBeActive = state.active
                    ? distanceSqr <= releaseRadiusSqr
                    : distanceSqr <= activeRadiusSqr;

                if (state.active == shouldBeActive) continue;
                state.active = shouldBeActive;
                if (!shouldBeActive)
                {
                    state.transform.localRotation = state.baseRotation;
                    state.transform.localPosition = state.basePosition;
                }
            }
        }

        private static float Hash01(int value)
        {
            unchecked
            {
                uint hash = (uint)value;
                hash ^= 2747636419u;
                hash *= 2654435769u;
                hash ^= hash >> 16;
                hash *= 2654435769u;
                hash ^= hash >> 16;
                hash *= 2654435769u;
                return (hash & 0x00FFFFFFu) / 16777216f;
            }
        }
    }
}
