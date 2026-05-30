Shader "ShaderUtilities/WaveEquationWater"
{
    Properties
    {
        _BaseMap ("Water Texture", 2D) = "white" {}
        _DeepColor ("Deep Color", Color) = (0.02, 0.22, 0.34, 1)
        _ShallowColor ("Shallow Color", Color) = (0.20, 0.72, 0.86, 1)
        _FoamColor ("Foam Color", Color) = (0.85, 0.98, 1.0, 1)
        _Amplitude ("Amplitude", Range(0, 1)) = 0.14
        _WaveSpeed ("Wave Speed", Range(0, 6)) = 2.4
        _WaveScale ("Wave Scale", Range(0.1, 8)) = 0.8
        _FoamThreshold ("Foam Threshold", Range(0, 1)) = 0.92
        _TextureStrength ("Texture Strength", Range(0, 1)) = 0.08
        _GlossHighlight ("Gloss Highlight", Range(0, 2)) = 0.7
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

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _DeepColor;
                half4 _ShallowColor;
                half4 _FoamColor;
                half _Amplitude;
                half _WaveSpeed;
                half _WaveScale;
                half _FoamThreshold;
                half _TextureStrength;
                half _GlossHighlight;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half waveHeight : TEXCOORD3;
                half foam : TEXCOORD4;
            };

            half EvaluateWave(float2 position, float2 direction, half waveNumber, half amplitude, half speed, half phaseOffset)
            {
                direction = normalize(direction);
                half omega = speed * waveNumber;
                half phase = waveNumber * dot(direction, position) - omega * _Time.y + phaseOffset;
                return amplitude * sin(phase);
            }

            half EvaluateWaveSet(float2 position)
            {
                half scale = max(_WaveScale, 0.001h);

                // Each sine component satisfies u_tt = c^2 * Laplacian(u);
                // the sum remains a traveling-wave solution for the same wave speed.
                half h = 0;
                h += EvaluateWave(position, float2(1.0, 0.20), 0.78h * scale, 0.58h, _WaveSpeed, 0.0h);
                h += EvaluateWave(position, float2(-0.25, 1.0), 1.08h * scale, 0.28h, _WaveSpeed, 1.7h);
                h += EvaluateWave(position, float2(0.72, 0.58), 1.42h * scale, 0.14h, _WaveSpeed, 3.1h);
                return h * _Amplitude;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 positionOS = input.positionOS.xyz;
                half height = EvaluateWaveSet(positionOS.xz);
                positionOS.y += height;

                const float normalStep = 0.08;
                half heightX = EvaluateWaveSet(positionOS.xz + float2(normalStep, 0));
                half heightZ = EvaluateWaveSet(positionOS.xz + float2(0, normalStep));
                half3 tangentX = normalize(half3(normalStep, heightX - height, 0));
                half3 tangentZ = normalize(half3(0, heightZ - height, normalStep));
                half3 normalOS = normalize(cross(tangentZ, tangentX));

                VertexPositionInputs positionInputs = GetVertexPositionInputs(positionOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = TransformObjectToWorldNormal(normalOS);
                output.waveHeight = saturate(height / max(_Amplitude, 0.001h) * 0.5h + 0.5h);
                output.foam = smoothstep(_FoamThreshold, 1.0h, output.waveHeight);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                half ndotl = saturate(dot(normalWS, mainLight.direction));

                half4 textureSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv * 0.45h + _Time.yy * half2(0.006h, 0.004h));
                half softHeight = smoothstep(0.15h, 0.95h, input.waveHeight);
                half3 waterColor = lerp(_DeepColor.rgb, _ShallowColor.rgb, softHeight);
                waterColor = lerp(waterColor, waterColor * lerp(0.82h, 1.08h, textureSample.b), _TextureStrength);
                waterColor = lerp(waterColor, _FoamColor.rgb, input.foam * 0.18h);

                half fresnel = pow(1.0h - saturate(dot(normalWS, normalize(GetWorldSpaceViewDir(input.positionWS)))), 4.0h);
                half highlight = pow(saturate(dot(normalWS, normalize(mainLight.direction + normalize(GetWorldSpaceViewDir(input.positionWS))))), 96.0h);

                half3 lit = waterColor * (0.48h + ndotl * 0.54h);
                lit += _FoamColor.rgb * (fresnel * 0.12h + highlight * _GlossHighlight);
                return half4(lit, 1);
            }
            ENDHLSL
        }
    }
}
