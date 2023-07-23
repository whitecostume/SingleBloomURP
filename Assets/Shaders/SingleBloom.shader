Shader "Unlit/SingleBloom"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color",color) = (1,1,1,1)
        [HDR]_BloomColor ("BloomColor",color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "LightMode" = "SingleBloom" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            struct PixelOutput {
                float4 col0 : COLOR0;
                float4 col1 : COLOR1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _BloomColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            PixelOutput frag (v2f i) : SV_Target
            {
                PixelOutput o;
                float4 c = tex2D(_MainTex, i.uv); 
                // sample the texture
                o.col0 = c * _Color;
                o.col1 = c *  _Color + _BloomColor;
    
                return o;
            }
            ENDHLSL
        }
    }
}
