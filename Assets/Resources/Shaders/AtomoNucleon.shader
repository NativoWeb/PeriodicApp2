// Protones y neutrones del nucleo.
//
// Iluminacion suave (half-lambert) mas un borde luminoso tipo Fresnel, que es
// lo que da volumen a una esfera vista en AR sobre video real. Funciona aunque
// la escena no tenga ninguna luz direccional, cosa habitual en escenas Vuforia:
// el termino _Ambiente garantiza que nunca se vea negro.
//
// Soporta GPU instancing, asi que las 238 esferas del uranio se dibujan en
// muy pocas draw calls.

Shader "Atomo/Nucleon"
{
    Properties
    {
        _Color ("Color base", Color) = (0.8, 0.2, 0.2, 1)
        _ColorBorde ("Color del borde", Color) = (1, 0.6, 0.5, 1)
        _PotenciaBorde ("Concentracion del borde", Range(0.5, 8)) = 2.5
        _FuerzaBorde ("Fuerza del borde", Range(0, 3)) = 0.9
        _Emision ("Emision propia", Range(0, 2)) = 0.15
        _Ambiente ("Luz ambiente minima", Range(0, 1)) = 0.35
        _Brillo ("Brillo especular", Range(0, 1)) = 0.35
        _Dureza ("Dureza del especular", Range(1, 128)) = 32
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
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
            #include "Lighting.cginc"

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
            fixed4 _ColorBorde;
            half _PotenciaBorde;
            half _FuerzaBorde;
            half _Emision;
            half _Ambiente;
            half _Brillo;
            half _Dureza;

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

                // Direccion de luz: la principal de la escena o, si no hay,
                // la propia camara, para que el nucleo nunca quede plano.
                float3 l = _WorldSpaceLightPos0.xyz;
                float longitudLuz = length(l);
                l = longitudLuz > 0.0001 ? l / longitudLuz : v;

                // Half-lambert: mas suave que el lambert clasico y evita
                // que la mitad de la esfera quede completamente apagada.
                half difusa = dot(n, l) * 0.5 + 0.5;
                half3 luz = _LightColor0.rgb * difusa + _Ambiente;

                // Especular Blinn-Phong
                float3 h = normalize(l + v);
                half especular = pow(saturate(dot(n, h)), _Dureza) * _Brillo;

                // Borde Fresnel: mas intenso cuanto mas de perfil se ve
                half fresnel = pow(saturate(1.0 - saturate(dot(n, v))), _PotenciaBorde);

                half3 color = _Color.rgb * luz
                            + _ColorBorde.rgb * fresnel * _FuerzaBorde
                            + _Color.rgb * _Emision
                            + especular;

                return fixed4(color, 1);
            }
            ENDCG
        }
    }

    Fallback "Diffuse"
}