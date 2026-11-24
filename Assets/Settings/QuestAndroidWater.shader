Shader "PortableSilentRoom/Quest Water"
{
    Properties
    {
        _BaseColor("Water Color", Color) = (0.15, 0.49, 0.47, 1)
        _HighlightColor("Soft Highlight", Color) = (0.48, 0.72, 0.68, 1)
        _RippleTiling("Ripple Tiling", Float) = 36
        _RippleSpeed("Ripple Speed", Float) = 0.28
        _Variation("Surface Variation", Range(0, 0.2)) = 0.07
        _HighlightStrength("Highlight Strength", Range(0, 0.25)) = 0.16
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
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _HighlightColor;
                float _RippleTiling;
                float _RippleSpeed;
                float _Variation;
                float _HighlightStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float phase = (input.uv.x * 0.91 + input.uv.y * 0.42) *
                    (_RippleTiling * 6.2831853) + _Time.y * _RippleSpeed;
                half surface = sin(phase);
                half highlight = smoothstep(0.7, 0.97, surface) * _HighlightStrength;
                half3 water = _BaseColor.rgb * (1.0h + surface * _Variation);
                water = lerp(water, _HighlightColor.rgb, highlight);
                return half4(water, 1.0h);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
