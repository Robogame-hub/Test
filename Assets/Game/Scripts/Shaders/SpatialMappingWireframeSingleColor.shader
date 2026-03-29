Shader "Universal Render Pipeline/VR/SpatialMapping/Wireframe Single Color"
{
    Properties
    {
        _WireThickness ("Wire Thickness", Range(0.5, 6.0)) = 1.5
        _WireColor ("Wire Color", Color) = (0.0, 1.0, 1.0, 1.0)
        _FillColor ("Fill Color", Color) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "Spatial Mapping Color Fallback"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 barycentric : TEXCOORD3;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 barycentric : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _WireColor;
                half4 _FillColor;
                float _WireThickness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vpi = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vpi.positionCS;
                output.barycentric = input.barycentric;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 bary = saturate(input.barycentric);
                float3 fw = max(fwidth(bary), 1e-4);
                float3 a3 = smoothstep(0.0, fw * _WireThickness, bary);
                float wire = 1.0 - min(a3.x, min(a3.y, a3.z));
                return lerp(_FillColor, _WireColor, wire);
            }
            ENDHLSL
        }
    }
}
