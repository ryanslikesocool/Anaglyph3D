// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

Shader "RenderFeature/Anaglyph" {
    Properties {
        [HideInInspector] _MainTex ("Texture", 2D) = "clear" {}
    }
    SubShader {
        Pass {
            Cull Off
            ZWrite Off
            ZTest Off
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            Varyings vert (Attributes IN) {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.texcoord = IN.texcoord;
                return OUT;
            }

            TEXTURE2D(_MainTex);
            TEXTURE2D(_LeftTex);
            TEXTURE2D(_RightTex);

            SAMPLER(sampler_MainTex);

            half4 frag (Varyings IN) : SV_Target {
                half4 colorMain = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.texcoord);
                half4 colorL = SAMPLE_TEXTURE2D(_LeftTex, sampler_MainTex, IN.texcoord);
                half4 colorR = SAMPLE_TEXTURE2D(_RightTex, sampler_MainTex, IN.texcoord);

                half lumL = Luminance(colorL.xyz);
                half lumR = Luminance(colorR.xyz);

                half3 anaglyph = half3(lumL, lumR, lumR);

                //float opacity = (colorL.a + colorR.a) * 0.5f;
                half opacity = max(colorL.a, colorR.a);

                //float3 anaglyph = float3(colorL.r, colorR.gb);
                //anaglyph = step(0.001, anaglyph);
                //float maxAlpha = max(colorL.a, colorR.a);

                half4 output;
                output.rgb = lerp(colorMain.rgb, anaglyph, opacity);
                output.a = 1;

                return output;
            }
            ENDHLSL
        }
    }

    Fallback off
}