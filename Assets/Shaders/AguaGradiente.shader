Shader "Custom/AguaGradiente"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _Color ("Sprite Color", Color) = (1,1,1,1)
        _ColorInicial ("Color inicial", Color) = (1,1,1,1)
        _ColorFinal ("Color final", Color) = (0.34,0.42,0.58,1)
        _XInicio ("Inicio X", Float) = 90
        _XFin ("Fin X", Float) = 210
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float worldX : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _ColorInicial;
            fixed4 _ColorFinal;
            float _XInicio;
            float _XFin;

            v2f vert(appdata_t input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.color = input.color * _Color;
                output.worldX = mul(unity_ObjectToWorld, input.vertex).x;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 textura = tex2D(_MainTex, input.texcoord);
                float rango = max(_XFin - _XInicio, 0.0001);
                float t = saturate((input.worldX - _XInicio) / rango);
                t = t * t * (3 - 2 * t);
                fixed4 tinte = lerp(_ColorInicial, _ColorFinal, t);
                return textura * input.color * tinte;
            }
            ENDCG
        }
    }
}
