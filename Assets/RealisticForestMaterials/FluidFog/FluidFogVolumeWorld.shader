Shader "Hidden/RealisticForest/FluidFogVolumeWorld"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.56, 0.66, 0.70, 1)
        _DensityTex ("Density Field", 3D) = "white" {}
        _DensityScale ("Density Scale", Float) = 1
        _RaymarchSteps ("Raymarch Steps", Float) = 28
        _Anisotropy ("Anisotropy", Range(-0.85, 0.85)) = 0.48
        _SunScatter ("Sun Scatter", Float) = 1
        _AmbientScatter ("Ambient Scatter", Float) = 0.35
    }

    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Front
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler3D _DensityTex;
            sampler2D _CameraDepthTexture;
            float4 _FogColor;
            float3 _VolumeMin;
            float3 _VolumeMax;
            float3 _SunDirection;
            float4 _SunColor;
            float _DensityScale;
            float _RaymarchSteps;
            float _Anisotropy;
            float _SunScatter;
            float _AmbientScatter;
            float _SimTime;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.vertex = UnityWorldToClipPos(o.worldPos);
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            float2 RayBox(float3 origin, float3 direction, float3 boxMin, float3 boxMax)
            {
                float3 safeDirection = float3(
                    abs(direction.x) < 0.0001 ? (direction.x < 0 ? -0.0001 : 0.0001) : direction.x,
                    abs(direction.y) < 0.0001 ? (direction.y < 0 ? -0.0001 : 0.0001) : direction.y,
                    abs(direction.z) < 0.0001 ? (direction.z < 0 ? -0.0001 : 0.0001) : direction.z);
                float3 inverseDirection = 1.0 / safeDirection;
                float3 t0 = (boxMin - origin) * inverseDirection;
                float3 t1 = (boxMax - origin) * inverseDirection;
                float3 nearValues = min(t0, t1);
                float3 farValues = max(t0, t1);
                return float2(max(max(nearValues.x, nearValues.y), nearValues.z),
                              min(min(farValues.x, farValues.y), farValues.z));
            }

            float PhaseFunction(float cosAngle)
            {
                float g = clamp(_Anisotropy, -0.85, 0.85);
                float g2 = g * g;
                float denominator = pow(max(0.001, 1.0 + g2 - 2.0 * g * cosAngle), 1.5);
                return (1.0 - g2) / (4.0 * UNITY_PI * denominator);
            }

            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDirection = normalize(i.worldPos - rayOrigin);
                float2 intersection = RayBox(rayOrigin, rayDirection, _VolumeMin, _VolumeMax);
                if (intersection.y <= max(intersection.x, 0.0))
                    discard;

                float startDistance = max(intersection.x, 0.0);
                float endDistance = intersection.y;

                // The depth texture only clips the world-space ray at solid scene geometry.
                // The fog itself is still rendered from the 3D volume object, not from a full-screen image.
                float2 screenUv = i.screenPos.xy / max(i.screenPos.w, 0.0001);
                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUv);
                float sceneEyeDepth = LinearEyeDepth(rawDepth);
                float3 cameraForward = normalize(UNITY_MATRIX_V[2].xyz);
                float forwardCosine = max(0.05, dot(rayDirection, cameraForward));
                float sceneDistance = sceneEyeDepth / forwardCosine;
                endDistance = min(endDistance, sceneDistance);
                if (endDistance <= startDistance)
                    discard;

                int steps = clamp((int)_RaymarchSteps, 4, 64);
                float stepLength = (endDistance - startDistance) / steps;
                float transmittance = 1.0;
                float3 scatteredLight = 0.0;
                float3 sunDirection = normalize(_SunDirection);
                float phase = PhaseFunction(dot(rayDirection, sunDirection));
                float jitter = Hash31(rayOrigin + i.worldPos + _SimTime) - 0.5;

                [loop]
                for (int stepIndex = 0; stepIndex < 64; stepIndex++)
                {
                    if (stepIndex >= steps)
                        break;

                    float distanceAlongRay = startDistance + (stepIndex + 0.5 + jitter * 0.55) * stepLength;
                    float3 sampleWorld = rayOrigin + rayDirection * distanceAlongRay;
                    float3 uvw = saturate((sampleWorld - _VolumeMin) / max(_VolumeMax - _VolumeMin, 0.0001));
                    float density = tex3Dlod(_DensityTex, float4(uvw, 0)).r * _DensityScale;
                    float opticalDepth = density * stepLength * 0.045;
                    float sampleTransmittance = exp(-opticalDepth);
                    float sunLuminance = dot(_SunColor.rgb, float3(0.2126, 0.7152, 0.0722));
                    float3 balancedSun = lerp(float3(sunLuminance, sunLuminance, sunLuminance), _SunColor.rgb, 0.18);
                    float3 localLight = _FogColor.rgb * _AmbientScatter;
                    localLight += balancedSun * (_SunScatter * phase);
                    scatteredLight += transmittance * localLight * (1.0 - sampleTransmittance);
                    transmittance *= sampleTransmittance;

                    if (transmittance < 0.01)
                        break;
                }

                float alpha = saturate(1.0 - transmittance);
                return float4(scatteredLight, alpha);
            }
            ENDCG
        }
    }
}
