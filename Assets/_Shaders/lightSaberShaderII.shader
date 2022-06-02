Shader "Shimmy/lightSaberShaderII"
{
	Properties
	{
		_Color ("Color", Color) = (1,0,1,1)
		_MainTex ("Texture", 2D) = "white" {}
		_OutlineColor ("Outline Color", Color) = (0,0,1,1)
		_OutlineWidth ("Outline Width", Range (.002, 1)) = .005
		_Boost ("Boost", Range (1, 100)) = 1.5
		[Toggle] _Outline ("Outline", Float) = 0
		[Toggle] _Displacement ("Displacement", Float) = 0
        [Toggle] _NormalDisplacement ("Normal Displacement", Float) = 1
        _DisplacementScale ("Displacement Scale", Float) = 1
		_Extrude ("Extrude", Range(-.25,.25)) = 0.005
		_Waviness ("Waviness", Range (0,200)) = 0
		_TimeMultiplier ("Time Multiplier", Float) = 1
	}
	SubShader
	{
		//Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		LOD 100
		Cull off
		Lighting Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			//#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;

				float3 normal: NORMAL;
			};

			float _Displacement;
            float _NormalDisplacement;
            float _DisplacementScale;
			fixed _Extrude;
			fixed _Waviness;

			struct v2f
			{
				float2 uv : TEXCOORD0;
				//UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _Color;
			float _Boost;
			float _TimeMultiplier;

			v2f vert (appdata v)
			{
				v2f o;

				//UNITY_TRANSFER_FOG(o,o.vertex);
				float3 displaceVector;
                //if (_NormalDisplacement > 0)
				    displaceVector = v.normal;
				    //normalize(displaceVector);
				    //displaceVector.x *= 10;
				    //displaceVector.y *= .00001;//actually z
					//displaceVector.z *= 10;

                //else
				    //displaceVector = ((1,1,1) + v.normal) * .5;
				fixed _WaveOffset = _Time[1] * _TimeMultiplier;
				// v.vertex.x *= _DisplacementScale.x;
				// v.vertex.y *= _DisplacementScale.y;
				// v.vertex.z *= _DisplacementScale.z;
				// v.vertex.x += displaceVector.x * _Displacement * _Extrude * (sin((v.vertex.y + _WaveOffset) * _Waviness ));
				// v.vertex.y += displaceVector.y * _Displacement * _Extrude * (sin((v.vertex.y + _WaveOffset) * _Waviness + .5));
				// v.vertex.z += displaceVector.z * _Displacement * _Extrude * (sin((v.vertex.y + _WaveOffset) * _Waviness - .5));
				float3 myDisplacement = (0,0,0);
				if (_Displacement > 0){
					myDisplacement.xyz += displaceVector * _Extrude * ((sin((v.vertex.x + _WaveOffset)+1) * _Waviness )/2);
					myDisplacement.xyz += displaceVector * _Extrude * ((sin((v.vertex.y - _WaveOffset)+1) * _Waviness )/2);
					myDisplacement.xyz += displaceVector * _Extrude * ((sin((v.vertex.z + _WaveOffset*.5)+1) * _Waviness )/2);

					v.vertex.xyz += myDisplacement.xyz/3 * _DisplacementScale;
				}
				//v.vertex.xyz += displaceVector * _Displacement * _Extrude * ((sin((v.vertex.x + _WaveOffset) * _Waviness) + sin((v.vertex.y - _WaveOffset)* _Waviness) + sin((v.vertex.z + _WaveOffset * .5) * _Waviness) - 1.5) * 2);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv) * _Color;
				// apply fog
				//UNITY_APPLY_FOG(i.fogCoord, col);
				return col * _Boost;
			}
			ENDCG
		}
        //Blend SrcAlpha OneMinusSrcAlpha

		Pass {
			Cull Front

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
				
			struct appdata {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			float _Displacement;
            float _NormalDisplacement;
            float _DisplacementScale;
			fixed _Extrude;
			fixed _Waviness;

			struct v2f {
				float4 pos : SV_POSITION;
				fixed4 color : COLOR;
			};
			
			float _OutlineWidth;
			float4 _OutlineColor;
			float _Outline;
			fixed4 _Color;
			float _Boost;
			float _TimeMultiplier;

			v2f vert(appdata v) {
				float3 displaceVector;
                //if (_NormalDisplacement > 0)
				    displaceVector = v.normal;
				    //displaceVector.x *= 10;
				    //displaceVector.y *= .00001;//actually z
					//displaceVector.z *= 10;
                //else
				    //displaceVector = ((1,1,1) + v.normal) * .5;
				fixed _WaveOffset = _Time[1] * _TimeMultiplier;
				// v.vertex.x *= _DisplacementScale.x;
				// v.vertex.y *= _DisplacementScale.y;
				// v.vertex.z *= _DisplacementScale.z;
				// v.vertex.x += displaceVector.x * _Displacement * _Extrude * (sin((v.vertex.y + _WaveOffset) * _Waviness ));
				// v.vertex.y += displaceVector.y * _Displacement * _Extrude * (sin((v.vertex.y + _WaveOffset) * _Waviness + .5));
				// v.vertex.z += displaceVector.z * _Displacement * _Extrude * (sin((v.vertex.y + _WaveOffset) * _Waviness - .5));

				float3 myDisplacement = (0,0,0);
				if (_Displacement > 0){
					myDisplacement.xyz += displaceVector * _Extrude * ((sin((v.vertex.x + _WaveOffset)+1) * _Waviness )/2);
					myDisplacement.xyz += displaceVector * _Extrude * ((sin((v.vertex.y - _WaveOffset)+1) * _Waviness )/2);
					myDisplacement.xyz += displaceVector * _Extrude * ((sin((v.vertex.z + _WaveOffset*.5)+1) * _Waviness )/2);

					v.vertex.xyz += myDisplacement.xyz/3 * _DisplacementScale;
				}



				//v.vertex.xyz += displaceVector * _Displacement * _Extrude * (((sin((v.vertex.x + _WaveOffset) + 1)* _Waviness) + (sin((v.vertex.y - _WaveOffset)+1)* _Waviness) + (sin((v.vertex.z + _WaveOffset * .5) +1)* _Waviness) - 1.5) * 2);

				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex); //position in clipping space

				float3 norm   = normalize(mul ((float3x3)UNITY_MATRIX_IT_MV, v.normal));//worldspace normal
				float2 offset = TransformViewToProjection(norm.xy); //worldspace normal in clipping space

				o.pos.xy += offset * saturate(pow(o.pos.z,1.5)) * _OutlineWidth * _Outline; //add clipping offset normal mult by z?
				o.color = _OutlineColor * _Outline;
				if (_Outline == 0)
					o.color = _Color * _Boost;//same as normal pass

				return o;
			}


			fixed4 frag(v2f i) : SV_Target
			{
				return i.color;
			}
			ENDCG
		}

	}
}
