Shader "Custom/WallShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _BumpMap("Bumpmap", 2D) = "bump" {}
        _Scale("TextureScale", Range(0,10)) = 1
    }
        SubShader{
          Tags { "RenderType" = "Opaque" }
          Cull Off
          CGPROGRAM
          #pragma surface surf Lambert
          struct Input {
              float2 uv_MainTex;
              float2 uv_BumpMap;
              float3 worldPos;
          };
          sampler2D _MainTex;
          sampler2D _BumpMap;

          half _Glossiness;
          half _Metallic;
          half _Scale;
          fixed4 _Color;
          void surf(Input IN, inout SurfaceOutput o) {
              float2 uv = float2(IN.worldPos.x + IN.worldPos.z, IN.worldPos.y) * _Scale;
              o.Albedo = tex2D(_MainTex, uv).rgb * _Color;
              o.Normal = UnpackNormal(tex2D(_BumpMap, uv));
          }
          ENDCG
        }
            Fallback "Diffuse"
}

