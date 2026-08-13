using UnityEngine;

namespace DiscoDialoguePrototype
{
    public class BillboardLabel : MonoBehaviour
    {
        private void LateUpdate()
        {
            Camera cameraToFace = Camera.main;
            if (cameraToFace == null) return;
            // TextMesh's readable face points toward local -Z, so its forward axis points
            // away from the camera while keeping the glyphs readable.
            transform.forward = cameraToFace.transform.forward;
        }
    }
}
