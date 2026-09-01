Shader "Unlit/SegmentedObjectDepth"
{
    Properties {}
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        Pass
        {
            // Nearest-surface depth must win so the rendered depth texture matches the projection
            // test. Enforce explicitly since the pre-pass runs under the switched pipeline for HDRP
            // records, whose default render state differs.
            ZWrite On
            ZTest LEqual
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            int object_instance_id;

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 clip_pos : TEXCOORD0;
            };

            v2f vert(const float4 vertex : POSITION)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(vertex);

                const bool orthographic_projection = unity_OrthoParams.w;

                if (orthographic_projection)
                {
                    o.clip_pos = o.vertex;

                    #if defined(UNITY_REVERSED_Z)
                    #if UNITY_REVERSED_Z == 1
                    //D3d with reversed Z
                    o.clip_pos.z = 1 - o.clip_pos.z;
                    #else
                    //GL with reversed z
                    // UNTESTED
                    o.clip_pos.z = 1 - o.clip_pos.z;
                    #endif
                    #elif UNITY_UV_STARTS_AT_TOP
                    //D3d without reversed z => nothing to do
                    o.clip_pos.z = o.clip_pos.z;
                    #else
                    //Opengl => nothing to do
                    o.clip_pos.z = (o.clip_pos.z + 1) / 2.0;
                    #endif
                }
                else
                {
                    // Linear eye-space depth in metres (0 at the camera plane, increasing forward).
                    // Platform-independent, so it matches the compute shader without depending on the
                    // reversed-Z clip convention (which differs from the raw projection matrix the
                    // compute shader uses). TEXCOORD interpolation is perspective-correct, which is
                    // exact for eye-space Z since it is linear in world space.
                    o.clip_pos = o.vertex;
                    o.clip_pos.z = -UnityObjectToViewPos(vertex).z;
                }
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // output depth in range [0; 1] with 0 corresponding to near and 1 to far
                float depth = i.clip_pos.z;
                return float4(depth, asfloat(object_instance_id), 0, 1);
            }
            ENDCG
        }
    }
}