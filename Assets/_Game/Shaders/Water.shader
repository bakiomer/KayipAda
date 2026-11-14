// Water.shader
// Adanın çevresindeki su (URP, mobil dostu, ışıksız ve tek geçişli).
// - Köşe dalgaları: yüzey yavaşça kabarıp iner.
// - Renk: kıyıya yaklaştıkça açılır, açıkta koyulaşır. Kıyıya uzaklık, sahne kurma komutunun ürettiği
//   küçük bir dokudan (_ShoreTex) okunur; derinlik dokusu gerekmez (telefonda ucuz).
// - Kıyıda nefes alan köpük şeridi ve dışa doğru açılan köpük halkası, açıkta küçük parıltılar.
Shader "KayipAda/Water"
{
    Properties
    {
        _ShallowColor ("Sığ Renk (A = saydamlık)", Color) = (0.36, 0.86, 0.84, 0.55)
        _DeepColor ("Derin Renk (A = saydamlık)", Color) = (0.06, 0.38, 0.62, 1)
        _FoamColor ("Köpük Rengi", Color) = (1, 1, 1, 0.9)
        _ShoreTex ("Kıyı Uzaklık Dokusu", 2D) = "white" {}
        _ShoreRect ("Doku Alanı (xMin, zMin, genişlik, derinlik)", Vector) = (-60, -60, 120, 120)
        _ShoreMaxDistance ("Doku Uzaklık Ölçeği (m)", Float) = 16
        _DepthRange ("Sığdan Derine Geçiş (m)", Float) = 9
        _FoamWidth ("Köpük Genişliği (m)", Float) = 1.1
        _WaveHeight ("Dalga Yüksekliği (m)", Float) = 0.08
        _WaveSpeed ("Dalga Hızı", Float) = 1
        _WaveScale ("Dalga Sıklığı", Float) = 0.35
        _Sparkle ("Parıltı", Float) = 0.12
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Water"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_ShoreTex);
            SAMPLER(sampler_ShoreTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _FoamColor;
                float4 _ShoreTex_ST;
                float4 _ShoreRect;
                float _ShoreMaxDistance;
                float _DepthRange;
                float _FoamWidth;
                float _WaveHeight;
                float _WaveSpeed;
                float _WaveScale;
                float _Sparkle;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float fogFactor : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(input.positionOS.xyz);
                float t = _Time.y * _WaveSpeed;
                ws.y += (sin(ws.x * _WaveScale + t) * cos(ws.z * _WaveScale * 0.8 + t * 0.8)
                       + 0.5 * sin((ws.x + ws.z) * _WaveScale * 1.7 + t * 1.3)) * _WaveHeight;
                o.positionWS = ws;
                o.positionCS = TransformWorldToHClip(ws);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float3 ws = i.positionWS;
                float t = _Time.y * _WaveSpeed;

                // Kıyıya uzaklık (metre): 0 = kıyı, büyüdükçe açık deniz.
                float2 uv = (ws.xz - _ShoreRect.xy) / _ShoreRect.zw;
                float d = SAMPLE_TEXTURE2D(_ShoreTex, sampler_ShoreTex, uv).r * _ShoreMaxDistance;

                float depthT = smoothstep(0.0, 1.0, saturate(d / _DepthRange));
                half4 col = lerp(_ShallowColor, _DeepColor, depthT);

                // Kıyı köpüğü: nefes alan şerit + dışa açılan halka.
                float n = sin(ws.x * 1.3 + t * 1.1) * 0.5 + sin(ws.z * 1.7 - t * 0.9) * 0.5;
                float band = _FoamWidth * (0.8 + 0.2 * sin(t * 1.4 + n));
                float foam = 1.0 - smoothstep(band * 0.55, band, d + n * 0.12);
                float cycle = frac(t * 0.22);
                float ringPos = band * 0.6 + cycle * _FoamWidth * 2.0;
                float ring = (1.0 - smoothstep(0.0, 0.2, abs(d - ringPos + n * 0.1))) * (1.0 - cycle) * 0.7;
                foam = saturate(foam + ring);

                // Açık denizde küçük parıltılar.
                float sparkle = pow(saturate(sin(ws.x * 3.1 + t * 2.0) * sin(ws.z * 2.7 - t * 1.7)), 20.0) * _Sparkle * depthT;

                col.rgb = lerp(col.rgb, _FoamColor.rgb, foam * _FoamColor.a) + sparkle;
                col.a = max(col.a, foam * _FoamColor.a);
                col.rgb = MixFog(col.rgb, i.fogFactor);
                return col;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
