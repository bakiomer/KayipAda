// BuildReveal.shader
// İnşa edilen gemi için ışık alan (URP, mat) shader: model aşağıdan yukarıya doğru belirir.
// _Reveal 0 → hiçbir şey görünmez, 1 → tamamı. Kesilen kenarda sıcak bir parlama şeridi (inşa ediliyor hissi).
// Gölge düşürür ve alır; tek ana ışık + ortam ışığı (telefonda ucuz).
Shader "KayipAda/BuildReveal"
{
    Properties
    {
        _BaseMap ("Doku", 2D) = "white" {}
        _BaseColor ("Renk", Color) = (1, 1, 1, 1)
        _Reveal ("Görünen Oran", Range(0, 1)) = 1
        _MinY ("Alt Yükseklik (dünya)", Float) = 0
        _MaxY ("Üst Yükseklik (dünya)", Float) = 5
        _GlowColor ("Kenar Parlaması", Color) = (1, 0.65, 0.2, 1)
        _GlowWidth ("Parlama Genişliği", Float) = 0.25
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    TEXTURE2D(_BaseMap);
    SAMPLER(sampler_BaseMap);

    CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        half4 _BaseColor;
        float _Reveal;
        float _MinY;
        float _MaxY;
        half4 _GlowColor;
        float _GlowWidth;
    CBUFFER_END

    float CutHeight()
    {
        return lerp(_MinY - 0.01, _MaxY + 0.01, _Reveal);
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float cut = CutHeight();
                clip(cut - i.positionWS.y);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                float3 n = normalize(i.normalWS);
                Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half ndl = saturate(dot(n, light.direction));
                half3 color = albedo.rgb * (light.color * ndl * light.shadowAttenuation + SampleSH(n));

                half glow = (1.0 - saturate((cut - i.positionWS.y) / max(_GlowWidth, 0.001))) * step(_Reveal, 0.999);
                color = lerp(color, _GlowColor.rgb, glow * 0.85);
                color = MixFog(color, i.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings o;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                o.positionCS = positionCS;
                o.positionWS = positionWS;
                return o;
            }

            half4 ShadowFrag(Varyings i) : SV_Target
            {
                clip(CutHeight() - i.positionWS.y);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            Varyings DepthVert(Attributes input)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                return o;
            }

            half4 DepthFrag(Varyings i) : SV_Target
            {
                clip(CutHeight() - i.positionWS.y);
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
