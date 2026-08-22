using UnityEngine;

namespace XiYouJi.VFX.FluidCloud
{
    public sealed class FluidCloudDemoUI : MonoBehaviour
    {
        private GUIStyle labelStyle;

        private void OnGUI()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 17,
                    normal = { textColor = new Color(0.9f, 0.95f, 1f, 0.96f) },
                    padding = new RectOffset(14, 14, 9, 9)
                };
            }

            GUI.Box(new Rect(18f, 18f, 470f, 72f), GUIContent.none);
            GUI.Label(
                new Rect(24f, 22f, 460f, 62f),
                "GPU Fluid Cloud (Stable Fluids)\nWASD / Arrow Keys: move; auto demo resumes after 4s",
                labelStyle);
        }
    }
}
