Shader "XiYouJi/VFX/Fluid Cloud Surface"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.88, 0.94, 1, 1)
        _ShadowColor ("Shadow Color", Color) = (0.32, 0.43, 0.57, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.76
        _BaseCoverage ("Minimum Coverage", Range(0, 1)) = 0.46
        _NoiseScale ("Noise Scale", Float) = 0.08
        _Threshold ("Coverage Threshold", Range(0, 1)) = 0.36
        _Softness ("Coverage Softness", Range(0.01, 0.8)) = 0.3
        _Wind ("Wind XY", Vector) = (0.016, 0.006, 0, 0)
        _FlowDistortion ("Fluid Distortion", Range(0, 0.3)) = 0.08
        _FlowDisplacement ("Vertex Displacement", Range(0, 0.2)) = 0.035
        _ClearStrength ("Clear Strength", Range(0, 1)) = 0.96
        _HeightAmplitude ("Height Amplitude", Range(0, 1)) = 0.16
        _DistanceFadeStart ("Distance Fade Start", Float) = 60
        _DistanceFadeEnd ("Distance Fade End", Float) = 120
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent-20"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Cull Back
        ZWrite Off
        ZTest LEqual
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            sampler2D _FluidCloudVelocityTex;
            sampler2D _FluidCloudDensityTex;
            float4 _FluidCloudCenterSize;
            float4 _FluidCloudInteractor;
            float _FluidCloudActive;

            fixed4 _TopColor;
            fixed4 _ShadowColor;
            half _Opacity;
            half _BaseCoverage;
            float _NoiseScale;
            half _Threshold;
            half _Softness;
            float4 _Wind;
            half _FlowDistortion;
            half _FlowDisplacement;
            half _ClearStrength;
            half _HeightAmplitude;
            float _DistanceFadeStart;
            float _DistanceFadeEnd;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 worldPosition : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };

            float Hash21(float2 samplePosition)
            {
                samplePosition = frac(samplePosition * float2(123.34, 456.21));
                samplePosition += dot(samplePosition, samplePosition + 45.32);
                return frac(samplePosition.x * samplePosition.y);
            }

            float Noise2D(float2 samplePosition)
            {
                float2 cell = floor(samplePosition);
                float2 localPosition = frac(samplePosition);
                localPosition = localPosition * localPosition * (3.0 - 2.0 * localPosition);
                float bottom = lerp(Hash21(cell), Hash21(cell + float2(1.0, 0.0)), localPosition.x);
                float top = lerp(Hash21(cell + float2(0.0, 1.0)), Hash21(cell + 1.0), localPosition.x);
                return lerp(bottom, top, localPosition.y);
            }

            half Fbm(float2 samplePosition)
            {
                half value = Noise2D(samplePosition) * 0.55h;
                samplePosition = samplePosition * 2.03 + 17.7;
                value += Noise2D(samplePosition) * 0.29h;
                samplePosition = samplePosition * 2.11 + 9.2;
                value += Noise2D(samplePosition) * 0.16h;
                return value;
            }

            half3 SampleFluid(float2 worldXZ)
            {
                float2 fluidUv = (worldXZ - _FluidCloudCenterSize.xy) * _FluidCloudCenterSize.w + 0.5;
                half inside = step(0.0, fluidUv.x) * step(fluidUv.x, 1.0)
                    * step(0.0, fluidUv.y) * step(fluidUv.y, 1.0) * _FluidCloudActive;
                half2 velocity = tex2Dlod(
                    _FluidCloudVelocityTex,
                    float4(saturate(fluidUv), 0.0, 0.0)).xy * inside;
                half clearance = tex2Dlod(
                    _FluidCloudDensityTex,
                    float4(saturate(fluidUv), 0.0, 0.0)).x * inside;
                return half3(velocity, clearance);
            }

            v2f vert(appdata input)
            {
                v2f output;
                float3 worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
                half3 fluid = SampleFluid(worldPosition.xz);
                worldPosition.xz += fluid.xy * _FlowDisplacement;
                half lowNoise = Noise2D(worldPosition.xz * (_NoiseScale * 0.42) + _Time.y * _Wind.xy);
                worldPosition.y += (lowNoise - 0.5h) * _HeightAmplitude;

                output.worldPosition = worldPosition;
                output.position = UnityWorldToClipPos(worldPosition);
                UNITY_TRANSFER_FOG(output, output.position);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                half3 fluid = SampleFluid(input.worldPosition.xz);
                float2 noisePosition = input.worldPosition.xz * _NoiseScale
                    + _Time.y * _Wind.xy
                    + fluid.xy * _FlowDistortion;
                half cloudNoise = Fbm(noisePosition);
                half detail = Noise2D(noisePosition * 3.7 + float2(5.1, 11.3));
                half densitySignal = cloudNoise * 0.82h + detail * 0.18h;
                half coverage = smoothstep(_Threshold, _Threshold + _Softness, densitySignal);
                half alpha = lerp(_BaseCoverage, 1.0h, coverage) * _Opacity;

                half clearance = saturate(fluid.z);
                half rollingEdge = smoothstep(0.08h, 0.34h, clearance)
                    * (1.0h - smoothstep(0.58h, 0.96h, clearance));
                alpha *= 1.0h - clearance * _ClearStrength;
                alpha = saturate(alpha + rollingEdge * 0.28h);

                float playerDistance = distance(input.worldPosition.xz, _FluidCloudInteractor.xy);
                half playerFade = smoothstep(
                    _FluidCloudInteractor.z * 0.32,
                    _FluidCloudInteractor.z,
                    playerDistance);
                alpha *= lerp(1.0h, playerFade, _FluidCloudActive);

                float cameraDistance = distance(_WorldSpaceCameraPos, input.worldPosition);
                alpha *= 1.0h - smoothstep(_DistanceFadeStart, _DistanceFadeEnd, cameraDistance);

                half lighting = saturate(0.3h + cloudNoise * 0.86h + rollingEdge * 0.58h);
                fixed3 color = lerp(_ShadowColor.rgb, _TopColor.rgb, lighting);
                color = lerp(color, fixed3(0.86, 0.94, 1.0), rollingEdge * 0.78h);

                fixed4 foggedColor = fixed4(color, 1.0h);
                UNITY_APPLY_FOG(input.fogCoord, foggedColor);
                return fixed4(foggedColor.rgb * alpha, alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
