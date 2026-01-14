Shader "TankGame/BulletTracerShader"
{
    Properties
    {
        [Header(Tracer Settings)]
        _MainTex            ("Texture", 2D)                     = "white" {}
        _Color              ("Tracer Color", Color)             = (1, 0.5, 0, 1)
        _EmissionStrength   ("Emission Strength", Range(0, 10)) = 2.0
        
        [Header(Fade Settings)]
        _FadeDistance       ("Fade Distance", Range(0, 1))      = 0.3
        _FadeSharpness      ("Fade Sharpness", Range(1, 10))    = 2.0
        
        [Header(Render Settings)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5  // SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 1  // One (Additive)
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        LOD 100
        
        Pass
        {
            Name "TracerPass"
            
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float fogFactor : TEXCOORD1;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _EmissionStrength;
                float _FadeDistance;
                float _FadeSharpness;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                // Sample texture
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // Apply color
                float4 finalColor = texColor * _Color * input.color;
                
                // Fade effect (от начала к концу трассера)
                // UV.x идет от 0 (начало) до 1 (конец)
                float fade = 1.0 - input.uv.x;
                fade = pow(fade, _FadeSharpness);
                
                // Дополнительный fade по Y для сужения
                float widthFade = 1.0 - abs(input.uv.y * 2.0 - 1.0);
                widthFade = smoothstep(0.0, 1.0, widthFade);
                
                // Комбинируем fade
                float totalFade = fade * widthFade;
                
                // Применяем emission
                finalColor.rgb *= _EmissionStrength;
                finalColor.a *= totalFade;
                
                // Apply fog
                finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

