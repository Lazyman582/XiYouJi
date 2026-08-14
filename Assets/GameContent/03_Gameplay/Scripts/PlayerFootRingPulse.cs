using UnityEngine;

namespace XiYouJi.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlayerFootRingPulse : MonoBehaviour
    {
        [SerializeField] private float pulseSpeed = 2.2f;
        [SerializeField] private float minimumScale = 0.88f;
        [SerializeField] private float maximumScale = 1.12f;
        [SerializeField] private float minimumAlpha = 0.45f;
        [SerializeField] private float maximumAlpha = 0.9f;

        private Renderer ringRenderer;
        private MaterialPropertyBlock propertyBlock;
        private Vector3 baseScale;

        private void Awake()
        {
            ringRenderer = GetComponent<Renderer>();
            propertyBlock = new MaterialPropertyBlock();
            baseScale = transform.localScale;
        }

        private void Update()
        {
            if (ringRenderer == null)
            {
                return;
            }

            float wave = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
            float scale = Mathf.Lerp(minimumScale, maximumScale, wave);
            float alpha = Mathf.Lerp(minimumAlpha, maximumAlpha, wave);

            transform.localScale = new Vector3(
                baseScale.x * scale,
                baseScale.y * scale,
                baseScale.z);

            ringRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat("_Alpha", alpha);
            ringRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
