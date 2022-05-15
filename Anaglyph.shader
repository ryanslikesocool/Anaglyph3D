// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

Shader "RenderFeature/Anaglyph" {
    Properties {
        [HideInInspector] _MainTex ("Texture", 2D) = "clear" {}
    }
    SubShader {
        Pass {
            Cull Back
            ZWrite Off
            ZTest Off
            Blend Off

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
            SAMPLER(sampler_LeftTex);
            SAMPLER(sampler_RightTex);

            float4 frag (Varyings IN) : SV_Target {
                float4 colorMain = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.texcoord);
                float4 colorL = SAMPLE_TEXTURE2D(_LeftTex, sampler_LeftTex, IN.texcoord);
                float4 colorR = SAMPLE_TEXTURE2D(_RightTex, sampler_RightTex, IN.texcoord);

                float lumL = Luminance(colorL.rgb);
                float lumR = Luminance(colorR.rgb);
                float3 anaglyph = float3(lumL, lumR, lumR);

                float opacity = max(colorL.a, colorR.a);

                float4 output;
                output.rgb = lerp(colorMain.rgb, anaglyph, opacity);
                output.a = 1;

                return output;
            }

            ENDHLSL
        }
    }

    Fallback off
}