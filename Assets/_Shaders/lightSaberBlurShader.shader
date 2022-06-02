Shader "Shimmy/lightSaberBlurShader"
{
	Properties
	{
		_Color ("Color", Color) = (1,0,1,1)
		_MainTex ("Texture", 2D) = "white" {}
		_Boost ("Boost", Range (1, 100)) = 1.5
		_Speed ("Speed", Float) = 0
		[Toggle] _SoftTail ("SoftTail", Float) = 0
        _SoftLength ("SoftLength", Range (.1,1)) = 1.0
		[Toggle] _Dither ("Dither", Float) = 0

	}
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		LOD 100
		Cull off
        Lighting Off

        Blend SrcAlpha OneMinusSrcAlpha
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
				float4 color : COLOR;
				//float3 normal: NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _Color;//-2 to 2
			half _Boost;//half 16 bits
			fixed _SoftTail;//0 or 1
			fixed _Dither;
			half _Speed;
            fixed _SoftLength;

			/*float Random (int min, int max, float2 uv)    //Pass this function a minimum and maximum value, as well as your texture UV
			 {
			     if (min > max)
			         return 1;        //If the minimum is greater than the maximum, return a default value
			 
			     float cap = max - min;    //Subtract the minimum from the maximum
			     //float rand = tex2D (_MainTex, uv + _Time.x).r * cap + min;    //Make the texture UV random (add time) and multiply noise texture value by the cap, then add the minimum back on to keep between min and max 
			     float rand = (sin(uv.x * 2000) + 1)/2;
			     rand *= (sin(uv.y * 2000) + 1)/2;
			     return rand;    //Return this value
			 }*/

			fixed Grid (int resolution,fixed2 uv,float time)    //Pass this function a minimum and maximum value, as well as your texture UV
			 {
			    float grid = (sin((uv.x - uv.y) * resolution + time) + 9)/10;
			    grid *= (sin((uv.y + uv.x) * resolution + time) + 9)/10;		     
			    return 1-grid;    //Return this value
			 }

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = _Color * _Boost;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = i.color;// _Color * _Boost;
				col.a = tex2D(_MainTex, i.uv).r;
				fixed dither=0;
				if (_SoftTail==1){
					if (_Dither==1)
					{
						//dither = max(pow(col.a * (1 - (i.uv.x / _SoftLength)),1),0);//blade to trail end
						dither = saturate(col.a * (1 - (i.uv.x / _SoftLength)));//blade to trail end
						dither *= saturate(i.uv.y * 10);//top grad
						dither *= saturate((1-i.uv.y) * 10);//bot grad

						if (dither >= Grid(1000,i.uv,_Time[1]* 10 * _Speed) )
							col.a = 1;
						else
							col.a =0;
					}
					else
					col.a *= (1 - i.uv.x) * 1.5 - .5;//

				}

				return col;
			}
			ENDCG
		}


	}
}
