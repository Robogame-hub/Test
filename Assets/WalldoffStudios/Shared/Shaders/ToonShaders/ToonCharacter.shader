shader "WalldoffStudios/ToonCharacter"
{
    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.universal": "10.2.1"
        }
        Pass
        {
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }
            Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
            HLSLPROGRAM
            #include "ToonColorsURP.cginc"
            ENDHLSL
        }
        Pass 
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            CGPROGRAM
            #include "ShadowCasting.cginc"
            ENDCG
        }
    }

    SubShader
    {
        
        Pass
        {
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }
            Tags { "LightMode"="ForwardBase" "RenderType"="Opaque" }
            CGPROGRAM
            #include "ToonColorsBIP.cginc"
            ENDCG
        }
        Pass 
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            CGPROGRAM
            #include "ShadowCasting.cginc"
            ENDCG
        }
    }
}