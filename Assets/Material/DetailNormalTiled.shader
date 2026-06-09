Shader "Custom/DetailNormalTiled"
{
    Properties
    {
        _Color ("Base Color", Color) = (1, 1, 1, 1)
        _MainTex ("Base Texture", 2D) = "white" {}
        _BumpMap ("Detail Normal", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 5)) = 1
        _Roughness ("Roughness (0=Smooth 1=Rough)", Range(0, 1)) = 0.6
        _Mapping ("UV Plane (0=XY 1=XZ 2=YZ)", Range(0, 2)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        float4 _BumpMap_ST;
        float4 _Color;
        float _NormalStrength;
        float _Roughness;
        float _Mapping;

        struct Input
        {
            float2 uv_MainTex;
            float2 detailUV;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);

            float3 axisX = mul(unity_ObjectToWorld, float4(1, 0, 0, 0)).xyz;
            float3 axisY = mul(unity_ObjectToWorld, float4(0, 1, 0, 0)).xyz;
            float3 axisZ = mul(unity_ObjectToWorld, float4(0, 0, 1, 0)).xyz;
            float3 scale = float3(length(axisX), length(axisY), length(axisZ));

            float2 scaleUV;
            if (_Mapping < 0.5)
            {
                scaleUV = float2(scale.x, scale.y);
            }
            else if (_Mapping < 1.5)
            {
                scaleUV = float2(scale.x, scale.z);
            }
            else
            {
                scaleUV = float2(scale.y, scale.z);
            }

            float2 detailUV = v.texcoord.xy * scaleUV;
            o.detailUV = detailUV * _BumpMap_ST.xy + _BumpMap_ST.zw;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 baseTex = tex2D(_MainTex, IN.uv_MainTex);
            o.Albedo = baseTex.rgb * _Color.rgb;

            fixed3 normalSample = UnpackNormal(tex2D(_BumpMap, IN.detailUV));
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
