Shader "Unlit/BloomBlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        HLSLINCLUDE

        float GaussWeight2D(float x,float y,float sigma)
        {
            float PI = 3.14159265358;
            float E = 2.71828182846;
            float sigma_2 = pow(sigma,2);

            float a = -(x*x + y*y) / (2.0 * sigma_2);
            return pow(E,a) / (2.0 * PI * sigma_2);
        }

        float3 GaussNxN(sampler2D tex,float2 uv,int n,float2 stride,float sigma)
        {
            float3 color = float3(0,0,0);
            int r = n / 2;
            float weight = 0.0;

            for (int i = -r; i <= r; i++) {
                for (int j = -r; j <= r; j++) {
                    
                    float2 coord = uv + float2(i,j) * stride;
                    float3 c = tex2D(tex,coord).rgb; 
                    float luma = dot(float3(0.2126,0.7152,0.0722),c);

                    float w1 = GaussWeight2D(i,j,sigma);
                    float w2 = 1.0 / (1.0 + luma) * w1;
                    color += c * w2;
                    weight += w2;
                }
            }

            color /= weight;
            return color;
        }

        ENDHLSL

        // Donwsize And Blur
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

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // sample the texture
                half4 color = half4(0,0,0,1);

                color.rgb = GaussNxN(_MainTex,i.uv,5,_MainTex_TexelSize.xy,1.0);

                return color;
            }
            ENDHLSL
        }

        // UpSize And Blur
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

            sampler2D _PrewMip;
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // sample the texture
                half4 color = half4(0,0,0,1);
                float2 prev_stride = 0.5 * _MainTex_TexelSize.xy;
                float2 curr_stride = _MainTex_TexelSize.xy;

                color.rgb = GaussNxN(_PrewMip,i.uv,5,prev_stride,1)
                                + GaussNxN(_MainTex,i.uv,5,curr_stride,1);
                return color;
            }
            ENDHLSL
        }

        // Output
        Pass
        {
            Blend One One
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

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float4 color = tex2D(_MainTex, i.uv);
                color.a = 1;
                

                return color;
            }
            ENDHLSL
        }


        // Filter And Donwsize And Blur
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

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _luminanceThreshole;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // sample the texture
                half4 color = tex2D(_MainTex,i.uv);

                float lum = dot(float3(0.2126, 0.7152, 0.0722), color.rgb);
                if(lum>_luminanceThreshole) 
                {
                    color.rgb = GaussNxN(_MainTex,i.uv,5,_MainTex_TexelSize.xy,1.0);
                    return color;
                }

                return half4(0,0,0,1);
            }
            ENDHLSL
        }
    }
}
