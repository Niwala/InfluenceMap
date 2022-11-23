//Metaballs © 2022 by Sam's Backpack is licensed under CC BY-SA 4.0 (http://creativecommons.org/licenses/by-sa/4.0/)
//Source page of the project : https://niwala.itch.io/metaballs

Shader "Unlit/Metaballs_Upscaling"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }

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
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            Texture2D _BorderGradient;
            Texture2D _RenderData;
            SamplerState sampler_linear_clamp;
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

            float4 frag(v2f i) : SV_Target
            {
                float4 area = _RenderData.Sample(sampler_linear_clamp, i.uv);// tex2D(_RenderData, i.uv);//x = id, y = distance, zw = coords

                //Get area color & shape
	            float4 color = _Colors[(int)area.x];
	            float alpha = step(0.0001, area.y);
	            color.a *= alpha;
        
	            //Add borders
	            if (_BorderMode != 0)
	            {
		            float borders = _BorderGradient.Sample(sampler_linear_clamp, float2(pow(abs(area.y), _BorderPower), 0.5)).x;
		            color.a *= borders;
	            }

	            //Return result
	            return color;
            }
            ENDCG
        }
    }
}
