Shader "RealisticForest/Falling Leaf Particle"
{
    Properties
    {
        _MainTex ("Existing Leaf Detail", 2D) = "white" {}
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _DetailStrength ("Detail Strength", Range(0, 0.6)) = 0.22
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Tint;
            float _DetailStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float3 worldNormal : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Tint;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed3 textureColor = tex2D(_MainTex, i.uv).rgb;
                fixed detail = dot(textureColor, fixed3(0.2126, 0.7152, 0.0722));
                fixed facingLight = 0.72 + 0.28 * abs(normalize(i.worldNormal).y);
                fixed detailModulation = lerp(1.0 - _DetailStrength, 1.0 + _DetailStrength, detail);
                fixed4 result = fixed4(i.color.rgb * facingLight * detailModulation, i.color.a);
                UNITY_APPLY_FOG(i.fogCoord, result);
                return result;
            }
            ENDCG
        }
    }
}
