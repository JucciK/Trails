Shader "Jucci/LinearDepth"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            ZWrite On
            ZTest LEqual
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            float4x4 _CameraMatrixV;
            float _CameraFar;

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = UnityObjectToViewPos(v.vertex);
                return o;
            }

            float4 frag(v2f i) : SV_Target {
                float linearDepth = -i.worldPos.z / _CameraFar;
                return float4(linearDepth, i.worldPos.x, 0, 1);
            }
            ENDCG
        }
    }
}