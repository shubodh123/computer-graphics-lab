Shader "CS4104/VelvetSheenShader"
{
    Properties
    {
        _Color ("Base Velvet Color", Color) = (0.6, 0.1, 0.1, 1.0)
        _SheenColor ("Sheen Fuzz Color", Color) = (1.0, 0.7, 0.7, 1.0)
        _SheenRoughness ("Sheen Roughness (Alpha)", Range(0.05, 1.0)) = 0.35
        _SheenIntensity ("Sheen Intensity", Range(0.0, 5.0)) = 2.5
        _SubsurfaceColor ("Subsurface Scatter Tint", Color) = (0.8, 0.15, 0.05, 1.0)
        _SubsurfaceAmount ("Subsurface Amount", Range(0.0, 1.0)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "LightMode"="ForwardBase" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            fixed4 _Color;
            fixed4 _SheenColor;
            float _SheenRoughness;
            float _SheenIntensity;
            fixed4 _SubsurfaceColor;
            float _SubsurfaceAmount;

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
                float3 N = normalize(i.worldNormal);
                float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float3 H = normalize(L + V);

                // Dot products
                float NdotL = max(0.0, dot(N, L));
                float NdotV = max(0.001, dot(N, V));
                float NdotH = max(0.001, dot(N, H));

                // 1. Diffuse Term (Energy-conserving Lambertian + Subsurface organic wrap)
                float diffuseWrap = (dot(N, L) + 0.3) / 1.3;
                float3 diffuse = _Color.rgb * max(0.0, diffuseWrap) * _LightColor0.rgb;

                // 2. Subsurface scattering imitation for translucent fabric fibers
                float3 sss = _SubsurfaceColor.rgb * (1.0 - NdotL) * _SubsurfaceAmount * _LightColor0.rgb;

                // 3. Charlie Sheen BRDF Grazing Fuzz:
                // D_Charlie(h) ~ sin(theta_h)^(1/alpha) / (2*pi)
                // Grazing edge halo term: (1 - NdotV)^(1/alpha)
                float sinThetaH = sqrt(max(0.0, 1.0 - NdotH * NdotH));
                float invAlpha = 1.0 / max(0.01, _SheenRoughness);
                float charlieD = pow(sinThetaH, invAlpha);

                // Grazing Fresnel rim factor (fibers scatter strongly at grazing angles)
                float grazingRim = pow(1.0 - NdotV, 3.0);
                float3 sheen = _SheenColor.rgb * (charlieD + grazingRim * 1.5) * _SheenIntensity * NdotL * _LightColor0.rgb;

                // 4. Ambient Environmental Radiance (simulating Spherical Harmonics)
                float3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb * _Color.rgb;

                // Final Combined Radiance
                float3 finalColor = ambient + diffuse + sss + sheen;
                return fixed4(finalColor, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
