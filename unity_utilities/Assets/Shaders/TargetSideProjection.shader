Shader "Custom/TargetSideProjection"
{
    Properties
    {
        [MainTexture] _BaseMap("Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _ProjectionScale("Projection Scale", Float) = 1
        _ProjectionOffset("Projection Offset", Vector) = (0, 0, 0, 0)
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
            Name "ForwardUnlit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalOS : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _ProjectionScale;
                float4 _ProjectionOffset;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionOS = input.positionOS.xyz;
                output.normalOS = normalize(input.normalOS);
                return output;
            }

            float2 GetProjectedUV(float3 positionOS, float3 normalOS)
            {
                float3 absNormal = abs(normalOS);
                float2 uv;

                if (absNormal.x >= absNormal.y && absNormal.x >= absNormal.z)
                {
                    uv = normalOS.x > 0.0 ? float2(-positionOS.z, positionOS.y) : float2(positionOS.z, positionOS.y);
                }
                else if (absNormal.y >= absNormal.x && absNormal.y >= absNormal.z)
                {
                    uv = normalOS.y > 0.0 ? float2(positionOS.x, -positionOS.z) : float2(positionOS.x, positionOS.z);
                }
                else
                {
                    uv = normalOS.z > 0.0 ? float2(positionOS.x, positionOS.y) : float2(-positionOS.x, positionOS.y);
                }

                uv = uv * max(_ProjectionScale, 0.0001) + 0.5 + _ProjectionOffset.xy;
                return uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = GetProjectedUV(input.positionOS, normalize(input.normalOS));
                half4 textureColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                return textureColor * _BaseColor;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
