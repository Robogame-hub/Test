Shader "WalldoffStudios/PlatformShader"
{
    Properties
    {
        _ColorA ("Color A", Color) = (1, 0, 0, 1)
        _ColorB ("Color B", Color) = (0, 0, 1, 1)
        _GradientRadius ("Gradient Radius", Float) = 1.0
        _GradientSoftness ("Gradient Softness", Float) = 0.1
    }
    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.universal": "10.2.1"
        }
        Pass
        {
            Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
            HLSLPROGRAM
            #include "PlatformURP.cginc"
            ENDHLSL
        }
        Pass 
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            CGPROGRAM
            #include "ToonShaders/ShadowCasting.cginc"
            ENDCG
        }
    }

    SubShader
    {
        
        Pass
        {
            Tags { "LightMode"="ForwardBase" "RenderType"="Opaque" }
            CGPROGRAM
            #include "PlatformBIP.cginc"
            ENDCG
        }
        Pass 
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            CGPROGRAM
            #include "ToonShaders/ShadowCasting.cginc"
            ENDCG
        }
    }
}
