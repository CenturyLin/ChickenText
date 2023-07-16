Shader"SL/UICustomText"
{
	Properties
	{
		[PerRendererData] _MainTex ("Font Texture", 2D) = "white" {}
		[HDR]_Color ("Tint", Color) = (1,1,1,1)
		
		[Enum(UnityEngine.Rendering.CompareFunction)]_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		[Enum(UnityEngine.Rendering.StencilOp)]_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255

		_ColorMask ("Color Mask", Float) = 15

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

		[Space(20)]
		[NoScaleOffset]_TextColTex("文字颜色贴图(TextColTex)", 2D) = "white" {}
		_ColTexUVFlow("uv流动速度(ColTexUVFlow)", Vector) = (1, 1, 1, 1)
		[Toggle]_BlockVertexColor("屏蔽顶点色(BlockVertexColor)", Int) = 0

	}

	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}
		
		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp] 
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		Pass
		{
			CGPROGRAM

			#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
			#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
			#endif
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			#pragma multi_compile __ UNITY_UI_CLIP_RECT
			#pragma multi_compile __ UNITY_UI_ALPHACLIP
			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
				float2 texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				half2 texcoord  : TEXCOORD0;
				float2 texcoord1 : TEXCOORD1;
				float4 worldPosition : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			uniform fixed4 _Color;
			uniform fixed4 _TextureSampleAdd;
			uniform float4 _ClipRect;
			uniform sampler2D _MainTex;

			uniform sampler2D _TextColTex;
			float2 _ColTexUVFlow;
			uint _BlockVertexColor;
			
			v2f vert( appdata_t IN )
			{
				v2f OUT;
				UNITY_SETUP_INSTANCE_ID( IN );
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
				OUT.worldPosition = IN.vertex;
				
				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

				OUT.texcoord = IN.texcoord;
				OUT.texcoord1 = IN.texcoord1;

				OUT.color = lerp(IN.color, 1, _BlockVertexColor) * _Color;
				return OUT;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
				
				#ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

				#ifdef UNITY_UI_ALPHACLIP
				clip (color.a - 0.001);
				#endif
				
				color.rgb *= tex2D(_TextColTex, IN.texcoord1 + _ColTexUVFlow * _Time.x);

				return color;
			}
		ENDCG
		}
	}
}
