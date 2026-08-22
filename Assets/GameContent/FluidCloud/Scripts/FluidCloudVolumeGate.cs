using UnityEngine;

namespace XiYouJi.VFX.FluidCloud
{
    [DisallowMultipleComponent]
    public sealed class FluidCloudVolumeGate : MonoBehaviour
    {
        public bool disableOnMobile = true;

        private void Awake()
        {
            if ((disableOnMobile && Application.isMobilePlatform) || !SystemInfo.supportsComputeShaders)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
