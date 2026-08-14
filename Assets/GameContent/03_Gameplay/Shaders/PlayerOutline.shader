Shader "XiYouJi/PlayerOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0.05, 0.75, 1, 1)
        _OutlineWidth ("Outline Width", Range(0.005, 0.08)) = 0.03
    }

    SubShader
    {
        Tags { "Queue" = "Geometry+10" "RenderType" = "Opaque" }

        Pass
        {
            Cull Front
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
            };

            fixed4 _OutlineColor;
            float _OutlineWidth;

            v2f vert(appdata input)
            {
                v2f output;
                input.vertex.xyz += input.normal * _OutlineWidth;
                output.position = UnityObjectToClipPos(input.vertex);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
}
