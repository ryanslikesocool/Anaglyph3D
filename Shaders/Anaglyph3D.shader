// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

Shader "RenderFeature/Anaglyph3D" {
    Properties {
        [HideInInspector] _ChannelSeparation ("Channel Separation", Vector) = (-0.0025, 0, 0, 0)
        [HideInInspector] _TintOpacity ("Tint Opacity", Float) = 0.05
    }
    SubShader {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass {
            HLSLPROGRAM
            #pragma target 2.0
            #pragma multi_compile_instancing

            #pragma vertex vert
            #pragma fragment frag

	        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                uniform float2 _ChannelSeparation;
                uniform float _TintOpacity;
            CBUFFER_END

            Varyings vert(Attributes IN) {
                Varyings OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.texcoord = UnityStereoTransformScreenSpaceTex(IN.texcoord);
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float depth = SampleSceneDepth(IN.texcoord);
                depth = 1 - Linear01Depth(depth, _ZBufferParams);
                depth *= depth;

                float2 uvL = IN.texcoord - (_ChannelSeparation * depth);
                float2 uvR = IN.texcoord + (_ChannelSeparation * depth);

                float3 lhs = SampleSceneColor(uvL);
                float3 rhs = SampleSceneColor(uvR);

                float l = lhs.x * 0.3 + lhs.y * 0.6 + lhs.z * 0.1;
                float r = rhs.x * 0.3 + rhs.y * 0.6 + rhs.z * 0.1;

                float3 col = float3(l, r, r);
                float3 tint = float3(uvL.x, uvR.y, uvR.y) * _TintOpacity;
                col += tint;

                return float4(col.r, col.g, col.b, 1);
            }
            ENDHLSL
        }
    }
}