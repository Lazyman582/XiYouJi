Shader "Hidden/RealisticForest/FluidFogComposite"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _DensityTex ("Density", 3D) = "black" {}
        _FogColor ("Fog Color", Color) = (0.56, 0.66, 0.70, 1)
        _DensityScale ("Density Scale", Float) = 1.35
        _RaymarchSteps ("Raymarch Steps", Float) = 28
        _Anisotropy ("Anisotropy", Float) = 0.48
        _SunScatter ("Sun Scatter", Float) = 1.1
        _AmbientScatter ("Ambient Scatter", Float) = 0.38
        _VolumeMin ("Volume Min", Vector) = (-125, -13, -115, 0)
        _VolumeMax ("Volume Max", Vector) = (125, 57, 115, 0)
        _SunDirection ("Sun Direction", Vector) = (0, -1, 0, 0)
        _SunColor ("Sun Color", Color) = (1, 1, 1, 1)
        _SimTime ("Simulation Time", Float) = 0
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            sampler3D _DensityTex;
            float4 _MainTex_TexelSize;
            float4 _FogColor;
            float _DensityScale;
            float _RaymarchSteps;
            float _Anisotropy;
            float _SunScatter;
            float _AmbientScatter;
            float3 _VolumeMin;
            float3 _VolumeMax;
            float3 _SunDirection;
            float4 _SunColor;
            float _SimTime;

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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0)
                    o.uv.y = 1.0 - o.uv.y;
                #endif
                return o;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float PhaseHenyeyGreenstein(float cosAngle, float g)
            {
                float g2 = g * g;
                float denominator = pow(max(0.001, 1.0 + g2 - 2.0 * g * cosAngle), 1.5);
                return (1.0 - g2) / (12.5663706 * denominator);
            }

            float3 WorldRay(float2 uv)
            {
                float4 clip = float4(uv * 2.0 - 1.0, 1.0, 1.0);
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0)
                    clip.y = -clip.y;
                #endif
                float4 view = mul(unity_CameraInvProjection, clip);
                view.xyz /= max(0.0001, view.w);
                return normalize(mul((float3x3)unity_CameraToWorld, view.xyz));
            }

            bool IntersectVolume(float3 origin, float3 direction, out float nearDistance, out float farDistance)
            {
                float3 invDirection = 1.0 / max(abs(direction), 0.0001) * sign(direction);
                float3 t0 = (_VolumeMin - origin) * invDirection;
                float3 t1 = (_VolumeMax - origin) * invDirection;
                float3 tMin = min(t0, t1);
                float3 tMax = max(t0, t1);
                nearDistance = max(max(tMin.x, tMin.y), tMin.z);
                farDistance = min(min(tMax.x, tMax.y), tMax.z);
                return farDistance > max(nearDistance, 0.0);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 source = tex2D(_MainTex, i.uv);
                float3 origin = _WorldSpaceCameraPos;
                float3 direction = WorldRay(i.uv);
                float nearDistance;
                float farDistance;
                if (!IntersectVolume(origin, direction, nearDistance, farDistance))
                    return source;

                nearDistance = max(nearDistance, 0.0);
                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                float eyeDepth = LinearEyeDepth(rawDepth);
                float3 cameraForward = mul((float3x3)unity_CameraToWorld, float3(0, 0, 1));
                float cosForward = max(0.1, dot(direction, cameraForward));
                float sceneDistance = rawDepth < 0.99999 ? eyeDepth / cosForward : 10000.0;
                farDistance = min(farDistance, sceneDistance);
                if (farDistance <= nearDistance)
                    return source;

                int steps = min(64, max(4, (int)_RaymarchSteps));
                float segmentLength = (farDistance - nearDistance) / steps;
                float jitter = Hash21(i.uv * _ScreenParams.xy + _SimTime) - 0.5;
                float transmittance = 1.0;
                float3 fogLight = 0.0;
                float viewLightCos = dot(-direction, normalize(_SunDirection));
                float phase = PhaseHenyeyGreenstein(viewLightCos, _Anisotropy);
                float3 scatterColor = _FogColor.rgb * _AmbientScatter + _SunColor.rgb * (_SunScatter * phase);

                [loop]
                for (int s = 0; s < 64; s++)
                {
                    if (s >= steps)
                        break;

                    float distanceAlongRay = nearDistance + (s + 0.5 + jitter) * segmentLength;
                    float3 worldPosition = origin + direction * distanceAlongRay;
                    float3 uvw = saturate((worldPosition - _VolumeMin) / (_VolumeMax - _VolumeMin));
                    float density = tex3D(_DensityTex, uvw).r * _DensityScale;
                    float extinction = 1.0 - exp(-density * segmentLength * 0.08);
                    fogLight += transmittance * extinction * scatterColor;
                    transmittance *= 1.0 - extinction;
                    if (transmittance < 0.01)
                        break;
                }

                float fogAmount = saturate(1.0 - transmittance);
                float3 result = source.rgb * transmittance + fogLight;
                return float4(result, source.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
