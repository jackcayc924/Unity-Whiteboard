Shader "Custom/DrawShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DrawColor ("Draw Color", Color) = (1,1,1,1)
        _DrawPosition ("Draw Position", Vector) = (0,0,0,0)
        _PenSize ("Pen Size", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
            float4 _DrawColor;
            float4 _DrawPosition;
            float _PenSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                float2 drawPos = _DrawPosition.xy;
                float2 diff = i.uv - drawPos;
                float distSq = dot(diff, diff);

                if (distSq < _PenSize * _PenSize)
                {
                    col = _DrawColor;
                }

                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
