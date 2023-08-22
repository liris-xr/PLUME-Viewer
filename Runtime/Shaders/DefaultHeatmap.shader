Shader "Unlit/DefaultHeatmap"
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
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
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
                return default_color();
            }
            ENDCG
        }
    }
}