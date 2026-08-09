Shader "Custom/ExactColorSwap"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap("Pixel snap", Float) = 0
        
        [Header(Color Slots 1 to 5)]
        _OriginalColor1("Original 1", Color) = (1,1,1,1)
        _TargetColor1("Target 1", Color) = (1,1,1,1)
        _OriginalColor2("Original 2", Color) = (1,1,1,1)
        _TargetColor2("Target 2", Color) = (1,1,1,1)
        _OriginalColor3("Original 3", Color) = (1,1,1,1)
        _TargetColor3("Target 3", Color) = (1,1,1,1)
        _OriginalColor4("Original 4", Color) = (1,1,1,1)
        _TargetColor4("Target 4", Color) = (1,1,1,1)
        _OriginalColor5("Original 5", Color) = (1,1,1,1)
        _TargetColor5("Target 5", Color) = (1,1,1,1)

        [Header(Color Slots 6 to 10)]
        _OriginalColor6("Original 6", Color) = (1,1,1,1)
        _TargetColor6("Target 6", Color) = (1,1,1,1)
        _OriginalColor7("Original 7", Color) = (1,1,1,1)
        _TargetColor7("Target 7", Color) = (1,1,1,1)
        _OriginalColor8("Original 8", Color) = (1,1,1,1)
        _TargetColor8("Target 8", Color) = (1,1,1,1)
        _OriginalColor9("Original 9", Color) = (1,1,1,1)
        _TargetColor9("Target 9", Color) = (1,1,1,1)
        _OriginalColor10("Original 10", Color) = (1,1,1,1)
        _TargetColor10("Target 10", Color) = (1,1,1,1)
        
        [Header(Thresholds Settings)]
        _RGBTolerance("RGB Precision Matcher", Range(0, 0.3)) = 0.05
    }

    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent" 
            "RenderType" = "Transparent" 
            "IgnoreProjector" = "True" 
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0 // Upgraded to Shader Model 3.0 to support array loop unrolling smoothly!
            #pragma multi_compile_local _ PIXELSNAP_ON
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 uv       : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _RGBTolerance;

            // Declare our 10 individual Original/Target properties uniform registers
            fixed4 _OriginalColor1;  fixed4 _TargetColor1;
            fixed4 _OriginalColor2;  fixed4 _TargetColor2;
            fixed4 _OriginalColor3;  fixed4 _TargetColor3;
            fixed4 _OriginalColor4;  fixed4 _TargetColor4;
            fixed4 _OriginalColor5;  fixed4 _TargetColor5;
            fixed4 _OriginalColor6;  fixed4 _TargetColor6;
            fixed4 _OriginalColor7;  fixed4 _TargetColor7;
            fixed4 _OriginalColor8;  fixed4 _TargetColor8;
            fixed4 _OriginalColor9;  fixed4 _TargetColor9;
            fixed4 _OriginalColor10; fixed4 _TargetColor10;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;

                #ifdef PIXELSNAP_ON
                o.vertex = UnityPixelSnap(o.vertex);
                #endif

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                if (col.a <= 0.005)
                {
                    return fixed4(0, 0, 0, 0);
                }

                // Pack our uniform variables into compile-time array memory buffers 
                // This lets us write a ultra-clean processing loop instead of a massive if-else branch mess!
                float3 originals[10] = {
                    _OriginalColor1.rgb,  _OriginalColor2.rgb,  _OriginalColor3.rgb,  _OriginalColor4.rgb,  _OriginalColor5.rgb,
                    _OriginalColor6.rgb,  _OriginalColor7.rgb,  _OriginalColor8.rgb,  _OriginalColor9.rgb,  _OriginalColor10.rgb
                };

                float3 targets[10] = {
                    _TargetColor1.rgb,  _TargetColor2.rgb,  _TargetColor3.rgb,  _TargetColor4.rgb,  _TargetColor5.rgb,
                    _TargetColor6.rgb,  _TargetColor7.rgb,  _TargetColor8.rgb,  _TargetColor9.rgb,  _TargetColor10.rgb
                };

                float3 swappedColor = col.rgb;

                // Loop through all 10 slots. The compiler optimizes this into lightning-fast GPU operations.
                for (int idx = 0; idx < 10; idx++)
                {
                    if (distance(col.rgb, originals[idx]) < _RGBTolerance)
                    {
                        swappedColor = targets[idx];
                        break; // Match confirmed, step out of processing loop immediately!
                    }
                }

                return fixed4(swappedColor, col.a) * i.color;
            }
            ENDCG
        }
    }
}