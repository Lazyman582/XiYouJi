Shader "UI/Black Smoke Cover"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _SmokeProgress ("Smoke Progress", Range(0,1)) = 0
        _SmokeOpacity ("Smoke Opacity", Range(0,1)) = 0.92
        _SmokeNoiseScale ("Smoke Noise Scale", Range(1,30)) = 7
        _SmokeSoftness ("Smoke Edge Softness", Range(0.005,0.5)) = 0.08
        _SmokeScrollSpeed ("Smoke Scroll Speed", Vector) = (0.08,0.32,0,0)
        _SmokeColor ("Smoke Color", Color) = (0,0,0,1)
        [Toggle] _OverlayMode ("Full Screen Transition", Float) = 0
        _SmokeTime ("Unscaled Time", Float) = 0
        _SmokeAspect ("Screen Aspect", Float) = 1.77778

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "SmokeCover"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _SmokeProgress;
            float _SmokeOpacity;
            float _SmokeNoiseScale;
            float _SmokeSoftness;
            float4 _SmokeScrollSpeed;
            fixed4 _SmokeColor;
            float _OverlayMode;
            float _SmokeTime;
            float _SmokeAspect;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float noise2d(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float smokeNoise(float2 uv)
            {
                float t = lerp(_Time.y, _SmokeTime, _OverlayMode);
                float2 drift = _SmokeScrollSpeed.xy * t;
                float n = noise2d(uv * _SmokeNoiseScale + drift);
                n += noise2d(uv * (_SmokeNoiseScale * 2.1) - drift * 1.7) * 0.45;
                n += noise2d(uv * (_SmokeNoiseScale * 4.3) + drift * 2.3) * 0.2;
                return saturate(n / 1.65);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 baseColor = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;

                float progress = saturate(_SmokeProgress);
                float noise = smokeNoise(i.texcoord);
                float softness = max(_SmokeSoftness, 0.005);
                float boundary = progress + (noise - 0.5) * softness * 2.6;

                // Smoke rises from the bottom. At progress 0 the image is clear;
                // at progress 1 the entire image is covered.
                float cover = 1.0 - smoothstep(boundary - softness, boundary + softness, i.texcoord.y);
                cover *= step(0.001, progress);
                cover = lerp(cover, 1.0, step(0.999, progress));

                float wisps = saturate(0.62 + noise * 0.55);
                float smokeAlpha = saturate(cover * _SmokeOpacity * wisps);
                baseColor.rgb = lerp(baseColor.rgb, _SmokeColor.rgb, smokeAlpha);

                if (_OverlayMode > 0.5)
                {
                    float2 uv = i.texcoord;
                    uv.x *= _SmokeAspect;
                    // Domain-warped layers give the front rolling, uneven smoke edges.
                    float2 drift = _SmokeScrollSpeed.xy * _SmokeTime;
                    float2 warp = float2(noise2d(uv * 3.0 + drift), noise2d(uv * 3.0 - drift + 8.7));
                    float billow = smokeNoise(uv + (warp - 0.5) * 0.45);
                    float front = lerp(-0.65, 1.65, progress);
                    float field = i.texcoord.y + (billow - 0.5) * 0.9;
                    float coverage = 1.0 - smoothstep(front - softness, front + softness, field);
                    float depth = saturate((front - field) / (softness * 3.0));
                    float alpha = coverage * lerp(0.5 + billow * 0.4, 1.0, depth) * _SmokeOpacity;
                    // Endpoint guarantees: no white rectangle at 0, no visible scene at 1.
                    alpha = lerp(alpha, 1.0, smoothstep(0.78, 1.0, progress));
                    alpha *= smoothstep(0.0, 0.025, progress);
                    float shading = (0.015 + billow * 0.055) * (1.0 - depth);
                    shading *= 1.0 - smoothstep(0.78, 1.0, progress);
                    baseColor = fixed4(_SmokeColor.rgb + shading, saturate(alpha) * i.color.a);
                }

                #ifdef UNITY_UI_CLIP_RECT
                baseColor.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(baseColor.a - 0.001);
                #endif

                return baseColor;
            }
            ENDHLSL
        }
    }
}
