Shader "RenderFeatures/URPAnaglyph3D" {
    Properties {
		_MainTex ("Main Texture", 2D) = "" {}
	}
	
    HLSLINCLUDE
   
    #include "UnityCG.cginc"
    #include "Anaglyph3DFunctions.hlsl"

    UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
	UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

    uniform half4 _CameraClip;
    uniform half4 _SeparationDistance;

	uniform half4 _MainTex_TexelSize;

    struct v2f {
		float4 vertex : SV_POSITION;
		float2 uv : TEXCOORD0;
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv1 : TEXCOORD1;
		#endif
		UNITY_VERTEX_OUTPUT_STEREO
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};

    v2f vert(appdata_img v) {
		v2f o;

		UNITY_SETUP_INSTANCE_ID(v);
		UNITY_INITIALIZE_OUTPUT(v2f, o);
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.texcoord.xy;
		
		#if UNITY_UV_STARTS_AT_TOP
		o.uv1 = v.texcoord.xy;
		if (_MainTex_TexelSize.y < 0) {
			o.uv1.y = 1 - o.uv1.y;
		}
		#endif				
		
		return o;
	}
    
    half4 frag(v2f i) : SV_Target {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

        #if UNITY_UV_STARTS_AT_TOP
		float depthSample = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv1.xy);
		#else
		float depthSample = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv.xy);		
		#endif

		depthSample = Linear01Depth(depthSample);

        float worldDepth = remap(0.0, 1.0, _CameraClip.x, _CameraClip.y, depthSample);
        worldDepth = clamp(worldDepth, _SeparationDistance.z, _SeparationDistance.w);
        depthSample = remap(_CameraClip.x, _CameraClip.y, 0.0, 1.0, worldDepth);

        float2 leftUV = i.uv + _SeparationDistance.xy * depthSample;
        float2 rightUV = i.uv + _SeparationDistance.xy * -depthSample;

        half4 grayLeft = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, leftUV) * half4(0.3, 0.6, 0.1, 0);
        half4 grayRight = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, rightUV) * half4(0.3, 0.6, 0.1, 0);
        
        half left = grayLeft.r + grayLeft.g + grayLeft.b;
        half right = grayRight.r + grayRight.g + grayRight.b;

        return half4(left, right, right, 1);
    }

    ENDHLSL

    SubShader {
        Pass { //0
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
			#pragma target 3.0

            #pragma vertex vert
            #pragma fragment frag

            ENDHLSL
        }
    }
}