Shader "WalldoffStudios/Outlines"
{
    Properties
    {
	    _OutlineColor ("Outline color", color) = (0.0, 0.0, 0.0, 1.0)
        _OutlineScale ("Outline scale", range(0.001, 1.0)) = 0.01
    }
	
    SubShader
    {
	    pass
		{

			Tags
			{
				"RenderType" = "Opaque"
				"Queue" = "Geometry+1"
			}
			
			Stencil
			{
				Ref 1
				Comp NotEqual
				Pass Keep
			}			
			
			Cull front

			CGPROGRAM

			#pragma vertex vert 
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct MeshData
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct vertexOutput
			{
				float4 pos : SV_POSITION;
				float3 normal : NORMAL;
			};
			
			float4 _OutlineColor;
			float _OutlineScale;

			vertexOutput vert(MeshData v)
			{
				vertexOutput o;
				o.normal = v.normal;
				o.pos = UnityObjectToClipPos(float4(v.vertex.xyz + normalize(v.normal) * _OutlineScale, v.vertex.w));
				return o;
			}

			half4 frag(vertexOutput i) : SV_Target
			{
				return _OutlineColor;
			}
			
			ENDCG
		}
    }
}
