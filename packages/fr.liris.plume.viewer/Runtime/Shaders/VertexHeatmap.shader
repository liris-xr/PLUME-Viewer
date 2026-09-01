Shader "Unlit/VertexHeatmap"
{
    Properties
    {
        [Toggle] _Shading("Enable shading", Float) = 1
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        Pass
        {
            // Enforce opaque depth state explicitly: when an HDRP record is displayed, the heatmap
            // renders under the switched pipeline (built-in/URP) whose default render state differs,
            // which otherwise breaks depth sorting (internal meshes bleed through the surface).
            ZWrite On
            ZTest LEqual
            Cull Back

            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Math.cginc"

            uniform float _Shading;

            // Float values stored as uint bits; read with asfloat()
            StructuredBuffer<uint> vertex_value_buffer;
            StructuredBuffer<uint> samples_max_value;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 pos_world : TEXCOORD0;
                float3 normal_dir : TEXCOORD1;
                float value : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.pos_world = mul(unity_ObjectToWorld, v.vertex);
                o.normal_dir = UnityObjectToWorldNormal(v.normal);
                o.value = asfloat(vertex_value_buffer[v.vertexID]);
                return o;
            }

            fixed4 value_to_color(const float val)
            {
                const float max_value = asfloat(samples_max_value[0]);
                const float mapped_val = saturate(map(val, 0, max_value, 0, 1));

                // Lerp from blue (h=0.66) to red (h=0)
                const float h = 0.66f - lerp(0, 0.66f, mapped_val);
                float r = abs(h * 6 - 3) - 1;
                float g = 2 - abs(h * 6 - 2);
                float b = 2 - abs(h * 6 - 4);
                return saturate(fixed4(r, g, b, 1));
            }

            fixed4 frag(v2f i) : COLOR
            {
                fixed4 color = value_to_color(i.value);

                if (!_Shading)
                    return color;

                const float3 normal_direction = i.normal_dir;
                float3 light_direction;

                if (_WorldSpaceLightPos0.w == 0.0)
                    light_direction = normalize(_WorldSpaceLightPos0.xyz);
                else
                    light_direction = normalize(_WorldSpaceLightPos0.xyz - i.pos_world.xyz);

                const float light = saturate(dot(normal_direction, light_direction));
                return float4(color.xyz * (light + 0.5f), 1);
            }
            ENDCG
        }
    }
}
