Shader "MotionStudio/UIGlow"
{
    // A soft, shaped light for UI. The shape comes from the alpha of the sprite (or its brightness),
    // the shape can be blurred, brightened and filled with a gradient that can scroll.
    // Works with Mask and RectMask2D like any UI graphic.
    Properties
    {
        [PerRendererData] _MainTex ("Shape (alpha)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Rect ("Rect (x, y, width, height)", Vector) = (0, 0, 1, 1)

        _ColorA ("Gradient color A", Color) = (0.35, 0.65, 1, 1)
        _ColorB ("Gradient color B", Color) = (1, 0.45, 0.85, 1)
        _Angle ("Gradient angle", Float) = 45
        _Offset ("Gradient offset", Float) = 0
        _Scroll ("Gradient scroll speed", Float) = 0
        _Repeat ("Repeat gradient (0/1)", Float) = 0

        _Blur ("Blur", Range(0, 1)) = 0.3
        _Brightness ("Brightness", Float) = 1.4
        _Softness ("Softness (shape curve)", Float) = 1
        _UseLuma ("Shape from brightness (0/1)", Float) = 0

        [Enum(Alpha, 10, Additive, 1)] _DstBlend ("Blend", Float) = 1

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
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
        Blend SrcAlpha [_DstBlend]
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

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
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            fixed4 _ColorA;
            fixed4 _ColorB;
            float _Angle;
            float _Offset;
            float _Scroll;
            float _Repeat;
            float _Blur;
            float _Brightness;
            float _Softness;
            float _UseLuma;
            float4 _Rect;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            float Shape(float2 uv)
            {
                fixed4 s = tex2D(_MainTex, uv);
                float a = lerp(s.a, dot(s.rgb, float3(0.299, 0.587, 0.114)) * s.a, _UseLuma);
                return a;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Blur: a golden angle spiral of taps with a gaussian weight. The spiral is turned a little
                // per pixel so the result is smooth instead of showing the sample pattern.
                float radius = _Blur * _Blur * 0.45 + _Blur * 0.02;
                float a = 0.0;
                float total = 0.0;
                float turn = frac(sin(dot(i.worldPosition.xy, float2(12.9898, 78.233))) * 43758.5453) * 6.2831853;
                [loop]
                for (int k = 0; k < 40; k++)
                {
                    float fk = (float(k) + 0.5) / 40.0;
                    float r = sqrt(fk);
                    float theta = float(k) * 2.3999632 + turn;
                    float w = exp(-r * r * 2.2);
                    a += Shape(i.uv + float2(cos(theta), sin(theta)) * r * radius) * w;
                    total += w;
                }

                a = total > 0.0 ? a / total : Shape(i.uv);
                a = pow(saturate(a), max(0.05, _Softness));

                // gradient across the rectangle of the graphic
                float rad = radians(_Angle);
                float2 dir = float2(cos(rad), sin(rad));
                float2 local = (i.worldPosition.xy - _Rect.xy) / max(_Rect.zw, float2(0.0001, 0.0001));
                float t = dot(local - 0.5, dir) + 0.5 + _Offset + _Time.y * _Scroll;
                t = _Repeat > 0.5 ? 1.0 - abs(frac(t * 0.5) * 2.0 - 1.0) : saturate(t);
                t = smoothstep(0.0, 1.0, t);
                fixed4 grad = lerp(_ColorA, _ColorB, t);

                fixed4 color;
                color.rgb = grad.rgb * i.color.rgb * _Brightness;
                color.a = a * i.color.a * grad.a;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }

    Fallback "UI/Default"
}
