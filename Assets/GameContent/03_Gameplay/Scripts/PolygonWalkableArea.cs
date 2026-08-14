using System.Collections.Generic;
using UnityEngine;

namespace XiYouJi.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PolygonWalkableArea : MonoBehaviour
    {
        [SerializeField]
        private List<Vector2> localPoints = new List<Vector2>
        {
            new Vector2(-10f, -10f),
            new Vector2(-10f, 10f),
            new Vector2(10f, 10f),
            new Vector2(10f, -10f)
        };

        [SerializeField]
        [Min(0f)]
        private float boundaryTolerance = 0.12f;

        [SerializeField]
        private bool showWhenNotSelected = true;

        public int PointCount => localPoints == null ? 0 : localPoints.Count;
        public float BoundaryTolerance => boundaryTolerance;

        public Vector2 GetLocalPoint(int index)
        {
            return localPoints[index];
        }

        public Vector3 GetWorldPoint(int index)
        {
            Vector2 point = localPoints[index];
            return transform.TransformPoint(new Vector3(point.x, 0f, point.y));
        }

        public void SetLocalPoint(int index, Vector2 point)
        {
            localPoints[index] = point;
        }

        public void InsertLocalPoint(int index, Vector2 point)
        {
            localPoints.Insert(Mathf.Clamp(index, 0, PointCount), point);
        }

        public void RemoveLocalPointAt(int index)
        {
            if (PointCount <= 3 || index < 0 || index >= PointCount)
            {
                return;
            }

            localPoints.RemoveAt(index);
        }

        public void SetRectangle(Vector2 size)
        {
            size.x = Mathf.Max(0.1f, size.x);
            size.y = Mathf.Max(0.1f, size.y);
            Vector2 half = size * 0.5f;

            localPoints = new List<Vector2>
            {
                new Vector2(-half.x, -half.y),
                new Vector2(-half.x, half.y),
                new Vector2(half.x, half.y),
                new Vector2(half.x, -half.y)
            };
        }

        public bool Contains(Vector3 worldPosition)
        {
            if (PointCount < 3)
            {
                return true;
            }

            Vector3 local = transform.InverseTransformPoint(worldPosition);
            return ContainsLocal(new Vector2(local.x, local.z));
        }

        public bool ContainsPath(Vector3 start, Vector3[] corners, float maximumSampleSpacing)
        {
            if (PointCount < 3)
            {
                return true;
            }

            if (!Contains(start))
            {
                return false;
            }

            if (corners == null || corners.Length == 0)
            {
                return true;
            }

            maximumSampleSpacing = Mathf.Max(0.05f, maximumSampleSpacing);
            Vector3 previous = start;
            for (int i = 0; i < corners.Length; i++)
            {
                if (!ContainsSegment(previous, corners[i], maximumSampleSpacing))
                {
                    return false;
                }

                previous = corners[i];
            }

            return true;
        }

        public Vector3 ClosestPointOnBoundary(Vector3 worldPosition)
        {
            if (PointCount == 0)
            {
                return worldPosition;
            }

            Vector3 local3 = transform.InverseTransformPoint(worldPosition);
            Vector2 local = new Vector2(local3.x, local3.z);
            Vector2 closest = localPoints[0];
            float closestSqrDistance = float.PositiveInfinity;

            for (int i = 0; i < PointCount; i++)
            {
                Vector2 a = localPoints[i];
                Vector2 b = localPoints[(i + 1) % PointCount];
                Vector2 candidate = ClosestPointOnSegment(local, a, b);
                float sqrDistance = (candidate - local).sqrMagnitude;
                if (sqrDistance < closestSqrDistance)
                {
                    closestSqrDistance = sqrDistance;
                    closest = candidate;
                }
            }

            Vector3 result = transform.TransformPoint(new Vector3(closest.x, 0f, closest.y));
            result.y = worldPosition.y;
            return result;
        }

        private bool ContainsSegment(Vector3 worldStart, Vector3 worldEnd, float maximumSampleSpacing)
        {
            Vector2 start = new Vector2(worldStart.x, worldStart.z);
            Vector2 end = new Vector2(worldEnd.x, worldEnd.z);
            float distance = Vector2.Distance(start, end);
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / maximumSampleSpacing));

            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                if (!Contains(Vector3.Lerp(worldStart, worldEnd, t)))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ContainsLocal(Vector2 point)
        {
            float toleranceSqr = boundaryTolerance * boundaryTolerance;
            bool inside = false;
            int previousIndex = PointCount - 1;

            for (int i = 0; i < PointCount; i++)
            {
                Vector2 current = localPoints[i];
                Vector2 previous = localPoints[previousIndex];

                if ((ClosestPointOnSegment(point, previous, current) - point).sqrMagnitude <= toleranceSqr)
                {
                    return true;
                }

                bool crossesHorizontalRay = (current.y > point.y) != (previous.y > point.y);
                if (crossesHorizontalRay)
                {
                    float intersectionX = (previous.x - current.x)
                        * (point.y - current.y)
                        / (previous.y - current.y)
                        + current.x;
                    if (point.x < intersectionX)
                    {
                        inside = !inside;
                    }
                }

                previousIndex = i;
            }

            return inside;
        }

        private static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 segment = b - a;
            float sqrLength = segment.sqrMagnitude;
            if (sqrLength <= 0.000001f)
            {
                return a;
            }

            float t = Mathf.Clamp01(Vector2.Dot(point - a, segment) / sqrLength);
            return a + segment * t;
        }

        private void OnValidate()
        {
            boundaryTolerance = Mathf.Max(0f, boundaryTolerance);
            if (localPoints == null)
            {
                localPoints = new List<Vector2>();
            }
        }

        private void OnDrawGizmos()
        {
            if (!showWhenNotSelected || PointCount < 2)
            {
                return;
            }

            DrawOutline(new Color(0.2f, 1f, 0.35f, 0.62f));
        }

        private void OnDrawGizmosSelected()
        {
            if (PointCount < 2)
            {
                return;
            }

            DrawOutline(new Color(1f, 0.78f, 0.12f, 1f));
        }

        private void DrawOutline(Color color)
        {
            Gizmos.color = color;
            for (int i = 0; i < PointCount; i++)
            {
                Vector3 a = GetWorldPoint(i) + Vector3.up * 0.08f;
                Vector3 b = GetWorldPoint((i + 1) % PointCount) + Vector3.up * 0.08f;
                Gizmos.DrawLine(a, b);
            }
        }
    }
}
