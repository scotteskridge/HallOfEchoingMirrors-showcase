// Candle light for the planning screen's map: a warm glow round each candle, added to what is behind it,
// and a gentle darkening away from the candles so the flicker reads as light and swaying shadow.
// A UI shader (for an Image on the canvas), so it respects masks and canvas fades like the default one.
// Drawn premultiplied: result = light + what is behind * (1 - darkness). The flicker itself is worked
// out in CandleFlicker (so it can be tuned in the Inspector) and arrives as the _Candle numbers.
Shader "Hall of Echoing Mirrors/UI/Candle Light"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

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
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

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

            fixed4 _Color;
            float4 _ClipRect;

            // Per candle: xy = where it is (0-1 across the image), z = how far its light reaches (in image heights),
            // w = how bright it is now. Colours are the warm light of each.
            #define MAX_CANDLES 4
            float4 _Candle[MAX_CANDLES];
            float4 _CandleColour[MAX_CANDLES];
            float _CandleCount;
            float _Aspect;   // image width / height, so the glow is round
            float _Darkness; // how dark it gets far from every candle (0-1)

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = float2(i.uv.x * _Aspect, i.uv.y);
                float3 lit = 0;
                [unroll]
                for (int k = 0; k < MAX_CANDLES; k++)
                {
                    float2 c = float2(_Candle[k].x * _Aspect, _Candle[k].y);
                    float d = length(p - c) / max(_Candle[k].z, 0.0001);
                    float falloff = exp(-d * d * 2.5) + 0.25 * exp(-d * 1.2); // a bright core and a wide soft skirt
                    lit += (k < _CandleCount ? 1.0 : 0.0) * _CandleColour[k].rgb * falloff * _Candle[k].w;
                }
                float light = saturate(dot(lit, float3(0.4, 0.4, 0.4)));
                float dark = _Darkness * (1.0 - light);

                fixed4 col = fixed4(lit, dark) * i.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                col *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                return col;
            }
            ENDCG
        }
    }
}
