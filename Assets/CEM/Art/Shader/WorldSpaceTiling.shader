Shader "Custom/WorldSpaceTriplanar_Clean"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _TextureScale ("Scale", Float) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 worldPos : TEXCOORD0; float3 worldNormal : TEXCOORD1; };

            sampler2D _MainTex;
            sampler2D _BumpMap;
            float _TextureScale;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 blend = pow(abs(i.worldNormal), 4);
                blend /= (blend.x + blend.y + blend.z);

                fixed4 col = tex2D(_MainTex, i.worldPos.zy * _TextureScale) * blend.x +
                             tex2D(_MainTex, i.worldPos.xz * _TextureScale) * blend.y +
                             tex2D(_MainTex, i.worldPos.xy * _TextureScale) * blend.z;

                return col;
            }
            ENDCG
        }
    }
}