// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

Shader "RenderFeature/Anaglyph" {
    Properties {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "clear" {}
    }
    SubShader {
        Tags {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Fullscreen"
        }

        ZWrite Off
        Cull Off
        LOD 100

        Pass {
            HLSLPROGRAM
            #pragma target 2.5

            #pragma shader_feature_fragment _ _OPACITY_MODE_ADDITIVE _OPACITY_MODE_CHANNEL
            #pragma shader_feature_fragment _ _SINGLE_CHANNEL
            #pragma shader_feature_fragment _ _OVERLAY_EFFECT

            #pragma vertex VertNoScaleBias
            #pragma fragment frag

		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            Varyings VertNoScaleBias(Attributes input) {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            #if SHADER_API_GLES
                float4 pos = input.positionOS;
                float2 uv  = input.uv;
            #else
                float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
                float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);
            #endif

                output.positionCS = pos;
                output.texcoord = uv; // * _BlitScaleBias.xy + _BlitScaleBias.zw;
                return output;
            }

            #if _OVERLAY_EFFECT
                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);
            #endif

            TEXTURE2D(_LeftTex);
            SAMPLER(sampler_LeftTex);

            #if !_SINGLECHANNEL
                TEXTURE2D(_RightTex);
                SAMPLER(sampler_RightTex);
            #endif

            half4 frag (Varyings IN) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                #if _OVERLAY_EFFECT
                    half4 colorMain = SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, IN.texcoord);
                    // colorMain.rgb = half3(0.663, 0.663, 0.5);
                #endif

                half4 output;

                half4 colorL = SAMPLE_TEXTURE2D_X(_LeftTex, sampler_LeftTex, IN.texcoord);
                half lumL = Luminance(colorL.xyz);
                half alphaL = colorL.a;
                output.a = colorL.a;

                #if _SINGLE_CHANNEL
                    half opacity = alphaL;
                    half anaglyph = lumL;
                #else
                    half4 colorR = SAMPLE_TEXTURE2D_X(_RightTex, sampler_RightTex, IN.texcoord);
                    half alphaR = colorR.a;
                    output.a = max(output.a, colorR.a);

                    #if _OPACITY_MODE_ADDITIVE
                        half3 opacity = (alphaL + alphaR) * half(0.5);
                    #elif _OPACITY_MODE_CHANNEL
                        half3 opacity = half3(alphaL, alphaR, alphaR);
                    #else
                        half3 opacity = max(alphaL, alphaR);
                    #endif

                    half lumR = Luminance(colorR.xyz);
                    half3 anaglyph = half3(lumL, lumR, lumR);
                #endif

                #if _OVERLAY_EFFECT
                    output.rgb = lerp(colorMain.rgb, anaglyph, opacity);
                    output.a = max(output.a, colorMain.a);

                    output = colorMain;
                #else
                    output.rgb = anaglyph;

                    output = colorL;
                #endif

                return output;
            }
            ENDHLSL
        }
    }

    Fallback off
}