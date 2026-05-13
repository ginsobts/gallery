Shader "Hidden/GalleryFilter"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _Saturation ("Saturation", Float) = 1
        _Brightness ("Brightness", Float) = 0
        _Contrast ("Contrast", Float) = 1
        _Overlay ("Overlay", Color) = (0,0,0,0)
        _Vignette ("Vignette", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off

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
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            fixed4 _Tint;
            half _Saturation;
            half _Brightness;
            half _Contrast;
            fixed4 _Overlay;
            half _Vignette;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                col.rgb = col.rgb * _Tint.rgb;
                col.rgb = col.rgb + _Brightness;
                col.rgb = (col.rgb - 0.5) * _Contrast + 0.5;

                half grey = dot(col.rgb, half3(0.299, 0.587, 0.114));
                col.rgb = lerp(half3(grey, grey, grey), col.rgb, _Saturation);

                col.rgb = lerp(col.rgb, _Overlay.rgb, _Overlay.a);

                half2 vc = i.uv - 0.5;
                half vf = 1.0 - dot(vc, vc) * 4.0 * _Vignette;
                col.rgb = col.rgb * saturate(lerp(1.0, vf, step(0.001, _Vignette)));

                return col;
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
