Shader "XiYouJi/Demo Model Hover Outline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1,0.77,0.15,1)
        _OutlineWidth ("Width in pixels", Range(1,10)) = 4
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            Name "SilhouetteMask"
            Cull Off
            ZWrite Off
            ZTest LEqual
            ColorMask 0
            Stencil { Ref 64 Comp Always Pass Replace WriteMask 64 }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 vert(float4 position : POSITION) : SV_POSITION { return UnityObjectToClipPos(position); }
            fixed4 frag() : SV_Target { return 0; }
            ENDCG
        }
        Pass
        {
            Name "OuterOutline"
            Cull Front
            ZWrite Off
            ZTest LEqual
            Stencil { Ref 64 Comp NotEqual Pass Keep ReadMask 64 WriteMask 0 }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _OutlineWidth;
            fixed4 _OutlineColor;
            struct Input { float4 position : POSITION; float3 normal : NORMAL; };
            float4 vert(Input input) : SV_POSITION
            {
                float4 clipPosition = UnityObjectToClipPos(input.position);
                float3 worldNormal = UnityObjectToWorldNormal(input.normal);
                float3 viewNormal = mul((float3x3)UNITY_MATRIX_V, worldNormal);
                float2 direction = mul((float2x2)UNITY_MATRIX_P, viewNormal.xy);
                direction *= _ScreenParams.xy;
                direction /= max(length(direction), 0.0001);
                clipPosition.xy += direction * (2.0 * _OutlineWidth / _ScreenParams.xy) * clipPosition.w;
                return clipPosition;
            }
            fixed4 frag() : SV_Target { return _OutlineColor; }
            ENDCG
        }
    }
}
