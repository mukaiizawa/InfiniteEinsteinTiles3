Shader "TitleShimmer"
{
    Properties
    {
        _Progress ("Progress", Range(0, 1)) = 0
        _Width ("Width", Float) = 0.25
        _Sharpness ("Sharpness", Float) = 2.5
        _Brightness ("Brightness", Float) = 1.0
        _Saturation ("Saturation", Float) = 0.65

        // Required by Unity UI stencil system
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
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
        Blend SrcAlpha One   // Additive: only adds brightness, never obscures tiles
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos          : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 worldPos     : TEXCOORD1;
            };

            float _Progress;
            float _Width;
            float _Sharpness;
            float _Brightness;
            float _Saturation;
            float4 _ClipRect;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos      = UnityObjectToClipPos(v.vertex);
                o.uv       = v.uv;
                o.worldPos = v.vertex;
                return o;
            }

            // Hue-saturation-value → RGB
            float3 hsv2rgb(float h, float s, float v)
            {
                float4 K = float4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
                float3 p = abs(frac(h.xxx + K.xyz) * 6.0 - K.www);
                return v * lerp(K.xxx, saturate(p - K.xxx), s);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Clip to rect mask (needed for proper UI masking)
                if (!UnityGet2DClipping(i.worldPos.xy, _ClipRect))
                    discard;

                float2 uv = i.uv;

                // Diagonal projection — mimics a CD's specular highlight angle
                // The diagonal goes from bottom-left to top-right at ~40°
                float proj = uv.x * 0.72 + uv.y * 0.60;
                // proj range ≈ [0, 1.32]; normalize to [0,1]
                float projN = proj / 1.32;

                // Sweep: progress goes 0→1, band center travels from -width to 1+width
                float center = _Progress;
                float dist = abs(projN - center);

                // Smooth bell-shaped falloff
                float falloff = saturate(1.0 - dist / _Width);
                falloff = pow(falloff, _Sharpness);

                // Iridescent hue: shifts across the band width + drifts with progress
                float hue = frac(projN * 1.8 - _Progress * 0.6);
                float3 color = hsv2rgb(hue, _Saturation, _Brightness);

                // Add a white core at the peak of the band
                float core = pow(saturate(1.0 - dist / (_Width * 0.25)), 2.0);
                color = lerp(color, float3(1.5, 1.5, 1.5), core * 0.5);

                return fixed4(color, falloff * 0.55);
            }
            ENDCG
        }
    }
}
