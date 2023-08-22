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
                    o.clip_pos = o.vertex;
                    
                    #if defined(UNITY_REVERSED_Z)
                    #if UNITY_REVERSED_Z == 1
                    //D3d with reversed Z => remap [near, 0] -> [0, 1]
                    o.clip_pos.z = 1 - o.clip_pos.z / _ProjectionParams.y;
                    #else
                    //GL with reversed z => remap [near, -far] -> [0, 1]
                    // UNTESTED
                    o.clip_pos.z = (-o.clip_pos.z + _ProjectionParams.y) / (_ProjectionParams.y + _ProjectionParams.z);
                    #endif
                    #elif UNITY_UV_STARTS_AT_TOP
                    //D3d without reversed z => remap [0, far] -> [0, 1]
                    o.clip_pos.z = o.clip_pos.z / _ProjectionParams.z;
                    #else
                    //Opengl => remap [-near, far] -> [0, 1]
                    o.clip_pos.z = (o.clip_pos.z + _ProjectionParams.y) / (_ProjectionParams.y + _ProjectionParams.z);
                    #endif
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