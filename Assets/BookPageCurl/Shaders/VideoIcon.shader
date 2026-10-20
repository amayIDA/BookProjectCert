// A UI shader for line icons that play as video.
//  - The video's own alpha is used for the shape (the animated icons are transparent ProRes / WebM).
//  - _Recolor 1: the icon is painted in the RawImage colour (so one video serves a navy icon on the book and a white icon on the dark screen).
//  - Thin lines would vanish when a large icon video is drawn small. The shader measures how many video pixels fall on one screen pixel
//    and, when the stroke would be thinner than _MinStroke screen pixels, thickens it (dilates the shape) so a small icon stays crisp and readable.
//  - _BlackKey > 0: for a video WITHOUT alpha, black is removed (brightness becomes opacity), so it can still sit on any background.
//  - Works inside masks, RectMask2D and CanvasGroup fades like any other UI graphic.
Shader "BookUI/VideoIcon"
{
    Properties
    {
        [PerRendererData] _MainTex ("Video", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [Toggle] _Recolor ("Paint the icon in the tint colour", Float) = 1
        _SourceStroke ("Line width in the video, in video pixels", Float) = 8
        _MinStroke ("Thinnest line to show on screen, in pixels", Float) = 1.8
        _BlackKey ("Remove black (0 = off, for video without alpha)", Range(0, 0.6)) = 0

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
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

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
        Blend SrcAlpha OneMinusSrcAlpha
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
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _Recolor;
            float _BlackKey;
            float _SourceStroke;
            float _MinStroke;
            float4 _MainTex_TexelSize;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 video = tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd;
                float shape = video.a;

                // thicken a stroke that would be thinner than _MinStroke on screen
                float2 texel = IN.texcoord * _MainTex_TexelSize.zw;
                float perPixel = max(length(ddx(texel)), length(ddy(texel)));
                float radius = max(0.0, (_MinStroke * perPixel - _SourceStroke) * 0.5);
                if (radius > 0.01)
                {
                    float2 step1 = radius * _MainTex_TexelSize.xy;
                    float2 step2 = step1 * 0.5;
                    float a = shape;
                    a = max(a, tex2D(_MainTex, IN.texcoord + float2( step1.x, 0)).a);
                    a = max(a, tex2D(_MainTex, IN.texcoord + float2(-step1.x, 0)).a);
                    a = max(a, tex2D(_MainTex, IN.texcoord + float2(0,  step1.y)).a);
                    a = max(a, tex2D(_MainTex, IN.texcoord + float2(0, -step1.y)).a);
                    a = max(a, tex2D(_MainTex, IN.texcoord + step1 * 0.7071).a);
                    a = max(a, tex2D(_MainTex, IN.texcoord - step1 * 0.7071).a);
                    a = max(a, tex2D(_MainTex, IN.texcoord + float2(step1.x, -step1.y) * 0.7071).a);
                    a = max(a, tex2D(_MainTex, IN.texcoord + float2(-step1.x, step1.y) * 0.7071).a);
                    a = max(a, tex2D(_MainTex, IN.texcoord + float2( step2.x, 0)).a);
                    a = max(a, tex2D(_MainTex, IN.texcoord + float2(-step2.x, 0)).a);
                    a = max(a, tex2D(_MainTex, IN.texcoord + float2(0,  step2.y)).a);
                    a = max(a, tex2D(_MainTex, IN.texcoord + float2(0, -step2.y)).a);
                    shape = a;
                }

                // a video without alpha: brightness is the shape
                if (_BlackKey > 0.0001)
                {
                    float brightness = max(video.r, max(video.g, video.b));
                    shape = min(shape, smoothstep(0.0, _BlackKey, brightness));
                }

                fixed3 rgb = _Recolor > 0.5 ? IN.color.rgb : video.rgb * IN.color.rgb;
                fixed4 color = fixed4(rgb, shape * IN.color.a);

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
