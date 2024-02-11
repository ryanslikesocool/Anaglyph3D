// Developed With Love by Ryan Boyer https://ryanjboyer.com <3

Shader "Hidden/RenderFeature/Anaglyph/Main" {
    Properties { }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
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
        Cull Back
		Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            Name "Anaglyph"

            HLSLPROGRAM
            #pragma target 2.0

            #pragma multi_compile_fragment _ _ANAGLYPH_SINGLE_CHANNEL

            #pragma vertex vert
            #pragma fragment frag

			uniform TEXTURE2D_X(_AnaglyphLeft);
			uniform TEXTURE2D_X(_AnaglyphLeftDepth);
			uniform SAMPLER(sampler_AnaglyphLeft);

#ifndef _ANAGLYPH_SINGLE_CHANNEL
			uniform TEXTURE2D_X(_AnaglyphRight);
			uniform TEXTURE2D_X(_AnaglyphRightDepth);
			uniform SAMPLER(sampler_AnaglyphRight);
#endif

#if SHADER_API_GLES
			struct Attributes {
				UNITY_VERTEX_INPUT_INSTANCE_ID
				float4 positionOS : POSITION;
				float2 texcoord : TEXCOORD0;
			};
#else
			struct Attributes {
				UNITY_VERTEX_INPUT_INSTANCE_ID
				uint vertexID : SV_VertexID;
			};
#endif

			struct Varyings {
				UNITY_VERTEX_OUTPUT_STEREO
				float4 positionCS : SV_POSITION;
				float2 texcoord : TEXCOORD0;
			};

			Varyings vert(Attributes IN) {
				Varyings OUT = (Varyings)0;

				UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

#if SHADER_API_GLES
				OUT.positionCS = IN.positionOS;
				OUT.texcoord  = IN.texcoord;
#else
				OUT.positionCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
				OUT.texcoord  = GetFullScreenTriangleTexCoord(IN.vertexID);
#endif

				return OUT;
			}

            half4 frag (Varyings IN, out float depth : SV_Depth) : SV_Target {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                const float sceneDepth = SampleSceneDepth(IN.texcoord);

                const half4 colorL = SAMPLE_TEXTURE2D_X(_AnaglyphLeft, sampler_AnaglyphLeft, IN.texcoord);
				const float depthL = SAMPLE_TEXTURE2D_X(_AnaglyphLeftDepth, sampler_AnaglyphLeft, IN.texcoord).r;

#ifdef _ANAGLYPH_SINGLE_CHANNEL
				const half4 anaglyphColor = colorL;
				const float anaglyphDepth = depthL;
#else // multi-channel
				const half4 colorR = SAMPLE_TEXTURE2D_X(_AnaglyphRight, sampler_AnaglyphRight, IN.texcoord);
				const float depthR = SAMPLE_TEXTURE2D_X(_AnaglyphRightDepth, sampler_AnaglyphRight, IN.texcoord).r;

				const half4 anaglyphColor = half4(colorL.x, colorR.y, colorR.z, (colorL.a + colorR.a) * 0.5);

				const float anaglyphDepth = max(depthL, depthR);
#endif

                depth = max(anaglyphDepth, sceneDepth);
				half4 color = (half4)0;

				if (anaglyphDepth > sceneDepth) {
					color = anaglyphColor;
				}

                return color;
            }
            ENDHLSL
        }
    }

    Fallback off
}