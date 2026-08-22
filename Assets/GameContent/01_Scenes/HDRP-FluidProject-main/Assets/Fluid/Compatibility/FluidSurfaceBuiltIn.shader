Shader "XiYouJi/Fluid/Surface Built-In"
{
    Properties
    {
        _DensityTex ("Density Texture", 2D) = "black" {}
        _LowColor ("Low Density Color", Color) = (0.15, 0.35, 0.55, 0.0)
        _HighColor ("High Density Color", Color) = (0.55, 0.85, 1.0, 0.9)
        _DensityScale ("Density Scale", Range(0.1, 8.0)) = 1.5
        _Opacity ("Opacity", Range(0.0, 1.0)) = 0.85
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.5)) = 0.08
        _CloudScale ("Cloud Scale", Range(0.5, 20.0)) = 5.0
        _CloudStrength ("Cloud Strength", Range(0.0, 1.0)) = 0.75
        _FlowOffset ("Flow Offset", Vector) = (0, 0, 0, 0)
        _FlowRotation ("Flow Rotation", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

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
                UNITY_FOG_COORDS(1)
            };

            sampler2D _DensityTex;
            float4 _DensityTex_ST;
            fixed4 _LowColor;
            fixed4 _HighColor;
            float _DensityScale;
            float _Opacity;
            float _EdgeSoftness;
            float _CloudScale;
            float _CloudStrength;
            float4 _FlowOffset;
            float _FlowRotation;

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Noise2D(float2 p)
            {
                float2 cell = floor(p);
                float2 local = frac(p);
                local = local * local * (3.0 - 2.0 * local);
                float a = Hash21(cell);
                float b = Hash21(cell + float2(1.0, 0.0));
                float c = Hash21(cell + float2(0.0, 1.0));
                float d = Hash21(cell + float2(1.0, 1.0));
                return lerp(lerp(a, b, local.x), lerp(c, d, local.x), local.y);
            }

            float CloudNoise(float2 uv)
            {
                float value = 0.0;
                float amplitude = 0.5;
                for (int octave = 0; octave < 4; octave++)
                {
                    value += Noise2D(uv) * amplitude;
                    uv = uv * 2.03 + 17.13;
                    amplitude *= 0.5;
                }
                return saturate(value);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _DensityTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float rawDensity = max(tex2D(_DensityTex, i.uv).r, 0.0);
                float density = saturate(pow(rawDensity, 0.35) * _DensityScale);
                float2 flowUv = i.uv - _FlowOffset.xy;
                float2 centeredUv = flowUv - 0.5;
                float sinRotation = sin(_FlowRotation);
                float cosRotation = cos(_FlowRotation);
                flowUv = float2(
                    centeredUv.x * cosRotation - centeredUv.y * sinRotation,
                    centeredUv.x * sinRotation + centeredUv.y * cosRotation) + 0.5;
                float cloud = CloudNoise(flowUv * _CloudScale + _Time.y * 0.015);
                float cloudCoverage = lerp(1.0, 0.55 + cloud * 0.45, _CloudStrength);
                float coverage = smoothstep(0.0, max(_EdgeSoftness, 0.001), density * cloudCoverage);
                fixed4 color = lerp(_LowColor, _HighColor, saturate(density * cloudCoverage));
                color.rgb *= 0.8 + cloud * 0.35;
                color.a = saturate(coverage * _Opacity * _HighColor.a);
                UNITY_APPLY_FOG(i.fogCoord, color);
                return color;
            }
            ENDCG
        }
    }

    Fallback Off
}
