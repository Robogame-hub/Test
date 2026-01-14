    #pragma vertex vert
    #pragma fragment frag
    #pragma multi_compile_fwdbase
    #pragma multi_compile_instancing
    #pragma instancing_options
    #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
    #pragma multi_compile_fragment _ _SHADOWS_SOFT
    #include "UnityInstancing.cginc"
    #include "Lighting.cginc"
    #include "AutoLight.cginc"
    #include "UnityCG.cginc"

    // Shader properties
    float4 _ColorA;
    float4 _ColorB;
    float _GradientRadius;
    float _GradientSoftness;

    struct Attributes
    {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
        float2 uv     : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 pos       : SV_POSITION;
        float3 normal    : TEXCOORD0;
        float4 worldPos  : TEXCOORD1;
        SHADOW_COORDS(2)
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    Varyings vert(Attributes v)
    {
        Varyings o;

        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);

        o.pos = UnityObjectToClipPos(v.vertex);
        o.normal = UnityObjectToWorldNormal(v.normal);
        o.worldPos = mul(unity_ObjectToWorld, v.vertex);

        TRANSFER_SHADOW(o);

        return o;
    }

    half4 frag(Varyings i) : SV_Target
    {
        UNITY_SETUP_INSTANCE_ID(i);

        // Access per-instance gradient center
        float3 gradientCenter = float3(0,0,0);
        float gradientRadius = _GradientRadius;
        float gradientSoftness = _GradientSoftness;

        // Compute distance from gradient center
        float dist = distance(i.worldPos.xyz, gradientCenter);
        float t = smoothstep(gradientRadius - gradientSoftness, gradientRadius + gradientSoftness, dist);

        // Interpolate between Color A and Color B
        float4 color = lerp(_ColorA, _ColorB, t);

        // Shadows
        half shadowAmount = saturate(SHADOW_ATTENUATION(i) * 10);
        color.rgb = lerp(float3(0.0,0.0,0.0) * shadowAmount, color.rgb, shadowAmount);

        // Handle color space
        #ifndef UNITY_COLORSPACE_GAMMA
            color.rgb = GammaToLinearSpace(color.rgb);
        #endif

        return color;
    }
