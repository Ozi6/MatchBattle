Shader "Custom/SelectionGlowShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GlowColor ("Glow Color", Color) = (1, 0.8, 0.3, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 1)) = 0.2
        _ShineSpeed ("Shine Speed", Range(0, 3)) = 1
        _ShineWidth ("Shine Width", Range(0.1, 1)) = 0.3
        _EdgeGlow ("Edge Glow", Range(0, 1)) = 0.3
        _PulseSpeed ("Pulse Speed", Range(0, 2)) = 0.8
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _GlowColor;
            float _GlowIntensity;
            float _ShineSpeed;
            float _ShineWidth;
            float _EdgeGlow;
            float _PulseSpeed;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float pulse = sin(_Time.y * _PulseSpeed) * 0.2 + 0.8;
                float2 center = float2(0.5, 0.5);
                float distFromCenter = distance(i.uv, center);
                float edgeFactor = smoothstep(0.3, 0.5, distFromCenter);
                float shine = sin((i.uv.x + _Time.y * _ShineSpeed) * 6.2831) * 0.5 + 0.5;
                shine = pow(shine, 5 / _ShineWidth);
                float4 glow = _GlowColor * _GlowIntensity * pulse;
                glow *= (1 + shine * 0.2 + edgeFactor * _EdgeGlow);
                glow.g = lerp(glow.g, glow.g * 0.7, shine * edgeFactor);
                fixed4 finalColor = lerp(col, col + glow, 0.4);
                finalColor.a = col.a;
                
                return finalColor;
            }
            ENDCG
        }
    }
}