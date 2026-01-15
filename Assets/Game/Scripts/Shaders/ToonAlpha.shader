Shader "Custom/ToonAlpha"
{
    Properties
    {
        [Header(Base)]
        _BaseMap            ("Texture", 2D)                       = "white" {}
        _BaseColor          ("Color", Color)                      = (1,1,1,1)
        
        [Header(Transparency)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5 // SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10 // OneMinusSrcAlpha
        [Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 0
        [Toggle] _AlphaClip ("Alpha Clipping", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        
        [Header(Toon Shading)]
        _ShadowStep         ("Shadow Step", Range(0, 1))          = 0.5
        _ShadowStepSmooth   ("Shadow Step Smooth", Range(0, 1))  = 0.04
        
        [Space] 
        _SpecularStep       ("Specular Step", Range(0, 1))        = 0.6
        _SpecularStepSmooth ("Specular Step Smooth", Range(0, 1))= 0.05
        [HDR]_SpecularColor ("Specular Color", Color)             = (1,1,1,1)
        
        [Space]
        _RimStep            ("Rim Step", Range(0, 1))             = 0.65
        _RimStepSmooth      ("Rim Step Smooth",Range(0,1))        = 0.4
        _RimColor           ("Rim Color", Color)                  = (1,1,1,1)
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline" 
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull Back

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #pragma vertex vert
            #pragma fragment frag
            
            #pragma shader_feature_local _ALPHACLIP_ON
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
             
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D(_BaseMap); 
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Cutoff;
                float _ShadowStep;
                float _ShadowStepSmooth;
                float _SpecularStep;
                float _SpecularStepSmooth;
                float4 _SpecularColor;
                float _RimStepSmooth;
                float _RimStep;
                float4 _RimColor;
            CBUFFER_END

            struct Attributes
            {     
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            }; 

            struct Varyings
            {
                float2 uv            : TEXCOORD0;
                float3 normalWS      : TEXCOORD1;
                float3 viewDirWS     : TEXCOORD2;
                float4 shadowCoord   : TEXCOORD3;
                float3 positionWS    : TEXCOORD4;
                float fogFactor      : TEXCOORD5;
                float4 positionCS    : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                    
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                output.positionWS = vertexInput.positionWS;
                output.positionCS = vertexInput.positionCS;
                output.shadowCoord = GetShadowCoord(vertexInput);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                // Sample texture
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 baseColor = baseMap * _BaseColor;
                
                // Alpha clipping
                #ifdef _ALPHACLIP_ON
                    clip(baseColor.a - _Cutoff);
                #endif
                
                // Normalize vectors
                half3 normalWS = normalize(input.normalWS);
                half3 viewDirWS = normalize(input.viewDirWS);
                
                // Get main light
                Light mainLight = GetMainLight(input.shadowCoord);
                half3 lightDir = normalize(mainLight.direction);
                half3 lightColor = mainLight.color;
                
                // Shadow attenuation
                half shadowAtten = mainLight.shadowAttenuation;
                
                // Toon shadow calculation
                half NdotL = dot(normalWS, lightDir);
                half toonDiffuse = smoothstep(_ShadowStep - _ShadowStepSmooth, _ShadowStep + _ShadowStepSmooth, NdotL);
                toonDiffuse *= shadowAtten;
                
                // Ambient
                half3 ambient = half3(0.3, 0.3, 0.3);
                
                // Diffuse
                half3 diffuse = lerp(ambient, lightColor, toonDiffuse);
                
                // Specular (Blinn-Phong toon style)
                half3 halfDir = normalize(lightDir + viewDirWS);
                half NdotH = max(0, dot(normalWS, halfDir));
                half specular = smoothstep(_SpecularStep - _SpecularStepSmooth, _SpecularStep + _SpecularStepSmooth, NdotH);
                specular *= toonDiffuse; // Only show specular in lit areas
                half3 specularColor = specular * _SpecularColor.rgb * _SpecularColor.a;
                
                // Rim light (Fresnel)
                half rim = 1.0 - max(0, dot(viewDirWS, normalWS));
                half rimIntensity = smoothstep(_RimStep - _RimStepSmooth, _RimStep + _RimStepSmooth, rim);
                half3 rimColor = rimIntensity * _RimColor.rgb * _RimColor.a;
                
                // Combine lighting
                half3 finalColor = baseColor.rgb * diffuse + specularColor + rimColor;
                
                // Apply fog
                finalColor = MixFog(finalColor, input.fogFactor);
                
                return half4(finalColor, baseColor.a);
            }
            ENDHLSL
        }

        // Shadow Caster Pass (для отбрасывания теней)
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            #pragma shader_feature_local _ALPHACLIP_ON
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Cutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float3 _LightDirection;

            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                
                return positionCS;
            }

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.positionCS = GetShadowPositionHClip(input);
                
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                #ifdef _ALPHACLIP_ON
                    half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                    half alpha = baseMap.a * _BaseColor.a;
                    clip(alpha - _Cutoff);
                #endif
                
                return 0;
            }
            ENDHLSL
        }

        // Depth Only Pass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            
            #pragma shader_feature_local _ALPHACLIP_ON
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Cutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                #ifdef _ALPHACLIP_ON
                    half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                    half alpha = baseMap.a * _BaseColor.a;
                    clip(alpha - _Cutoff);
                #endif
                
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
    CustomEditor "UnityEditor.ShaderGUI"
}

