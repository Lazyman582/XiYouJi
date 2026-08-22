Shader "XiYouJi/VFX/Fluid Cloud Volume"
{
    Properties
    {
        _NoiseTex3D ("Generated 3D Noise", 3D) = "white" {}
        _TopColor ("Top Color", Color) = (0.91, 0.96, 1, 1)
        _ShadowColor ("Shadow Color", Color) = (0.3, 0.4, 0.55, 1)
        _Density ("Density", Range(0, 8)) = 3.2
        _Threshold ("Noise Threshold", Range(0, 1)) = 0.38
        _Softness ("Noise Softness", Range(0.01, 0.5)) = 0.24
        _NoiseScale ("Noise Scale", Float) = 1.8
        _Wind ("Wind XYZ", Vector) = (0.018, 0.004, 0.009, 0)
        _FlowDistortion ("Fluid Distortion", Range(0, 0.2)) = 0.045
        _ClearStrength ("Clear Strength", Range(0, 1)) = 0.95
        _StepCount ("Ray Steps", Range(8, 48)) = 24
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+10"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Cull Front
        ZWrite Off
        ZTest LEqual
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.0
            #include "UnityCG.cginc"

            sampler3D _NoiseTex3D;
            sampler2D _FluidCloudVelocityTex;
            sampler2D _FluidCloudDensityTex;
            float4 _FluidCloudCenterSize;
            float4 _FluidCloudInteractor;
            float _FluidCloudActive;

            fixed4 _TopColor;
            fixed4 _ShadowColor;
            half _Density;
            half _Threshold;
            half _Softness;
            half _NoiseScale;
            float4 _Wind;
            half _FlowDistortion;
            half _ClearStrength;
            half _StepCount;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 worldPosition : TEXCOORD0;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
                return output;
            }

            float2 RayBox(float3 rayOrigin, float3 rayDirection)
            {
                float3 safeDirection = sign(rayDirection) * max(abs(rayDirection), 1e-5);
                float3 inverseDirection = 1.0 / safeDirection;
                float3 t0 = (-0.5 - rayOrigin) * inverseDirection;
                float3 t1 = (0.5 - rayOrigin) * inverseDirection;
                float3 tMin = min(t0, t1);
                float3 tMax = max(t0, t1);
                float nearDistance = max(max(tMin.x, tMin.y), tMin.z);
                float farDistance = min(min(tMax.x, tMax.y), tMax.z);
                return float2(nearDistance, farDistance);
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

            fixed4 frag(v2f input) : SV_Target
            {
                float3 rayDirectionWorld = normalize(input.worldPosition - _WorldSpaceCameraPos);
                float3 rayOriginObject = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
                float3 rayDirectionObject = normalize(mul(unity_WorldToObject, float4(rayDirectionWorld, 0.0)).xyz);
                float2 hit = RayBox(rayOriginObject, rayDirectionObject);
                float startDistance = max(hit.x, 0.0);
                float distanceInside = hit.y - startDistance;
                if (distanceInside <= 0.0) discard;

                int stepCount = clamp((int)_StepCount, 8, 48);
                float stepLength = distanceInside / stepCount;
                float jitter = frac(sin(dot(input.position.xy, float2(12.9898, 78.233))) * 43758.5453);
                float3 samplePositionObject = rayOriginObject
                    + rayDirectionObject * (startDistance + stepLength * jitter);

                half transmittance = 1.0h;
                half3 accumulatedColor = 0.0h;

                [loop]
                for (int stepIndex = 0; stepIndex < 48; stepIndex++)
                {
                    if (stepIndex >= stepCount || transmittance < 0.025h) break;

                    float3 worldPosition = mul(unity_ObjectToWorld, float4(samplePositionObject, 1.0)).xyz;
                    half3 fluid = SampleFluid(worldPosition.xz);
                    float3 noisePosition = (samplePositionObject + 0.5) * _NoiseScale
                        + _Time.y * _Wind.xyz
                        + float3(fluid.x, 0.0, fluid.y) * _FlowDistortion;
                    half noiseValue = tex3D(_NoiseTex3D, frac(noisePosition)).r;

                    half height01 = saturate(samplePositionObject.y + 0.5);
                    half heightShape = smoothstep(0.0h, 0.18h, height01)
                        * (1.0h - smoothstep(0.68h, 1.0h, height01));
                    half shape = smoothstep(_Threshold, _Threshold + _Softness, noiseValue) * heightShape;
                    half clearance = saturate(fluid.z);

                    float playerDistance = distance(worldPosition.xz, _FluidCloudInteractor.xy);
                    half playerFade = smoothstep(
                        _FluidCloudInteractor.z * 0.28,
                        _FluidCloudInteractor.z,
                        playerDistance);
                    shape *= lerp(1.0h, playerFade, _FluidCloudActive);

                    half localDensity = shape * (1.0h - clearance * _ClearStrength) * _Density;
                    half alphaStep = 1.0h - exp(-localDensity * stepLength);
                    half lighting = saturate(0.28h + noiseValue * 0.72h + height01 * 0.18h);
                    half3 sampleColor = lerp(_ShadowColor.rgb, _TopColor.rgb, lighting);
                    accumulatedColor += transmittance * alphaStep * sampleColor;
                    transmittance *= 1.0h - alphaStep;

                    samplePositionObject += rayDirectionObject * stepLength;
                }

                half alpha = saturate(1.0h - transmittance);
                return fixed4(accumulatedColor, alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
