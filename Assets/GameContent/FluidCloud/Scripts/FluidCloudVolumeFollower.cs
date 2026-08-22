using UnityEngine;

namespace XiYouJi.VFX.FluidCloud
{
    [DisallowMultipleComponent]
    public sealed class FluidCloudVolumeFollower : MonoBehaviour
    {
        public Transform target;
        public bool followHeight;

        private float fixedHeight;

        private void Awake()
        {
            fixedHeight = transform.position.y;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 position = target.position;
            if (!followHeight)
            {
                position.y = fixedHeight;
            }

            transform.position = position;
        }
    }
}
