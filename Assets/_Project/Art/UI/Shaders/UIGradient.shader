Shader "UI/GradientTexture"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _GradientTex ("Gradient", 2D) = "white" {}
        [HideInInspector] _GradientDir ("Gradient Direction", Vector) = (1,0,0,0)
        [HideInInspector] _GradientScale ("Gradient Scale", Float) = 1
        [HideInInspector] _GradientOffset ("Gradient Offset", Float) = 0
        [HideInInspector] _RectSize ("Rect Size", Vector) = (100,100,0,0)
        [HideInInspector] _RectPivot ("Rect Pivot", Vector) = (0.5,0.5,0,0)

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float2 localPos : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _GradientTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _GradientDir;
            float _GradientScale;
            float _GradientOffset;
            float4 _RectSize;
            float4 _RectPivot;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                o.localPos = v.vertex.xy;
                return o;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 baseSample = tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd;
                float2 rectSize = max(_RectSize.xy, 0.0001);
                float2 uv = (IN.localPos + rectSize * _RectPivot.xy) / rectSize;
                float2 dir = normalize(_GradientDir.xy);
                float t = dot(uv, dir) * _GradientScale + _GradientOffset;
                t = saturate(t);
                fixed4 gradCol = tex2D(_GradientTex, float2(t, 0.5));
                fixed4 color = gradCol;
                color.rgb *= IN.color.rgb;
                color.a *= baseSample.a * IN.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
