// Anillos de orbita.
//
// Sustituye la linea blanca plana por un anillo de energia: mezcla aditiva
// (asi el anillo se funde con el video de la camara en vez de taparlo) y un
// pulso de luz que recorre la circunferencia, lo que sugiere movimiento
// incluso cuando el atomo esta quieto.
//
// Pensado para LineRenderer, que entrega la coordenada u recorriendo la linea
// de 0 a 1.

Shader "Atomo/Orbita"
{
    Properties
    {
        _Color ("Color del anillo", Color) = (0.55, 0.8, 1, 1)
        _ColorPulso ("Color del pulso", Color) = (1, 1, 1, 1)
        _Base ("Brillo de base", Range(0, 2)) = 0.35
        _FuerzaPulso ("Fuerza del pulso", Range(0, 4)) = 1.4
        _AnchoPulso ("Estrechez del pulso", Range(1, 40)) = 12
        _Velocidad ("Velocidad del pulso", Range(-4, 4)) = 0.6
        _Repeticiones ("Pulsos por vuelta", Range(1, 8)) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        LOD 100
        Blend SrcAlpha One      // aditivo: suma luz, no tapa el fondo
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            fixed4 _ColorPulso;
            half _Base;
            half _FuerzaPulso;
            half _AnchoPulso;
            half _Velocidad;
            half _Repeticiones;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Recorrido a lo largo del anillo, repetido N veces
                half recorrido = frac(i.uv.x * _Repeticiones - _Time.y * _Velocidad);

                // Pulso estrecho y suave
                half pulso = pow(saturate(1.0 - abs(recorrido - 0.5) * 2.0), _AnchoPulso);

                half3 color = _Color.rgb * _Base + _ColorPulso.rgb * pulso * _FuerzaPulso;
                half alfa = (_Base + pulso * _FuerzaPulso) * _Color.a * i.color.a;

                return fixed4(color * i.color.rgb, saturate(alfa));
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}