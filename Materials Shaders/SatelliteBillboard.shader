Shader "Unlit/SatelliteBillboard"
{
    Properties {
        _MainTex ("Satellite Texture", 2D) = "white" {}
    }

    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                v2f o;

    // World position of the center of the instance
    float3 worldCenter = mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz;

    // Get camera basis vectors (in world space)
    float3 right = _WorldSpaceCameraPos - worldCenter;
    float3 up = float3(0,1,0); // Assuming a Y-up world; adjust if needed

    // Compute forward and recompute up for true billboarding
    float3 forward = normalize(worldCenter - _WorldSpaceCameraPos);
    right = normalize(cross(up, forward));
    up = normalize(cross(forward, right));

    // Scale based on object scale
    float3 scale = float3(
        length(unity_ObjectToWorld._m00_m01_m02),
        length(unity_ObjectToWorld._m10_m11_m12),
        length(unity_ObjectToWorld._m20_m21_m22)
    );

    // Offset the vertex position in world space
    float3 offset = right * v.vertex.x * scale.x + up * v.vertex.y * scale.y;
    float3 worldPos = worldCenter + offset;

    // Project to clip space
    o.pos = UnityWorldToClipPos(worldPos);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
