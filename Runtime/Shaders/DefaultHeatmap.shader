Shader "Unlit/DefaultHeatmap"
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
            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            uniform float _Shading;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 pos_world : TEXCOORD0;
                float3 normal_dir : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.pos_world = mul(unity_ObjectToWorld, v.vertex);
                o.normal_dir = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 default_color()
            {
                // Lerp from blue to red
                const float h = 0.66f - lerp(0, 0.66f, 0);
                // const float h = 0.66f - lerp(0, 0.66f, val);
                float r = abs(h * 6 - 3) - 1;
                float g = 2 - abs(h * 6 - 2);
                float b = 2 - abs(h * 6 - 4);
                return saturate(fixed4(r, g, b, 1));
            }

            fixed4 frag(v2f i) : COLOR
            {
                fixed4 color = default_color();

                if (!_Shading)
                {
                    return color;
                }

                // Apply lighting
                const float3 normal_direction = i.normal_dir;
                float3 light_direction;

                // Directional light when w == 0, spot or point light when w == 1
                if (_WorldSpaceLightPos0.w == 0.0)
                {
                    // _WorldSpaceLightPos0 is the directional light world space direction
                    light_direction = normalize(_WorldSpaceLightPos0.xyz);
                }
                else
                {
                    // _WorldSpaceLightPos0 is the spot/point light world space position
                    light_direction = normalize(_WorldSpaceLightPos0.xyz - i.pos_world.xyz);
                }

                float light = saturate(dot(normal_direction, light_direction));
                return float4(color.xyz * (light + 0.5f), 1);
            }
            ENDCG
        }
    }
}