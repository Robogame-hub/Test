Shader "UCLA Game Lab/Wireframe/Single-Sided Cutout" 
{
	Properties 
	{
		_Color ("Line Color", Color) = (1,1,1,1)
		_MainTex ("Main Texture", 2D) = "white" {}
		_Thickness ("Thickness", Float) = 1
	}

	SubShader
	{
		Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			LOD 100

			CGPROGRAM
				#pragma only_renderers gles gles3
				#pragma vertex vertWeb
				#pragma fragment fragWeb
				#include "UnityCG.cginc"

				sampler2D _MainTex;
				float4 _MainTex_ST;
				float4 _Color;

				struct appdata
				{
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct v2f
				{
					float4 pos : SV_POSITION;
					float2 uv : TEXCOORD0;
				};

				v2f vertWeb(appdata v)
				{
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.uv = TRANSFORM_TEX(v.uv, _MainTex);
					return o;
				}

				fixed4 fragWeb(v2f i) : SV_Target
				{
					fixed4 col = tex2D(_MainTex, i.uv) * _Color;
					clip(col.a - 0.5);
					col.a = 1.0;
					return col;
				}
			ENDCG
		}
	}

	SubShader 
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }

		Pass
		{


			Blend SrcAlpha OneMinusSrcAlpha 
			LOD 200
			
			CGPROGRAM
				#pragma target 5.0
				#include "UnityCG.cginc"
				#include "UCLA GameLab Wireframe Functions.cginc"
				#pragma vertex vert
				#pragma fragment frag
				#pragma geometry geom

				// Vertex Shader
				UCLAGL_v2g vert(appdata_base v)
				{
					return UCLAGL_vert(v);
				}
				
				// Geometry Shader
				[maxvertexcount(3)]
				void geom(triangle UCLAGL_v2g p[3], inout TriangleStream<UCLAGL_g2f> triStream)
				{
					UCLAGL_geom( p, triStream);
				}
				
				// Fragment Shader
				float4 frag(UCLAGL_g2f input) : COLOR
				{	
					float4 col = UCLAGL_frag(input);
					if( col.a < 0.5f ) discard;
					else col.a = 1.0f;
					
					return col;
				}
			
			ENDCG
		}
	} 
}
