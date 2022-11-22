Shader "Unlit/Metaballs_Upscaling"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always

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
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _BorderGradient;
            sampler2D _RenderData;
            StructuredBuffer<float4> _Colors;
            int _BorderMode;
            float _BorderPower;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 area = tex2D(_RenderData, i.uv);//x = id, y = distance, zw = coords

                //Get area color & shape
	            float4 color = _Colors[(int)area.x];
	            float alpha = step(0.0001, area.y);
	            color.a *= alpha;
        
	            //Add borders
	            if (_BorderMode != 0)
	            {
		            float borders = tex2D(_BorderGradient, pow(abs(area.y), _BorderPower)).x;
		            color.a *= borders;
	            }

	            //Return result
	            return color;
            }
            ENDCG
        }
    }
}
