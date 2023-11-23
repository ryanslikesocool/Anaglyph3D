// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

Shader "Render Feature/Anaglyph" {
    Properties { }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
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

        ZWrite On
		ZTest Always
        Cull Off

        Pass {
            Name "Anaglyph"

            HLSLPROGRAM
            #pragma target 2.0

            #pragma multi_compile_fragment _ _ANAGLYPH_SINGLE_CHANNEL

            #pragma vertex Vert // defined in SRP Core > Runtime > Utilities > Blit.hlsl
            #pragma fragment frag

			uniform TEXTURE2D(_AnaglyphLeft);
			uniform TEXTURE2D(_AnaglyphLeftDepth);
			uniform SAMPLER(sampler_AnaglyphLeft);

#ifndef _ANAGLYPH_SINGLE_CHANNEL
			uniform TEXTURE2D(_AnaglyphRight);
			uniform TEXTURE2D(_AnaglyphRightDepth);
			uniform SAMPLER(sampler_AnaglyphRight);
#endif

			// TEXTURE2D(_BlitTexture); // defined in RP Core > Blit.hlsl
			uniform SAMPLER(sampler_BlitTexture);

            half4 frag (Varyings IN, out float depth : SV_Depth) : SV_Target {
				const half4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, IN.texcoord);
                const float sceneDepth = SampleSceneDepth(IN.texcoord);

                const half4 colorL = SAMPLE_TEXTURE2D(_AnaglyphLeft, sampler_AnaglyphLeft, IN.texcoord);
				const float depthL = SAMPLE_TEXTURE2D(_AnaglyphLeftDepth, sampler_AnaglyphLeft, IN.texcoord).r;

#ifdef _ANAGLYPH_SINGLE_CHANNEL
				const half3 anaglyphColor = colorL.xyz;
				const float anaglyphDepth = depthL;
#else // multi-channel
				const half4 colorR = SAMPLE_TEXTURE2D(_AnaglyphRight, sampler_AnaglyphRight, IN.texcoord);
				const float depthR = SAMPLE_TEXTURE2D(_AnaglyphRightDepth, sampler_AnaglyphRight, IN.texcoord).r;

				const half3 anaglyphColor = half3(colorL.x, colorR.y, colorR.z);

				const float anaglyphDepth = max(depthL, depthR);
#endif

                depth = max(anaglyphDepth, sceneDepth);
				half4 color = (half4)0;

				if (anaglyphDepth > sceneDepth) {
					color.rgb = anaglyphColor;
					color.a = 1.0;
				} else {
					color = sceneColor;
				}

                return color;
            }
            ENDHLSL
        }
    }

    Fallback off
}