Shader "Custom/SquareChecker"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _ColorA ("Base Tint", Color) = (1, 1, 1, 1)
        _ColorB ("Dark Cell", Color) = (0.82, 0.82, 0.82, 1)
        _ColorC ("Light Cell", Color) = (1.08, 1.08, 1.08, 1)
        _GridScale ("Squares Per Unit", Float) = 1.25
        _SquareSize ("Lego Square Size", Range(0, 1)) = 0.38
        _SquareOpacity ("Square Opacity", Range(0, 1)) = 1
        _BorderWidth ("Cell Border Width", Range(0, 0.2)) = 0.035
        _BorderStrength ("Cell Border Strength", Range(0, 1)) = 0.16
        _BevelStrength ("Lego Bevel Strength", Range(0, 1)) = 0.18
        _MainTex ("Overlay Texture", 2D) = "white" {}
        _TexBlend ("Texture Detail Strength", Range(0, 1)) = 0
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
            #pragma target 3.0
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
            float4 _Color;
            float4 _ColorA;
            float4 _ColorB;
            float4 _ColorC;
            float _GridScale;
            float _SquareSize;
            float _SquareOpacity;
            float _BorderWidth;
            float _BorderStrength;
            float _BevelStrength;
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
                float maxCenter = max(centered.x, centered.y);
                float halfSize = saturate(_SquareSize) * 0.5;
                float aa = max(fwidth(maxCenter), 0.001);
                float squareMask = 1.0 - smoothstep(halfSize - aa, halfSize + aa, maxCenter);
                float squareFactor = squareMask * saturate(_SquareOpacity);
                float parity = fmod(cellIndex.x + cellIndex.y, 2.0);
                fixed4 cellCol = lerp(_ColorB, _ColorC, parity) * _Color;
                fixed4 tileCol = lerp(_ColorA * _Color, cellCol, 0.45);

                float edgeDistance = min(min(cellUV.x, 1.0 - cellUV.x), min(cellUV.y, 1.0 - cellUV.y));
                float borderMask = 1.0 - smoothstep(max(_BorderWidth - aa, 0.0), _BorderWidth + aa, edgeDistance);
                fixed4 baseCol = tileCol * (1.0 - borderMask * saturate(_BorderStrength));

                float insetDistance = saturate((halfSize - maxCenter) / max(halfSize, 0.001));
                float bevel = lerp(1.0 - _BevelStrength, 1.0 + _BevelStrength, insetDistance);
                fixed4 studCol = cellCol * bevel;
                baseCol = lerp(baseCol, studCol, squareFactor);

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
