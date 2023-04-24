// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

Shader "RenderFeature/Anaglyph" {
    Properties { }
    SubShader {
        Tags {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        ZWrite Off
        Cull Off

        Pass {
            Name "Anaglyph"

            HLSLPROGRAM
            #pragma target 2.0

            #pragma multi_compile_fragment _ _SINGLE_CHANNEL
            #pragma multi_compile_fragment _ _OVERLAY_MODE_OPACITY _OVERLAY_MODE_DEPTH
            #pragma multi_compile_fragment _ _BLEND_MODE_ADDITIVE _BLEND_MODE_CHANNEL

            #pragma vertex Vert // defined in RP Core > Blit.hlsl
            #pragma fragment frag

		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            uniform TEXTURE2D(_AnaglyphLeft);
            uniform SAMPLER(sampler_AnaglyphLeft);

            #if !_SINGLECHANNEL
                uniform TEXTURE2D(_AnaglyphRight);
                uniform SAMPLER(sampler_AnaglyphRight);
            #endif

            #if _OVERLAY_MODE_OPACITY || _OVERLAY_MODE_DEPTH
                // TEXTURE2D(_BlitTexture); // defined in RP Core > Blit.hlsl
                uniform SAMPLER(sampler_BlitTexture);
            #endif

            #if _OVERLAY_MODE_DEPTH
                uniform TEXTURE2D(_AnaglyphLeftDepth);
                #if !_SINGLE_CHANNEL
                    uniform TEXTURE2D(_AnaglyphRightDepth);
                #endif
            #endif

            half4 frag (Varyings IN) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                #if _OVERLAY_MODE_OPACITY || _OVERLAY_MODE_DEPTH
                    const half4 colorMain = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, IN.texcoord);
                #endif
                #if _OVERLAY_MODE_DEPTH
                    const half stepThreshold = half(0.0001);
                #endif

                half4 output = half(0.0);

                const half4 colorL = SAMPLE_TEXTURE2D_X(_AnaglyphLeft, sampler_AnaglyphLeft, IN.texcoord);
                const half lumL = Luminance(colorL.xyz);
                #if _OVERLAY_MODE_DEPTH
                    const half depthL = SAMPLE_TEXTURE2D_X(_AnaglyphLeftDepth, sampler_AnaglyphLeft, IN.texcoord).r;
                    const half leftA = step(depthL, stepThreshold);
                #else
                    const half leftA = colorL.a;
                #endif
                output.a = leftA;

                #if _SINGLE_CHANNEL
                    half opacity = leftA;
                    const half anaglyph = lumL;
                #else
                    const half4 colorR = SAMPLE_TEXTURE2D_X(_AnaglyphRight, sampler_AnaglyphRight, IN.texcoord);
                    const half lumR = Luminance(colorR.xyz);
                    #if _OVERLAY_MODE_DEPTH
                        const half depthR = SAMPLE_TEXTURE2D_X(_AnaglyphRightDepth, sampler_AnaglyphRight, IN.texcoord).r;
                        const half rightA = step(depthR, stepThreshold);
                    #else
                        const half rightA = colorR.a;
                    #endif

                    output.a = max(output.a, rightA);

                    #if _BLEND_MODE_ADDITIVE
                       half3 opacity = (leftA + rightA) * half(0.5);
                    #elif _BLEND_MODE_CHANNEL
                        half3 opacity = half3(leftA, rightA, rightA);
                    #else
                       half3 opacity = max(leftA, rightA);
                    #endif

                    const half3 anaglyph = half3(lumL, lumR, lumR);
                #endif

                #if _OVERLAY_MODE_DEPTH && UNITY_REVERSED_Z
                    opacity = half(1.0) - opacity;
                #endif

                #if _OVERLAY_MODE_OPACITY || _OVERLAY_MODE_DEPTH
                    output.rgb = lerp(colorMain.rgb, anaglyph, opacity);
                    output.a = max(output.a, colorMain.a);
                #else
                    output.rgb = anaglyph;
                #endif

                return output;
            }
            ENDHLSL
        }
    }

    Fallback off
}