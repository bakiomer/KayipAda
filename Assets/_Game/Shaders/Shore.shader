// Shore.shader
// Kıyı şeridi (URP, mobil dostu, ışıksız, tek geçiş): kıyı çizgisi boyunca ince bir şerit ağı üstünde çizilir.
// - Kıyıya vuran dalga: ince su tabakası kuma doğru yavaşça ilerler, sonra geri çekilir (her yerde biraz farklı zamanda).
// - Dalganın ön kenarında yumuşak köpük çizgisi dalgayla birlikte hareket eder.
// - Su çekilince kumda ıslak iz kalır ve yavaşça kurur.
// Şerit ağının UV'si: x = kıyı boyunca uzaklık (m), y = kıyıdan uzaklık (m; eksi = karaya doğru).
// Yoğunluklar GameConfig > "Deniz ve kıyı" (SeaSettings global değişkenleri ayarlar).
Shader "KayipAda/Shore"
{
    Properties
    {
        _WaterColor ("Su Tabakası Rengi (A = saydamlık)", Color) = (0.55, 0.9, 0.88, 0.55)
        _FoamColor ("Köpük Rengi", Color) = (1, 1, 1, 0.95)
        _WetColor ("Islak Kum Rengi (A = en fazla koyuluk)", Color) = (0.42, 0.3, 0.16, 0.55)
        _Reach ("Dalganın Kumda En Fazla İlerlemesi (m)", Float) = 2.0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-1" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Shore"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _WaterColor;
                half4 _FoamColor;
                half4 _WetColor;
                float _Reach;
            CBUFFER_END

            // GameConfig'ten (SeaSettings).
            float _KA_ShoreSpeed;
            float _KA_ShoreReach;
            float _KA_ShoreFoam;
            float _KA_WetSand;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(ws);
                o.uv = input.uv;
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            // Kıyı boyunca yumuşak değişen değer (dalgalar her yerde aynı anda gelmesin).
            float Wobble(float x)
            {
                return sin(x * 0.21) * 0.5 + sin(x * 0.53 + 1.7) * 0.3 + sin(x * 1.13 + 4.1) * 0.2;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float along = i.uv.x;
                float s = -i.uv.y;                         // karaya doğru uzaklık (m); eksi = deniz tarafı
                float reach = _Reach * max(0.05, _KA_ShoreReach / 0.35) * (0.8 + 0.2 * Wobble(along * 0.7 + 3.0));
                float phase = frac(_Time.y * max(0.001, _KA_ShoreSpeed) + Wobble(along) * 0.35 + 0.5);

                // Ön kenar: 0..0.42 yavaşlayarak ilerler, 0.42..1 geri çekilir (biraz denize kadar).
                const float rise = 0.42;
                float front;
                if (phase < rise) front = lerp(-0.4, reach, sin(phase / rise * 1.5708));
                else front = lerp(reach, -0.4, smoothstep(0.0, 1.0, (phase - rise) / (1.0 - rise)));

                // Su tabakası: ön kenarın gerisinde, denize yaklaştıkça kalınlaşır.
                float covered = 1.0 - smoothstep(front - 0.12, front + 0.02, s);
                float sheet = covered * saturate(0.35 + 0.65 * (1.0 - saturate(s / max(0.1, reach)))) * _WaterColor.a;

                // Köpük: ön kenar çizgisi (ilerlerken belirgin, çekilirken incelir) + şeridin denize yakın tarafı.
                float edge = exp(-abs(s - front) / 0.09) * (phase < rise ? 1.0 : 0.55);
                float foam = saturate(edge * covered * 1.4 + edge * 0.35) * _KA_ShoreFoam * _FoamColor.a;

                // Islak iz: dalga çekildikten sonra kumda koyu kalır, yavaşça kurur. Dalganın hiç ulaşmadığı yer kuru.
                float wet = 0.0;
                if (s > 0.0 && s < reach)
                {
                    // Bu noktadan dalganın geri çekildiği an (geri çekilme evresinde).
                    float k = saturate((reach - s) / (reach + 0.4));
                    float leaveAt = rise + (1.0 - rise) * k;
                    float since = phase >= leaveAt ? phase - leaveAt : phase + 1.0 - leaveAt;
                    wet = exp(-since * 3.2) * (1.0 - covered);
                    wet *= smoothstep(reach, reach * 0.85, s);
                }
                wet *= _KA_WetSand;

                half3 col = _WetColor.rgb;
                float a = wet * _WetColor.a;
                col = lerp(col, _WaterColor.rgb, saturate(sheet / max(0.001, sheet + a)));
                a = saturate(a + sheet);
                col = lerp(col, _FoamColor.rgb, saturate(foam));
                a = saturate(max(a, foam));
                // Şeridin uçları (iç kenar) yumuşak.
                a *= smoothstep(reach + 0.6, reach + 0.1, s) * smoothstep(-0.9, -0.5, s);
                col = MixFog(col, i.fogFactor);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
