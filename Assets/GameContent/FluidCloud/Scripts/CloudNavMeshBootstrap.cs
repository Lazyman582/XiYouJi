using UnityEngine;
using UnityEngine.AI;

namespace XiYouJi.VFX.FluidCloud
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class CloudNavMeshBootstrap : MonoBehaviour
    {
        public NavMeshData navMeshData;
        public Vector3 worldPosition;
        public Quaternion worldRotation = Quaternion.identity;

        private NavMeshDataInstance instance;

        private void OnEnable()
        {
            if (navMeshData == null)
            {
                return;
            }

            instance = NavMesh.AddNavMeshData(navMeshData, worldPosition, worldRotation);
        }

        private void OnDisable()
        {
            if (instance.valid)
            {
                instance.Remove();
            }
        }
    }
}
