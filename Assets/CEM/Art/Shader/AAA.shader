Shader "Custom/WorldSpace_Wall_Final"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _TextureScale ("Texture Scale", Float) = 0.5
        _BlendSharpness ("Blend Sharpness", Range(1, 20)) = 4.0
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Glossiness ("Smoothness", Range(0,1)) = 0.2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;

        float _TextureScale;
        float _BlendSharpness;
        half _Metallic;
        half _Glossiness;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
            INTERNAL_DATA // Derleyicinin istediği eksik parça burası
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Dünya normallerini güvenli bir şekilde almak için WorldNormalVector kullanıyoruz
            float3 worldNormalVec = WorldNormalVector(IN, float3(0, 0, 1));
            
            // Keskinlik ayarlı ağırlık hesaplaması
            float3 blendWeights = pow(abs(worldNormalVec), _BlendSharpness);
            blendWeights /= (blendWeights.x + blendWeights.y + blendWeights.z);

            // Dünya koordinatlarından UV üretimi
            float2 xUV = IN.worldPos.zy * _TextureScale;
            float2 yUV = IN.worldPos.xz * _TextureScale;
            float2 zUV = IN.worldPos.xy * _TextureScale;

            // Albedo (Renk) Örneklemesi
            float3 colX = tex2D(_MainTex, xUV).rgb;
            float3 colY = tex2D(_MainTex, yUV).rgb;
            float3 colZ = tex2D(_MainTex, zUV).rgb;
            float3 finalAlbedo = colX * blendWeights.x + colY * blendWeights.y + colZ * blendWeights.z;
            
            // Normal Map Örneklemesi
            float3 normX = UnpackNormal(tex2D(_BumpMap, xUV));
            float3 normY = UnpackNormal(tex2D(_BumpMap, yUV));
            float3 normZ = UnpackNormal(tex2D(_BumpMap, zUV));

            float3 finalNormal = normalize(normX * blendWeights.x + normY * blendWeights.y + normZ * blendWeights.z);

            // Çıktıları Standart Aydınlatmaya Atama
            o.Albedo = finalAlbedo;
            o.Normal = finalNormal;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}