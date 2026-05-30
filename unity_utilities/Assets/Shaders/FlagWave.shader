Shader "ShaderUtilities/FlagWave"
{
    Properties
    {
        _BaseMap ("Flag Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _WaveAmplitude ("Wave Amplitude", Range(0, 1)) = 0.42
        _WaveSpeed ("Wave Speed", Range(0, 8)) = 3.2
        _WaveScale ("Wave Scale", Range(0.1, 8)) = 2.4
        _VerticalFlutter ("Vertical Flutter", Range(0, 1)) = 0.12
        _HoistStiffness ("Hoist Stiffness", Range(0.1, 4)) = 1.7
        _LightBoost ("Light Boost", Range(0, 2)) = 0.6
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _WaveAmplitude;
                half _WaveSpeed;
                half _WaveScale;
                half _VerticalFlutter;
                half _HoistStiffness;
                half _LightBoost;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half flutter : TEXCOORD3;
            };

            half HoistMask(float u)
            {
                return saturate(pow(saturate(u), _HoistStiffness));
            }

            half EvaluateFlagWave(float2 uv)
            {
                half mask = HoistMask(uv.x);
                half k = max(_WaveScale, 0.001h);
                half t = _Time.y * _WaveSpeed;

                // Traveling sine waves satisfy u_tt = c^2 * u_xx; the hoist mask
                // damps the fixed edge while preserving the wave motion across the cloth.
                half wave = sin((uv.x * 6.28318h * k) - t);
                wave += 0.42h * sin((uv.x * 9.42477h * k) + uv.y * 3.14159h - t * 1.35h);
                wave += 0.18h * sin((uv.x + uv.y) * 12.56636h - t * 0.72h);
                return wave * _WaveAmplitude * mask;
            }

            float3 DeformPosition(float3 positionOS, float2 uv)
            {
                half wave = EvaluateFlagWave(uv);
                half vertical = sin((uv.x * 5.5h + uv.y * 2.0h) - _Time.y * _WaveSpeed * 1.2h);
                positionOS.z += wave;
                positionOS.y += vertical * _VerticalFlutter * HoistMask(uv.x);
                return positionOS;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 positionOS = DeformPosition(input.positionOS.xyz, input.uv);

                const float normalStep = 0.01;
                float3 positionX = DeformPosition(input.positionOS.xyz + float3(4.6 * normalStep, 0, 0), input.uv + float2(normalStep, 0));
                float3 positionY = DeformPosition(input.positionOS.xyz + float3(0, 2.4 * normalStep, 0), input.uv + float2(0, normalStep));
                half3 tangentX = normalize(positionX - positionOS);
                half3 tangentY = normalize(positionY - positionOS);
                half3 normalOS = normalize(cross(tangentX, tangentY));

                VertexPositionInputs positionInputs = GetVertexPositionInputs(positionOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = TransformObjectToWorldNormal(normalOS);
                output.flutter = saturate(abs(EvaluateFlagWave(input.uv)) / max(_WaveAmplitude, 0.001h));
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 textureSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half rim = pow(1.0h - saturate(dot(normalWS, normalize(GetWorldSpaceViewDir(input.positionWS)))), 2.0h);
                half3 color = textureSample.rgb * _BaseColor.rgb;
                color *= 0.48h + ndotl * (0.62h + _LightBoost * 0.24h);
                color += textureSample.rgb * input.flutter * 0.12h;
                color += rim * _LightBoost * 0.08h;
                return half4(color, textureSample.a * _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
