using UnityEngine;

namespace DiscoDialoguePrototype
{
    public class IsometricCameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 12f, -10f);
        public float followSharpness = 8f;
        public float minHeight = 7f;
        public float maxHeight = 17f;

        private void LateUpdate()
        {
            if (target == null) return;
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.01f)
            {
                offset *= Mathf.Clamp(1f - wheel * 0.08f, 0.75f, 1.25f);
                offset.y = Mathf.Clamp(offset.y, minHeight, maxHeight);
            }
            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
            transform.LookAt(target.position + Vector3.up * 0.5f);
        }
    }
}
