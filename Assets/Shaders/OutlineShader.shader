Shader "Custom/Outline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)

        _OutlineColor ("Glow Color", Color) = (1,1,1,1)
        _OutlineWidth ("Glow Width", Float) = 0.03
        _GlowSoftness ("Glow Softness", Float) = 2.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        // --- PASS 1: Soft Glow Outline ---
        Pass
        {
            Name "SoftGlow"
            Cull Front

            CGPROGRAM
            #pragma vertex vertGlow
            #pragma fragment fragGlow
            #include "UnityCG.cginc"

            float _OutlineWidth;
            float _GlowSoftness;
            float4 _OutlineColor;

            struct GlowAppData
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct GlowV2F
            {
                float4 pos : SV_POSITION;
            };

            GlowV2F vertGlow (GlowAppData v)
            {
                GlowV2F o;

                float3 n = normalize(v.normal);
                float3 offset = n * _OutlineWidth;

                o.pos = UnityObjectToClipPos(v.vertex + float4(offset, 0));
                return o;
            }

            float4 fragGlow (GlowV2F i) : SV_Target
            {
                float glow = saturate(1.0 / _GlowSoftness);
                return float4(_OutlineColor.rgb, _OutlineColor.a * glow);
            }
            ENDCG
        }

        // --- PASS 2: Base Mesh ---
        Pass
        {
            Name "Base"
            Cull Back

            CGPROGRAM
            #pragma vertex vertBaseMesh
            #pragma fragment fragBaseMesh
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Color;

            struct BaseAppData
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct BaseV2F
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            BaseV2F vertBaseMesh (BaseAppData v)
            {
                BaseV2F o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 fragBaseMesh (BaseV2F i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * _Color;
            }
            ENDCG
        }
    }
}