Shader "XiYouJi/PlayerFootRing"
{
    Properties
    {
        _Color ("Ring Color", Color) = (0.05, 0.8, 1, 1)
        _Radius ("Radius", Range(0.1, 0.95)) = 0.72
        _Thickness ("Thickness", Range(0.01, 0.3)) = 0.07
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.2)) = 0.025
        _Alpha ("Alpha", Range(0, 1)) = 0.8
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
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
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            float _Radius;
            float _Thickness;
            float _EdgeSoftness;
            float _Alpha;

            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 centered = input.uv * 2.0 - 1.0;
                float distanceFromCenter = length(centered);
                float outer = 1.0 - smoothstep(_Radius, _Radius + _EdgeSoftness, distanceFromCenter);
                float inner = 1.0 - smoothstep(_Radius - _Thickness, _Radius - _Thickness + _EdgeSoftness, distanceFromCenter);
                float ring = saturate(outer - inner);
                return fixed4(_Color.rgb, _Color.a * _Alpha * ring);
            }
            ENDCG
        }
    }
}
