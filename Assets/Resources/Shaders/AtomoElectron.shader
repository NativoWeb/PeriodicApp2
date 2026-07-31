// Electrones.
//
// Un electron no es una bolita de plastico: se lee mejor como un punto de
// energia. El shader mantiene un nucleo solido de color y le suma un halo
// Fresnel muy intenso, de modo que el contorno brilla y la esfera parece
// emitir luz propia sin necesidad de post-procesado ni de particulas.
//
// Todo se resuelve en un solo pase opaco, asi que sigue siendo barato en movil
// incluso con los 118 electrones del oganeson.

Shader "Atomo/Electron"
{
    Properties
    {
        _Color ("Color del nucleo", Color) = (0.35, 0.55, 1, 1)
        _ColorHalo ("Color del halo", Color) = (0.6, 0.85, 1, 1)
        _PotenciaHalo ("Concentracion del halo", Range(0.5, 8)) = 1.8
        _FuerzaHalo ("Fuerza del halo", Range(0, 6)) = 2.2
        _Emision ("Emision propia", Range(0, 4)) = 1.2
        _VelocidadPulso ("Velocidad del pulso", Range(0, 10)) = 2.5
        _AmplitudPulso ("Amplitud del pulso", Range(0, 1)) = 0.18
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+1" }
        LOD 200

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normalMundo : TEXCOORD0;
                float3 dirVista : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            fixed4 _ColorHalo;
            half _PotenciaHalo;
            half _FuerzaHalo;
            half _Emision;
            half _VelocidadPulso;
            half _AmplitudPulso;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.normalMundo = UnityObjectToWorldNormal(v.normal);

                float3 posMundo = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.dirVista = normalize(_WorldSpaceCameraPos - posMundo);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float3 n = normalize(i.normalMundo);
                float3 v = normalize(i.dirVista);

                // Latido lento: da sensacion de energia sin distraer
                half pulso = 1.0 + sin(_Time.y * _VelocidadPulso) * _AmplitudPulso;

                half fresnel = pow(saturate(1.0 - saturate(dot(n, v))), _PotenciaHalo);

                half3 color = _Color.rgb * _Emision * pulso
                            + _ColorHalo.rgb * fresnel * _FuerzaHalo * pulso;

                return fixed4(color, 1);
            }
            ENDCG
        }
    }

    Fallback "Unlit/Color"
}