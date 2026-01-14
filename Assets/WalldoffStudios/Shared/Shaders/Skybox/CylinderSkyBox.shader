Shader "WalldoffStudios/CylinderSkyBox"
{
    Properties
    {
        _BackgroundColor ("Background Color", Color) = (0.8, 0.8, 0.8, 1)
        _GridColor ("Grid Color", Color) = (0.0, 0.0, 0.0, 1)
        _GridSize ("Grid Size", Float) = 10.0
        _LineThickness ("Line Thickness", Float) = 0.05
    }
    SubShader
    {
        Tags { "Queue"="Background" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            fixed4 _BackgroundColor;
            fixed4 _GridColor;
            float _GridSize;
            float _LineThickness;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);
                
                float angle = atan2(dir.x, dir.z);
                float height = dir.y;
                
                float2 uv = float2(angle / UNITY_PI, height);
                uv = uv * 0.5 + 0.5;
                uv.x *= _GridSize;
                uv.y *= _GridSize;
                
                float2 grid = abs(frac(uv) - 0.5) / fwidth(uv);
                float lines = min(grid.x, grid.y);
                
                half4 color = lerp(_BackgroundColor, _GridColor, step(lines, _LineThickness));

                return color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}

