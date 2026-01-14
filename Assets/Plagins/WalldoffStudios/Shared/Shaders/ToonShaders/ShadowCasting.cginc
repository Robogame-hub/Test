#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_instancing
#include <UnityShaderUtilities.cginc>

struct appdata {
    float4 vertex : POSITION;
};

struct Varyings {
    float4 pos : SV_POSITION;
};

Varyings vert(appdata v) {
    Varyings o;
    o.pos = UnityObjectToClipPos(v.vertex);
    return o;
}

float4 frag() : SV_Target {
    return 1.0;
}
