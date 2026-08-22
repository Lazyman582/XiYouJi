Shader "FullScreen/DepthFullScreenShader"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            Name "Built-In Depth Mask"
            ZWrite Off
            ZTest Always
            Cull Off

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                float hasGeometry = step(0.00001, Linear01Depth(rawDepth));
                return fixed4(0.0, 0.0, hasGeometry, 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
