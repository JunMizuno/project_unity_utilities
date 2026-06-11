Shader "Custom/TargetSideProjection"
{
    Properties
    {
        [MainTexture] _BaseMap("Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _ProjectionScale("Projection Scale", Float) = 1
        _ProjectionOffset("Projection Offset", Vector) = (0, 0, 0, 0)
        [Toggle] _UseAlphaClip("Hide Transparent Area", Float) = 1
        _AlphaClipThreshold("Alpha Clip Threshold", Range(0, 1)) = 0.1
        [Toggle] _UseTextureSlide("Use Texture Slide", Float) = 0
        _TextureSlideSpeed("Texture Slide Speed", Vector) = (0.2, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "AlphaTest"
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
                float2 projectedUV : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _ProjectionScale;
                float _UseAlphaClip;
                float _AlphaClipThreshold;
                float _UseTextureSlide;
                float4 _ProjectionOffset;
                float4 _TextureSlideSpeed;
            CBUFFER_END

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

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.projectedUV = GetProjectedUV(input.positionOS.xyz, normalize(input.normalOS));

                if (_UseTextureSlide > 0.5)
                {
                    output.projectedUV += _TextureSlideSpeed.xy * _Time.y;
                }

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.projectedUV) * _BaseColor;

                if (_UseAlphaClip > 0.5)
                {
                    clip(color.a - _AlphaClipThreshold);
                }

                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
