#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_fwdbase
#pragma multi_compile_instancing
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile_fragment _ _SHADOWS_SOFT

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

struct Attributes
{
    float4 positionOS  : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : TEXCOORD1;
    float4 worldPos : TEXCOORD2;
    float4 color : COLOR0;
    float4 shadowCoords : TEXCOORD3;
};

UNITY_INSTANCING_BUFFER_START(PerInstanceData)
    UNITY_DEFINE_INSTANCED_PROP(float4, _Color1)
    UNITY_DEFINE_INSTANCED_PROP(float4, _Color2)
    UNITY_DEFINE_INSTANCED_PROP(float4, _Color3)
    UNITY_DEFINE_INSTANCED_PROP(float4, _Color4)
    UNITY_DEFINE_INSTANCED_PROP(float4, _Color5)
    UNITY_DEFINE_INSTANCED_PROP(float4, _Color6)
    UNITY_DEFINE_INSTANCED_PROP(float4, _Color7)
UNITY_INSTANCING_BUFFER_END(PerInstanceData)

float4 TextureColor(float uvX)
{
    float count = 7;
    float scaledUvX = uvX * (count - 1);
    
    // Using step() to avoid conditionals
    float w1 = step(0.0, 0.5 - abs(scaledUvX - 0.0));
    float w2 = step(0.0, 0.5 - abs(scaledUvX - 1.0));
    float w3 = step(0.0, 0.5 - abs(scaledUvX - 2.0));
    float w4 = step(0.0, 0.5 - abs(scaledUvX - 3.0));
    float w5 = step(0.0, 0.5 - abs(scaledUvX - 4.0));
    float w6 = step(0.0, 0.5 - abs(scaledUvX - 5.0));
    float w7 = step(0.0, 0.5 - abs(scaledUvX - 6.0));

// Compute final color by summing weighted colors
float4 color = UNITY_ACCESS_INSTANCED_PROP(PerInstanceData, _Color1) * w1 +
               UNITY_ACCESS_INSTANCED_PROP(PerInstanceData, _Color2) * w2 +
               UNITY_ACCESS_INSTANCED_PROP(PerInstanceData, _Color3) * w3 +
               UNITY_ACCESS_INSTANCED_PROP(PerInstanceData, _Color4) * w4 +
               UNITY_ACCESS_INSTANCED_PROP(PerInstanceData, _Color5) * w5 +
               UNITY_ACCESS_INSTANCED_PROP(PerInstanceData, _Color6) * w6 +
               UNITY_ACCESS_INSTANCED_PROP(PerInstanceData, _Color7) * w7;
    
    return color;
}

Varyings vert(Attributes v)
{
    Varyings o;
    
    o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
    VertexPositionInputs positions = GetVertexPositionInputs(v.positionOS.xyz);
    o.uv = v.uv;
    o.normal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
    float3 worldPos = mul(unity_ObjectToWorld, v.positionOS).xyz;
    o.worldPos.xyz = worldPos;
    o.color = TextureColor(v.uv.x);
    o.shadowCoords = GetShadowCoord(positions);

    return o;
}

// Define the instancing buffer with per-instance properties
UNITY_INSTANCING_BUFFER_START(Props)
    UNITY_DEFINE_INSTANCED_PROP(float, _SelfShadingSize)
    UNITY_DEFINE_INSTANCED_PROP(float, _HighlightSmoothness)
    UNITY_DEFINE_INSTANCED_PROP(float, _HighlightStrength)
    UNITY_DEFINE_INSTANCED_PROP(float, _ShadowStrength)
UNITY_INSTANCING_BUFFER_END(Props)

half ComputeSelfShadowing(half3 normal, half3 lightDir)
{
    half NdotL = dot(normal, lightDir);
    return saturate((NdotL * 0.5 + 0.5) - UNITY_ACCESS_INSTANCED_PROP(Props, _SelfShadingSize));
}

inline half3 GammaToLinearSpace (half3 sRGB)
{
    return sRGB * (sRGB * (sRGB * 0.305306011h + 0.682171111h) + 0.012522878h);
}

half4 frag(Varyings i) : SV_Target
{
    float4 texColor = i.color;
    
    float3 normal = normalize(i.normal);
    float3 lightDir = normalize(_MainLightPosition.xyz);
    
    float3 ambient = 0.3 * float3(1.0,1.0,1.0);
    
    float NdotL = max(dot(normal, lightDir), 0.0);
    
    float3 diffuse = lerp(0.45 * float3(1.0,1.0,1.0), 0.75 * float3(1.0,1.0,1.0), (NdotL - 0.5) * step(0.5, NdotL));
    
    float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
    float3 halfDir = normalize(lightDir + viewDir);
    float specAngle = max(dot(normal, halfDir), 0.0);
    
    float3 lighting = ambient + diffuse + specAngle;

    float3 luminanceCoefficients = float3(0.2126, 0.7152, 0.0722);

    #ifdef UNITY_COLORSPACE_GAMMA
    #else
    luminanceCoefficients = float3(0.299, 0.587, 0.114);
    #endif
    
    lighting *= lerp(0.99, 1.0, dot(texColor.rgb, luminanceCoefficients));
    
    float3 reflectDir = reflect(-lighting, normal);
    float specularLight = saturate(dot(reflectDir, halfDir));
    float controlled = smoothstep(UNITY_ACCESS_INSTANCED_PROP(Props, _HighlightSmoothness), 1.0, specularLight) * UNITY_ACCESS_INSTANCED_PROP(Props, _HighlightStrength); //highlight strength
    texColor.rgb = saturate(texColor.rgb + (controlled));
    
    half shadowAmount = MainLightRealtimeShadow(i.shadowCoords);
    shadowAmount = saturate(shadowAmount * ComputeSelfShadowing(normal, lightDir));
    
    float3 shadowCol = lerp(texColor.rgb, float3(0.0,0.0,0.0), UNITY_ACCESS_INSTANCED_PROP(Props, _ShadowStrength));
    texColor.rgb = lerp(shadowCol, texColor.rgb, shadowAmount);
    
    float brightness = dot(texColor.rgb, luminanceCoefficients); // Vibrant color to saturate the colors
    
    texColor.rgb *= lerp(texColor.rgb, texColor.rgb * texColor.rgb, brightness);
    texColor.rgb *= (float3(1,1,1) * 1.75); //To brighten the colors before returning output

    #ifdef UNITY_COLORSPACE_GAMMA
    #else
    texColor.rgb = GammaToLinearSpace(texColor.rgb);
    #endif
    
    return texColor;
}