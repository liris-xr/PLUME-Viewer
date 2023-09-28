Shader "Unlit/SamplesHeatmap"
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
            #pragma geometry geom
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Math.cginc"

            uniform float _Shading;
            
            StructuredBuffer<uint> triangles_resolution_buffer;
            StructuredBuffer<uint> triangles_samples_index_offset_buffer;
            StructuredBuffer<float> samples_value_buffer;
            StructuredBuffer<uint> samples_min_value;
            StructuredBuffer<uint> samples_max_value;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2g
            {
                float4 vertex : SV_POSITION;
                float4 pos_world : TEXCOORD0;
                float3 normal_dir : TEXCOORD1;
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
                uint triangle_id : TEXCOORD0;
                float4 pos_world : TEXCOORD1;
                linear float3 barycentric_weights : TEXCOORD2;
                float3 normal_dir : TEXCOORD3;
            };

            v2g vert(appdata v)
            {
                v2g o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.pos_world = mul(unity_ObjectToWorld, v.vertex);
                o.normal_dir = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g tri[3], inout TriangleStream<g2f> stream, uint pid : SV_PrimitiveID)
            {
                g2f o[3];

                o[0].barycentric_weights = float3(1, 0, 0);
                o[1].barycentric_weights = float3(0, 1, 0);
                o[2].barycentric_weights = float3(0, 0, 1);

                for (int j = 0; j < 3; ++j)
                {
                    o[j].triangle_id = pid;
                    o[j].normal_dir = tri[j].normal_dir;
                    o[j].pos_world = tri[j].pos_world;
                    o[j].vertex = tri[j].vertex;
                    stream.Append(o[j]);
                }
            }

            fixed4 fixation_density_to_color(const float val)
            {
                const float min_value = asfloat(samples_min_value[0]);
                const float max_value = asfloat(samples_max_value[0]);
                const float mapped_val = saturate(map(val, min_value, max_value, 0, 1));

                // Lerp from blue to red
                const float h = 0.66f - lerp(0, 0.66f, mapped_val);
                // const float h = 0.66f - lerp(0, 0.66f, val);
                float r = abs(h * 6 - 3) - 1;
                float g = 2 - abs(h * 6 - 2);
                float b = 2 - abs(h * 6 - 4);
                return saturate(fixed4(r, g, b, 1));
            }

            uint scaled_barycentric_weights_to_sample_idx(const int2 sub_triangle_grid_pos, const uint resolution)
            {
                const uint col = sub_triangle_grid_pos.x;
                const uint row = resolution - sub_triangle_grid_pos.y;
                const int idx = col + row * (row + 1) / 2;
                return idx;
            }

            /*
             * Custom implementation of Mesh Colors, Cf. http://www.cemyuksel.com/research/meshcolors/
             * 
             * Mesh Colors divides the triangle into sub-triangles. It uses local sub-triangles barycentric weights
             * to apply colors from nearest samples.
             *
             * The following triangle has a resolution of 3 (3 sub-triangles per side).
             * It contains a total of 10 samples (one sample per sub-triangle vertex).
             * 
             *           v1
             *           /\
             *          /__\
             *         /\  /\
             *        /__\/__\
             *       /\  /\  /\
             *      /__\/__\/__\
             *     v0          v2
             *
             *  row
             *  ↑
             *  └-→ column
             *
             *  col = b.x;
             *  row = triangle_resolution - b.y;
             *
             *  barycentric_weight(v0) = (1, 0, 0)
             *  barycentric_weight(v1) = (0, 1, 0)
             *  barycentric_weight(v2) = (0, 0, 1)
             */
            fixed4 frag(g2f i) : COLOR
            {
                // Prevent MSAA from over-interpolating values
                i.barycentric_weights = saturate(i.barycentric_weights);
                
                // Ensures that barycentric coordinates adds up to 1.
                // This compensates floating point interpolation inaccuracies.
                i.barycentric_weights.z = 1.0 - (i.barycentric_weights.x + i.barycentric_weights.y);

                const int triangle_resolution = triangles_resolution_buffer[i.triangle_id];
                const int offset = triangles_samples_index_offset_buffer[i.triangle_id];

                // Values in range [0; triangle_resolution]
                const float3 scaled_barycentric_weights = triangle_resolution * i.barycentric_weights;
                // Sub-triangle position in the grid, values in range [0; triangle_resolution[
                const int3 b = floor(scaled_barycentric_weights);
                // Local sub-triangle barycentric weights
                float3 w = scaled_barycentric_weights - b;

                half4 color;

                // Left side of parallelogram composed of the two triangles
                if (abs(w.x + w.y + w.z - 1) <= 2 * epsilon)
                {
                    const uint idx0 = scaled_barycentric_weights_to_sample_idx(b + int2(1, 0), triangle_resolution);
                    const uint idx1 = scaled_barycentric_weights_to_sample_idx(b + int2(0, 1), triangle_resolution);
                    const uint idx2 = scaled_barycentric_weights_to_sample_idx(b + int2(0, 0), triangle_resolution);
                    const float val0 = samples_value_buffer[offset + idx0];
                    const float val1 = samples_value_buffer[offset + idx1];
                    const float val2 = samples_value_buffer[offset + idx2];
                    color = fixation_density_to_color(val0 * w.x + val1 * w.y + val2 * w.z);
                }

                // Right side of parallelogram composed of the two triangles
                else if (abs(w.x + w.y + w.z - 2) <= 2 * epsilon)
                {
                    const uint idx0 = scaled_barycentric_weights_to_sample_idx(b + int2(0, 1), triangle_resolution);
                    const uint idx1 = scaled_barycentric_weights_to_sample_idx(b + int2(1, 0), triangle_resolution);
                    const uint idx2 = scaled_barycentric_weights_to_sample_idx(b + int2(1, 1), triangle_resolution);
                    const float val0 = samples_value_buffer[offset + idx0];
                    const float val1 = samples_value_buffer[offset + idx1];
                    const float val2 = samples_value_buffer[offset + idx2];
                    w = float3(1, 1, 1) - w;
                    color = fixation_density_to_color(val0 * w.x + val1 * w.y + val2 * w.z);
                } else
                {
                    const uint idx = scaled_barycentric_weights_to_sample_idx(b, triangle_resolution);
                    const float val = samples_value_buffer[offset + idx];
                    color = fixation_density_to_color(val);
                }
                
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