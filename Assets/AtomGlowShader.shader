Shader "Custom/AtomGlowShader"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (0.3, 0.6, 1, 0.5)
        _GlowColor ("Glow Color", Color) = (0.2, 0.4, 0.8, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 10)) = 2
        _GlowFalloff ("Glow Falloff", Range(0.1, 5)) = 2
        _PulseSpeed ("Pulse Speed", Range(0, 2)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha

        CGPROGRAM
        #pragma surface surf Standard alpha:fade
        #pragma target 3.0

        struct Input
        {
            float3 viewDir;
            float3 worldNormal;
            float3 worldPos;
        };

        fixed4 _MainColor;
        fixed4 _GlowColor;
        float _GlowIntensity;
        float _GlowFalloff;
        float _PulseSpeed;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Efecto Fresnel (brillo en los bordes)
            float fresnel = pow(1.0 - saturate(dot(IN.viewDir, IN.worldNormal)), _GlowFalloff);
            
            // Efecto de pulsación
            float pulse = (sin(_Time.y * _PulseSpeed) * 0.5 + 0.5) * 0.3 + 0.7;
            
            // Color base con transparencia
            o.Albedo = _MainColor.rgb;
            o.Alpha = _MainColor.a;
            
            // Emisión con efecto glow
            o.Emission = _GlowColor * _GlowIntensity * fresnel * pulse * 3; // Multiplicador aumentado
            
            // Ajustes de material
            o.Metallic = 0.3;
            o.Smoothness = 0.8;
        }
        ENDCG
    }
    FallBack "Transparent"
}