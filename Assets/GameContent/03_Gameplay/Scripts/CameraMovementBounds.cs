using UnityEngine;
using UnityEngine.Serialization;

namespace XiYouJi.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CameraMovementBounds : MonoBehaviour
    {
        [SerializeField]
        [FormerlySerializedAs("size")]
        private Vector2 worldSize = new Vector2(60f, 60f);

        public Vector2 WorldSize
        {
            get => worldSize;
            set => worldSize = new Vector2(
                Mathf.Max(0.1f, value.x),
                Mathf.Max(0.1f, value.y));
        }

        public Vector3 ClampPosition(Vector3 worldPosition)
        {
            Vector2 halfSize = worldSize * 0.5f;
            worldPosition.x = Mathf.Clamp(worldPosition.x, transform.position.x - halfSize.x, transform.position.x + halfSize.x);
            worldPosition.z = Mathf.Clamp(worldPosition.z, transform.position.z - halfSize.y, transform.position.z + halfSize.y);
            return worldPosition;
        }

        public bool ContainsXZ(Vector3 worldPosition)
        {
            Vector2 halfSize = worldSize * 0.5f;
            return worldPosition.x >= transform.position.x - halfSize.x
                && worldPosition.x <= transform.position.x + halfSize.x
                && worldPosition.z >= transform.position.z - halfSize.y
                && worldPosition.z <= transform.position.z + halfSize.y;
        }

        private void OnValidate()
        {
            WorldSize = worldSize;
        }

        private void OnDrawGizmos()
        {
            Vector3 center = transform.position + Vector3.up * 0.1f;
            Vector3 volumeSize = new Vector3(worldSize.x, 0.2f, worldSize.y);
            Gizmos.color = new Color(0.12f, 0.85f, 1f, 0.10f);
            Gizmos.DrawCube(center, volumeSize);
            Gizmos.color = new Color(0.12f, 0.85f, 1f, 0.85f);
            Gizmos.DrawWireCube(center, volumeSize);
        }
    }
}
