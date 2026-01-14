Shader "TankGame/BulletTracerAdvanced"
{
    Properties
    {
        [Header(Tracer Settings)]
        _MainTex            ("Texture", 2D)                     = "white" {}
        _StartColor         ("Start Color (Hot)", Color)        = (1, 1, 0.5, 1)
        _EndColor           ("End Color (Cool)", Color)         = (1, 0.3, 0, 0.5)
        _EmissionStrength   ("Emission Strength", Range(0, 20)) = 5.0
        
        [Header(Glow Settings)]
        _GlowWidth          ("Glow Width", Range(0, 2))         = 1.0
        _GlowIntensity      ("Glow Intensity", Range(0, 5))     = 2.0
        
        [Header(Animation)]
        _ScrollSpeed        ("Scroll Speed", Float)             = 2.0
        _Noise              ("Noise Amount", Range(0, 1))       = 0.1
        
        [Header(Fade Settings)]
        _FadeSharpness      ("Fade Sharpness", Range(1, 10))    = 3.0
        _WidthFalloff       ("Width Falloff", Range(1, 5))      = 2.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+10"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        LOD 100
        
        Pass
        {
            Name "TracerAdvancedPass"
            
            Blend SrcAlpha One // Additive blending
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
                float4 _StartColor;
                float4 _EndColor;
                float _EmissionStrength;
                float _GlowWidth;
                float _GlowIntensity;
                float _ScrollSpeed;
                float _Noise;
                float _FadeSharpness;
                float _WidthFalloff;
            CBUFFER_END
            
            // Простая функция noise
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }
            
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
                // Анимированные UV
                float2 uv = input.uv;
                uv.x += _Time.y * _ScrollSpeed;
                
                // Sample texture
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                
                // Градиент цвета от начала к концу
                float colorBlend = input.uv.x;
                float4 gradientColor = lerp(_StartColor, _EndColor, colorBlend);
                
                // Применяем цвет
                float4 finalColor = texColor * gradientColor * input.color;
                
                // Fade от начала к концу (обратный - начало яркое, конец тусклый)
                float lengthFade = 1.0 - input.uv.x;
                lengthFade = pow(lengthFade, _FadeSharpness);
                
                // Центральное свечение (ярче в центре, темнее по краям)
                float centerDist = abs(input.uv.y * 2.0 - 1.0);
                float widthGlow = 1.0 - pow(centerDist, _WidthFalloff);
                widthGlow = smoothstep(0.0, 1.0, widthGlow);
                
                // Дополнительное внешнее свечение
                float outerGlow = exp(-centerDist * _GlowWidth) * _GlowIntensity;
                
                // Добавляем шум для живости
                float noise = hash(input.uv * 100.0 + _Time.y) * _Noise;
                
                // Комбинируем все эффекты
                float intensity = lengthFade * (widthGlow + outerGlow) * (1.0 + noise);
                
                // Применяем emission и интенсивность
                finalColor.rgb *= _EmissionStrength * intensity;
                finalColor.a *= intensity;
                
                // Apply fog
                finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);
                
                return saturate(finalColor);
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

