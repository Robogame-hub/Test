    #pragma vertex vert
    #pragma fragment frag
    #pragma multi_compile_fwdbase
    #pragma multi_compile_instancing
    #pragma instancing_options
    #include "UnityInstancing.cginc"
    #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
    #pragma multi_compile_fragment _ _SHADOWS_SOFT

    #include "Lighting.cginc"
    #include "AutoLight.cginc"
    #include "UnityCG.cginc"
    
    struct Attributes
    {
        float4 vertex  : POSITION;
        float3 normal : NORMAL;
        float2 uv : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
        float3 normal : TEXCOORD1;
        float4 worldPos : TEXCOORD2;
        float4 color : COLOR0; //Got an error saying to use COLOR0 instead of COLOR
        SHADOW_COORDS(3)
        UNITY_VERTEX_INPUT_INSTANCE_ID // use this to access instanced properties in the fragment shader.
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
    
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);
        
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv;
        o.normal = normalize(UnityObjectToWorldNormal(v.normal));
        float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
        o.worldPos.xyz = worldPos;
        o.color = TextureColor(v.uv.x);
        
        TRANSFER_SHADOW(o)

        return o;
    }

    // Define the instancing buffer with per-instance properties
    UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_DEFINE_INSTANCED_PROP(float, _SelfShadingSize)
        UNITY_DEFINE_INSTANCED_PROP(float, _HighlightSmoothness)
        UNITY_DEFINE_INSTANCED_PROP(float, _HighlightStrength)
        UNITY_DEFINE_INSTANCED_PROP(float, _ShadowStrength)
        UNITY_DEFINE_INSTANCED_PROP(float4, _SelectionColor)
    UNITY_INSTANCING_BUFFER_END(Props)

    half ComputeSelfShadowing(half3 normal, half3 lightDir)
    {
        half NdotL = dot(normal, lightDir);
        return saturate((NdotL * 0.5 + 0.5) - UNITY_ACCESS_INSTANCED_PROP(Props, _SelfShadingSize));
    }
    
    half4 frag(Varyings i) : SV_Target
    {
        UNITY_SETUP_INSTANCE_ID(i);
        float4 texColor = i.color;

        // Normal and light direction
        float3 normal = normalize(i.normal);
        float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
        
        // Ambient lighting
        float3 ambient = 0.3 * float3(1.0,1.0,1.0);

        // Diffuse lighting
        float NdotL = max(dot(normal, lightDir), 0.0);
        
        // Quantized lighting steps with interpolation
        float3 diffuse = lerp(0.45 * float3(1.0,1.0,1.0), 0.75 * float3(1.0,1.0,1.0), (NdotL - 0.5) * step(0.5, NdotL));

        // Specular lighting
        float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
        float3 halfDir = normalize(lightDir + viewDir);
        float specAngle = max(dot(normal, halfDir), 0.0);
        
        float3 lighting = ambient + diffuse + specAngle;

        float3 luminanceCoefficients = float3(0.2126, 0.7152, 0.0722);

        #ifdef UNITY_COLORSPACE_GAMMA
        #else
        luminanceCoefficients = float3(0.299, 0.587, 0.114);
        #endif
        
        // Adjust lighting intensity based on brightness
        lighting *= lerp(0.99, 1.0, dot(texColor.rgb, luminanceCoefficients));
        
        float3 reflectDir = reflect(-lighting, normal);
        float specularLight = saturate(dot(reflectDir, halfDir));
        float controlled = smoothstep(UNITY_ACCESS_INSTANCED_PROP(Props, _HighlightSmoothness), 1.0, specularLight) * UNITY_ACCESS_INSTANCED_PROP(Props, _HighlightStrength); //highlight strength
        texColor.rgb = saturate(texColor.rgb + (controlled));
        
        half shadowAmount = saturate(SHADOW_ATTENUATION(i) * 10); //For Built-in
        shadowAmount = saturate(shadowAmount * ComputeSelfShadowing(normal, lightDir));
        
        float3 shadowCol = lerp(texColor.rgb, float3(0.0,0.0,0.0), UNITY_ACCESS_INSTANCED_PROP(Props, _ShadowStrength));
        texColor.rgb = lerp(shadowCol, texColor.rgb, shadowAmount);

        float brightness = dot(texColor.rgb, luminanceCoefficients);
        
        texColor.rgb *= lerp(texColor.rgb, texColor.rgb * texColor.rgb, brightness);
        texColor.rgb *= (float3(1,1,1) * 1.75); //To brighten the colors before returning output
        
        #ifdef UNITY_COLORSPACE_GAMMA
        #else
        texColor.rgb = GammaToLinearSpace(texColor.rgb);
        #endif
        
        return texColor;
    }