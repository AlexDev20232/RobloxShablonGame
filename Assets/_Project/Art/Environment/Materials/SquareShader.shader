Shader "Custom/SquareChecker"
{
    Properties
    {
        _ColorA ("Base Green", Color) = (0.2, 0.8, 0.2, 1)
        _ColorB ("Dark Square", Color) = (0.0, 0.4, 0.0, 1)
        _ColorC ("Light Square", Color) = (0.35, 0.9, 0.35, 1)
        _GridScale ("Squares Per Unit", Float) = 8
        _SquareSize ("Square Size", Range(0, 1)) = 0.7
        _SquareOpacity ("Square Opacity", Range(0, 1)) = 1
        _MainTex ("Overlay Texture", 2D) = "white" {}
        _TexBlend ("Detail Strength", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ColorA;
            float4 _ColorB;
            float4 _ColorC;
            float _GridScale;
            float _SquareSize;
            float _SquareOpacity;
            float _TexBlend;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            float2 GetWorldUV(float3 worldPos, float3 worldNormal)
            {
                float3 n = abs(normalize(worldNormal));
                if (n.y >= n.x && n.y >= n.z)
                {
                    return worldPos.xz;
                }

                if (n.x >= n.z)
                {
                    return worldPos.zy;
                }

                return worldPos.xy;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 worldUV = GetWorldUV(i.worldPos, i.worldNormal);
                float2 scaledUV = worldUV * max(_GridScale, 0.0001);
                float2 cellIndex = floor(scaledUV);
                float2 cellUV = frac(scaledUV);
                float2 centered = abs(cellUV - 0.5);
                float halfSize = saturate(_SquareSize) * 0.5;
                float squareMask = step(centered.x, halfSize) * step(centered.y, halfSize);
                float squareFactor = squareMask * saturate(_SquareOpacity);
                float parity = fmod(cellIndex.x + cellIndex.y, 2.0);
                fixed4 squareCol = lerp(_ColorB, _ColorC, parity);
                fixed4 baseCol = lerp(_ColorA, squareCol, squareFactor);
                float2 overlayUV = worldUV * _MainTex_ST.xy + _MainTex_ST.zw;
                fixed4 texCol = tex2D(_MainTex, overlayUV);
                float texGray = dot(texCol.rgb, float3(0.299, 0.587, 0.114));
                float detail = (texGray - 0.5) * 2.0;
                float detailFactor = max(1.0 + detail * saturate(_TexBlend), 0.0);
                fixed4 finalCol = baseCol * detailFactor;
                return finalCol;
            }
            ENDCG
        }
    }
}
