Shader "Shimmy/lightSaberShader"
{
	Properties
	{
		_Color ("Color", Color) = (1,0,1,1)
		//_MainTex ("Texture", 2D) = "white" {}
		_Boost ("Boost", Range (1, 100)) = 1.5
		[Toggle] _Displacement ("Displacement", Float) = 0
        [Toggle] _NormalDisplacement ("Normal Displacement", Float) = 1
		_Extrude ("Extrude", Range(-.25,.25)) = 0.005
		_Waviness ("Waviness", Range (0,200)) = 0
	}
	SubShader
	{
		LOD 100
		Cull off
		Lighting Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				//float2 uv : TEXCOORD0;

				float3 normal: NORMAL;
			};

			float _Displacement;
            float _NormalDisplacement;
            float4 _DisplacementScale;
			fixed _Extrude;
			fixed _Waviness;

			struct v2f
			{
				//float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
			};

			//sampler2D _MainTex;
			//float4 _MainTex_ST;
			fixed4 _Color;
			float _Boost;

			v2f vert (appdata v)
			{
				v2f o;
				float3 displaceVector;
				    displaceVector = v.normal;
				    displaceVector.x *= 10;
				    displaceVector.y *= .00001;//actually z
					displaceVector.z *= 10;

				fixed _WaveOffset = _Time[1];
				v.vertex.xyz += displaceVector * (_Displacement * _Extrude * ((sin((v.vertex.x + _WaveOffset) * _Waviness) + sin((v.vertex.y - _WaveOffset)* _Waviness) + sin((v.vertex.z + _WaveOffset * .5) * _Waviness) - 1.5) * 2));
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = _Color * _Boost;
				//o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
			//	fixed4 col = tex2D(_MainTex, i.uv) * _Color;
			//	return col * _Boost;
				return i.color;// _Color * _Boost;//calculations moved to vert shader
			}
			ENDCG
		}


	}
}
