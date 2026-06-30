Shader "Bliss/DiagramStacked"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BgColor ("Bg Color", Color) = (1,1,1,1)
        _MiddleLineColor ("Middle Line Color", Color) = (1,1,1,1)
        _MiddleLineWidth ("Middle Line Width", Float) = 0.005
        _LineWidth ("Line Width", Float) = 0.005
        [HideInInspector] _DataOffset ("DataOffset", Int) = 0
        [HideInInspector] _DataCount ("DataCount", Int) = 0
        [HideInInspector] _CategoryCount ("CategoryCount", Int) = 0
        [HideInInspector] _MaxValue ("MaxValue", Float) = 1
        [HideInInspector] _CategoryMask ("CategoryMask", Float) = 1


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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "../CGIncludes/SDFs.cginc"

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
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex; // we dont use this, but unitys ui library expects the shader to have a texture
            fixed4 _Color;
            fixed4 _BgColor;
            fixed4 _MiddleLineColor;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            half _MiddleLineWidth;
            half _LineWidth;
            uint _DataOffset;
            uint _DataCount;
            uint _CategoryCount;
            uint _CategoryMask;
            uint _MaxValue;
            half _DiagramData[64 /* max. 128 points */ * 8 /* max 8 categories */];
            half4 _CategoryColors[8 /* max 8 categories */];

            half4 samplePoint(int offset, float2 pixelScale, float2 screenSpaceUV, half4 color)
            {
                uint seekA = (offset + _DataOffset) % _DataCount;
                uint seekB = (seekA + 1)  % _DataCount;

                float a_x = float(offset) / _DataCount;
                float b_x = a_x + 1.0f / _DataCount;
                
                for (uint categoryIndex = 0; categoryIndex < _CategoryCount; ++categoryIndex)
                {
                    float a_y = _DiagramData[seekA * _CategoryCount + categoryIndex] / _MaxValue;
                    float b_y = _DiagramData[seekB * _CategoryCount + categoryIndex] / _MaxValue;
                    float2 a = float2(a_x, a_y);
                    float2 b = float2(b_x, b_y);
                    uint categoryEnable = (_CategoryMask >> categoryIndex) & 1;
                    float distance = sdSegment(screenSpaceUV, a * pixelScale, b * pixelScale);
                    half4 categoryColor = lerp(color, _CategoryColors[categoryIndex], categoryEnable);
                    color = lerp(categoryColor, color, step(0.0, distance - _LineWidth));
                }
                return color;
            }

            v2f vert (appdata_t v)
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


            fixed4 frag (v2f i) : SV_Target
            {
                float upper = 0;
                float lower = 0;
                half4 color = _BgColor;

                uint cur = uint(round(i.texcoord.x * float(_DataCount)));
                uint seek = (cur + _DataOffset) % _DataCount;
                
                for (uint categoryIndex = 0; categoryIndex < _CategoryCount; ++categoryIndex)
                {
                    float y = _DiagramData[seek * _CategoryCount + categoryIndex] / _MaxValue;
                    uint categoryEnable = (_CategoryMask >> categoryIndex) & 1;
                    half4 categoryColor = lerp(color, _CategoryColors[categoryIndex], categoryEnable);
                    upper += lerp(0, y, categoryEnable);

                    //Subtraction operate
                    float d1 = i.texcoord.y - lower;
                    float d2 = i.texcoord.y - upper;
                    float res = max(-d1, d2);

                    
                    color= lerp(categoryColor, color, step(0.0,  res));
                    lower = upper;
                }

                // this scaling only works if the object is flat and not rotated - but thats fine
                float2 pixelScale = float2(1 / ddx(i.texcoord.x), 1 / ddy(i.texcoord.y));
                float2 screenSpaceUV = i.texcoord * pixelScale;
                float midLineDistance = sdSegment(screenSpaceUV, float2(0., 0.5) * pixelScale, float2(1., 0.5) * pixelScale);
                color = lerp(_MiddleLineColor, color,step(0., midLineDistance - _MiddleLineWidth));

                color *= i.color;
                
#ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
#endif

#ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
#endif
                return color;
            }
            ENDCG
        }
    }
}
