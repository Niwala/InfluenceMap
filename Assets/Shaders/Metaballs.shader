Shader "Hidden/Metaballs"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        //Draw metaball
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 p : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 p : SV_POSITION; };
            v2f vert(appdata v) { v2f o; o.p = UnityObjectToClipPos(v.p); o.uv = v.uv; return o; }


            struct Emitter
            {
                float3 position;
                float radius;
                float4 channels;
            };

            sampler2D _MainTex;
            float _Metaball_Smooth;
            int _EmitterCount;
            StructuredBuffer<Emitter> _Emitters;

            //smin from Inigo Quilez adapted for a float4
            float4 smin(float4 a, float4 b, float k)
            {
                float4 res = exp2(-k * a) + exp2(-k * b);
                return -log2(res) / k;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 col = 0.0;
                float2 pos = i.uv * 2.0 - 1.0;
                for (int j = 0; j < _EmitterCount; j++)
                {
                    Emitter emitter = _Emitters[j];

                    float dist = distance(pos, emitter.position.xz);
                    float factor = dist - emitter.radius;
                    col = lerp(col, smin(col, factor, _Metaball_Smooth), emitter.channels);
                }
                return col;
            }
            ENDCG
        }


        //Select main color
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 p : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 p : SV_POSITION; };
            v2f vert(appdata v) { v2f o; o.p = UnityObjectToClipPos(v.p); o.uv = v.uv; return o; }


            sampler2D _MainTex;
            float _Metaball_Smooth;

            float4 frag(v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);
                float minValue = min(min(col.r, col.g), min(col.b, col.a)) + 0.01;
                float alpha = step(minValue, -(1.0 / _Metaball_Smooth));

                if (col.r < minValue)
                    return float4(1, 0, 0, 0) * alpha;
                else if (col.g < minValue)
                    return float4(0, 1, 0, 0) * alpha;
                else if (col.b < minValue)
                    return float4(0, 0, 1, 0) * alpha;
                else if (col.a < minValue)
                    return float4(0, 0, 0, 1) * alpha;
                return float4(0, 0, 0, 0);
            }
            ENDCG
        }

        //Blur
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 p : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 p : SV_POSITION; };
            v2f vert(appdata v) { v2f o; o.p = UnityObjectToClipPos(v.p); o.uv = v.uv; return o; }


            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            int _BlurItterations;
            float2 _BlurRange;

            float4 frag(v2f i) : SV_Target
            {
                float4 col = float4(0.0, 0.0, 0.0, 0.0);
                float sum = 0.0;

                for (int j = 0; j < _BlurItterations; j++)
                {
                    float t = ((float)(_BlurItterations - j) / _BlurItterations);
                    col += tex2D(_MainTex, i.uv + _BlurRange * j * _MainTex_TexelSize.xy) * t;
                    col += tex2D(_MainTex, i.uv - _BlurRange * j * _MainTex_TexelSize.xy) * t;
                    sum += t * 2;
                }
                return col / sum;
            }
            ENDCG
        }

            
        //Isolate Edges
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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float _EdgeThickness;

            float4 _RedChannelColor;
            float4 _GreenChannelColor;
            float4 _BlueChannelColor;
            float4 _AlphaChannelColor;

            float4 frag(v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);
                float4 gradient = (1.0 - col) * smoothstep(0.5, 0.55, col);
                float4 edge = smoothstep(0.4 - _EdgeThickness, 0.45 - _EdgeThickness, gradient);

                float4 result = gradient + edge;

                return _RedChannelColor * result.r +
                    _GreenChannelColor * result.g +
                    _BlueChannelColor * result.b +
                    _AlphaChannelColor * result.a;
            }
            ENDCG
        }
    }
}
