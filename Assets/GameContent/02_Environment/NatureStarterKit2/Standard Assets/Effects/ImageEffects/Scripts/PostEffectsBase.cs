using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class PostEffectsBase : MonoBehaviour
    {
        protected bool supportHDRTextures = true;
        protected bool supportDX11 = false;
        protected bool isSupported = true;

        protected Material CheckShaderAndCreateMaterial(Shader s, Material m2Create)
        {
            if (!s)
            {
                Debug.Log("Missing shader in " + ToString());
                enabled = false;
                return null;
            }

            if (s.isSupported && m2Create && m2Create.shader == s)
                return m2Create;

            if (!s.isSupported)
            {
                NotSupported();
                Debug.Log("The shader " + s + " on effect " + ToString() + " is not supported on this platform!");
                return null;
            }

            m2Create = new Material(s) { hideFlags = HideFlags.DontSave };
            return m2Create;
        }

        protected Material CreateMaterial(Shader s, Material m2Create)
        {
            if (!s)
            {
                Debug.Log("Missing shader in " + ToString());
                return null;
            }

            if (m2Create && m2Create.shader == s && s.isSupported)
                return m2Create;

            if (!s.isSupported)
                return null;

            m2Create = new Material(s) { hideFlags = HideFlags.DontSave };
            return m2Create;
        }

        private void OnEnable() => isSupported = true;

        protected bool CheckSupport() => CheckSupport(false);

        public virtual bool CheckResources()
        {
            Debug.LogWarning("CheckResources() for " + ToString() + " should be overwritten.");
            return isSupported;
        }

        protected void Start() => CheckResources();

        protected bool CheckSupport(bool needDepth)
        {
            isSupported = true;
            supportHDRTextures = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf);
            supportDX11 = SystemInfo.graphicsShaderLevel >= 50 && SystemInfo.supportsComputeShaders;

            if (!SystemInfo.supportsImageEffects || !SystemInfo.supportsRenderTextures)
            {
                NotSupported();
                return false;
            }

            if (needDepth && !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth))
            {
                NotSupported();
                return false;
            }

            if (needDepth)
                GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;

            return true;
        }

        protected bool CheckSupport(bool needDepth, bool needHdr)
        {
            if (!CheckSupport(needDepth))
                return false;

            if (needHdr && !supportHDRTextures)
            {
                NotSupported();
                return false;
            }

            return true;
        }

        public bool Dx11Support() => supportDX11;

        protected void ReportAutoDisable()
        {
            Debug.LogWarning("The image effect " + ToString() + " has been disabled on the current platform.");
        }

        protected void NotSupported()
        {
            enabled = false;
            isSupported = false;
        }

        protected void DrawBorder(RenderTexture dest, Material material)
        {
            RenderTexture.active = dest;
            GL.PushMatrix();
            GL.LoadOrtho();
            for (int i = 0; i < material.passCount; i++)
            {
                material.SetPass(i);
                float borderX = 1.0f / dest.width;
                float borderY = 1.0f / dest.height;
                GL.Begin(GL.QUADS);
                DrawQuad(0, 0, borderX, 1, 0, 1);
                DrawQuad(1 - borderX, 0, 1, 1, 0, 1);
                DrawQuad(0, 0, 1, borderY, 0, 1);
                DrawQuad(0, 1 - borderY, 1, 1, 0, 1);
                GL.End();
            }
            GL.PopMatrix();
        }

        private static void DrawQuad(float x1, float y1, float x2, float y2, float uv1, float uv2)
        {
            GL.TexCoord2(0, uv1); GL.Vertex3(x1, y1, 0.1f);
            GL.TexCoord2(1, uv1); GL.Vertex3(x2, y1, 0.1f);
            GL.TexCoord2(1, uv2); GL.Vertex3(x2, y2, 0.1f);
            GL.TexCoord2(0, uv2); GL.Vertex3(x1, y2, 0.1f);
        }
    }
}
