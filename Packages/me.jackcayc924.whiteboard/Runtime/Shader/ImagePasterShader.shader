Shader "Custom/ImagePasteShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ImageTex ("Image Texture", 2D) = "white" {}
        _ImagePosition ("Image Position", Vector) = (0,0,0,0)
        _ImageSize ("Image Size", Vector) = (1,1,0,0)
        _ImageRotation ("Image Rotation", Float) = 0.0
        _IsGhost ("Is Ghost", Float) = 0.0
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
            sampler2D _ImageTex;
            float4 _ImagePosition; // Position of the image
            float4 _ImageSize;     // Size of the image
            float _ImageRotation;   // Rotation of the image
            float _IsGhost;        // Ghost flag

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

                // Calculate the UV coordinates for the image
                float2 imagePos = _ImagePosition.xy; // Image position
                float2 imageSize = _ImageSize.xy;     // Image size

                // Adjust UVs based on position and size
                float2 uv = (i.uv - imagePos) / imageSize;

                // Apply rotation
                float cosTheta = cos(_ImageRotation);
                float sinTheta = sin(_ImageRotation);
                float2 rotatedUV = float2(
                    cosTheta * (uv.x - 0.5) - sinTheta * (uv.y - 0.5) + 0.5,
                    sinTheta * (uv.x - 0.5) + cosTheta * (uv.y - 0.5) + 0.5
                );

                // Check if the rotated UV is within bounds
                if (rotatedUV.x >= 0 && rotatedUV.x <= 1 && rotatedUV.y >= 0 && rotatedUV.y <= 1)
                {
                    fixed4 imageCol = tex2D(_ImageTex, rotatedUV);
                    if (_IsGhost > 0.5)
                    {
                        imageCol.a *= 0.5; // Make the ghost image semi-transparent
                    }
                    col = imageCol; // Blend the image color with the main texture
                }

                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
