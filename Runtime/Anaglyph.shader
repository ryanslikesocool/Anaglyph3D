// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

Shader "Render Feature/Anaglyph" {
    Properties { }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    ENDHLSL

    SubShader {
        Tags {
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "SimpleLit"
            "LightMode" = "SRPDefaultUnlit"
			"RenderType" = "Overlay"
			"Queue" = "Overlay"
            "ForceNoShadowCasting" = "True"
            "IgnoreProjector" = "True"
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
            #pragma multi_compile_fragment _ _COLOR_MODE_COLOR

            #pragma vertex Vert // defined in SRP Core > Runtime > Utilities > Blit.hlsl
            #pragma fragment frag

            CBUFFER_START(UnityPerMaterial)
                uniform TEXTURE2D(_AnaglyphLeft);
                uniform SAMPLER(sampler_AnaglyphLeft);

#ifndef _SINGLECHANNEL
                    uniform TEXTURE2D(_AnaglyphRight);
                    uniform SAMPLER(sampler_AnaglyphRight);
#endif

#if defined(_OVERLAY_MODE_OPACITY) || defined(_OVERLAY_MODE_DEPTH)
                    // TEXTURE2D(_BlitTexture); // defined in RP Core > Blit.hlsl
                    uniform SAMPLER(sampler_BlitTexture);
#endif

#ifdef _OVERLAY_MODE_DEPTH
                    uniform TEXTURE2D(_AnaglyphLeftDepth);
	#ifndef _SINGLE_CHANNEL
                        uniform TEXTURE2D(_AnaglyphRightDepth);
	#endif
#endif
            CBUFFER_END

            half4 frag (Varyings IN) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

#if defined(_OVERLAY_MODE_OPACITY) || defined(_OVERLAY_MODE_DEPTH)
				const half4 colorMain = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, IN.texcoord);
#endif
#ifdef _OVERLAY_MODE_DEPTH
				const half stepThreshold = half(0.0001);
#endif

                half4 output = half(0.0);

                const half4 colorL = SAMPLE_TEXTURE2D(_AnaglyphLeft, sampler_AnaglyphLeft, IN.texcoord);
#ifndef _COLOR_MODE_COLOR
				const half lumL = Luminance(colorL.xyz);
#endif
#ifdef _OVERLAY_MODE_DEPTH
				const half depthL = SAMPLE_TEXTURE2D(_AnaglyphLeftDepth, sampler_AnaglyphLeft, IN.texcoord).r;
				const half leftA = step(depthL, stepThreshold);
#else
				const half leftA = colorL.a;
#endif
                output.a = leftA;

#ifdef _SINGLE_CHANNEL
				half opacity = leftA;
	#ifdef _COLOR_MODE_COLOR
				const half3 anaglyph = colorL.xyz;
	#else
				const half3 anaglyph = lumL;
	#endif
#else
				const half4 colorR = SAMPLE_TEXTURE2D(_AnaglyphRight, sampler_AnaglyphRight, IN.texcoord);
	#ifndef _COLOR_MODE_COLOR
				const half lumR = Luminance(colorR.xyz);
	#endif
	#if _OVERLAY_MODE_DEPTH
				const half depthR = SAMPLE_TEXTURE2D(_AnaglyphRightDepth, sampler_AnaglyphRight, IN.texcoord).r;
				const half rightA = step(depthR, stepThreshold);
	#else
				const half rightA = colorR.a;
	#endif

				output.a = max(output.a, rightA);

	#if defined(_BLEND_MODE_ADDITIVE)
				half3 opacity = (leftA + rightA) * half(0.5);
	#elif defined(_BLEND_MODE_CHANNEL)
				half3 opacity = half3(leftA, rightA, rightA);
	#else
				half3 opacity = max(leftA, rightA);
	#endif

	#ifdef _COLOR_MODE_COLOR
				const half3 anaglyph = half3(colorL.x, colorR.y, colorR.z);
	#else
				const half3 anaglyph = half3(lumL, lumR, lumR);
	#endif
#endif

#if defined(_OVERLAY_MODE_DEPTH) && defined(UNITY_REVERSED_Z)
				opacity = half(1.0) - opacity;
#endif

#if defined(_OVERLAY_MODE_OPACITY) || defined(_OVERLAY_MODE_DEPTH)
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