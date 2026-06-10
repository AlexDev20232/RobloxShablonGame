Shader "Custom/DetailNormalTiled"
{
    Properties
    {
        _Color ("Base Color", Color) = (1, 1, 1, 1)
        _MainTex ("Base Texture", 2D) = "white" {}
        _BumpMap ("Detail Normal", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 5)) = 1
        _Roughness ("Roughness (0=Smooth 1=Rough)", Range(0, 1)) = 0.6
        _WorldScale ("World Tiling", Float) = 1
        _Mapping ("UV Plane (0=XY 1=XZ 2=YZ 3=Auto)", Range(0, 3)) = 3
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        float4 _MainTex_ST;
        sampler2D _BumpMap;
        float4 _BumpMap_ST;
        float4 _Color;
        float _NormalStrength;
        float _Roughness;
        float _WorldScale;
        float _Mapping;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
            INTERNAL_DATA
        };

        float2 GetWorldUV(float3 worldPos, float3 worldNormal)
        {
            if (_Mapping < 0.5)
            {
                return worldPos.xy;
            }

            if (_Mapping < 1.5)
            {
                return worldPos.xz;
            }

            if (_Mapping < 2.5)
            {
                return worldPos.zy;
            }

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

        float2 ApplyTiling(float2 worldUV, float4 textureST)
        {
            float2 tiling = textureST.xy;
            if (_Mapping > 2.5)
            {
                float uniformTiling = max(abs(tiling.x), abs(tiling.y));
                tiling = float2(uniformTiling, uniformTiling);
            }

            return worldUV * tiling + textureST.zw;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 worldUV = GetWorldUV(IN.worldPos, IN.worldNormal) * max(_WorldScale, 0.0001);
            float2 baseUV = ApplyTiling(worldUV, _MainTex_ST);
            float2 normalUV = ApplyTiling(worldUV, _BumpMap_ST);

            fixed4 baseTex = tex2D(_MainTex, baseUV);
            o.Albedo = baseTex.rgb * _Color.rgb;

            fixed3 normalSample = UnpackNormal(tex2D(_BumpMap, normalUV));
            normalSample.xy *= _NormalStrength;
            normalSample.z = sqrt(saturate(1.0 - dot(normalSample.xy, normalSample.xy)));
            o.Normal = normalSample;

            o.Smoothness = 1.0 - saturate(_Roughness);
            o.Alpha = baseTex.a * _Color.a;
        }
        ENDCG
    }
    FallBack "Standard"
}
