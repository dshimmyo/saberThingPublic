Shader "Shimmy/saberCutOld" {
	
	Properties {
		_Color ("Color", Color) = (1,1,0,1)
		_MainTex ("Texture", 2D) = "white" {}
        //_HeatStartColor ("HeatStartColor", Color) = (1,1,0,1)
        //_HeatEndColor ("HeatEndColor", Color) = (1,0,0,1)
        _HeatInterval ("HeatInterval", Float) = 5
        _HeatStart ("HeatStart", Float) = 0
        _CoolTime ("CoolTime", Float) = 1
		//_OpacityMap ("Opacity", 2D) = "white" {}

		//_BumpMap ("Normal Map", 2D) = "bump" {}
        //_Smoothness ("Smoothness", Range (0,1))=0

	}
    
   SubShader {
        Tags {
            "Queue" = "Geometry"
        }
        ZWrite On
		CGPROGRAM
			#pragma surface surf Standard //vertex:vert addshadow

			struct appdata {
				float4 vertex: POSITION;
				float3 normal: NORMAL;
				float4 texcoord: TEXCOORD0; //hack?
				float4 texcoord1: TEXCOORD0; //hack?
				float4 texcoord2: TEXCOORD0; //hack?
				float4 tangent: TANGENT; //hack?
			};

			// void vert (inout appdata v) {
			// 	float3 displaceVector;
   //              if (_NormalDisplacement > 0)
			// 	    displaceVector = v.normal;
   //              else
			// 	    displaceVector = ((1,1,1) + v.normal) * .5;
			// 	v.vertex.xyz += displaceVector * _Displacement * _Extrude * ((sin((v.vertex.x + _WaveOffset)* _Waviness) + sin((v.vertex.y - _WaveOffset)* _Waviness) + sin((v.vertex.z + _WaveOffset * .5) * _Waviness) - 1.5) * 2);
			
   //          }

            sampler2D _MainTex;
            //sampler2D _BumpMap;

            //sampler2D _OpacityMap;

            struct Input {
                float2 uv_MainTex;
                //float2 uv_BumpMap;
                //float2 uv_OpacityMap;
            };

            fixed4 _Color;
            fixed4 _HeatStartColor;
            fixed4 _HeatEndColor;

            float _HeatInterval;
            float _HeatStart;
            float _CoolTime;
            //fixed _Smoothness;
        float3 hsv_to_rgb(float3 HSV)
        {
            float3 RGB = HSV.z;

            float var_h = HSV.x * 6;
            float var_i = floor(var_h);   // Or ... var_i = floor( var_h )
            float var_1 = HSV.z * (1.0 - HSV.y);
            float var_2 = HSV.z * (1.0 - HSV.y * (var_h-var_i));
            float var_3 = HSV.z * (1.0 - HSV.y * (1-(var_h-var_i)));
            if      (var_i == 0) { RGB = float3(HSV.z, var_3, var_1); }
            else if (var_i == 1) { RGB = float3(var_2, HSV.z, var_1); }
            else if (var_i == 2) { RGB = float3(var_1, HSV.z, var_3); }
            else if (var_i == 3) { RGB = float3(var_1, var_2, HSV.z); }
            else if (var_i == 4) { RGB = float3(var_3, var_1, HSV.z); }
            else                 { RGB = float3(HSV.z, var_1, var_2); }

            return (RGB);
        }

			void surf (Input IN, inout SurfaceOutputStandard o) {
		      	fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
                float timeElapsed = _Time[1] - _HeatStart;
                float heatRatio = (_HeatInterval - timeElapsed) / _HeatInterval;
                o.Albedo = (c.rgb * _Color);

                float sineTime = 0;
                float sineWaveUV = sin((IN.uv_MainTex.x + IN.uv_MainTex.y) * 3.14 * 10) * .5 + .5;
                //float3 hsv = float3(max(heatRatio,0 ) * .12, 1,1); //.16(yellow) downto 0(red)
                float3 hsv = (0,0,0);//float3(heatRatio * .12, 1,1); //.16(yellow) downto 0(red)

                if (heatRatio>0){
                    sineTime = (sin(_Time[3]*4)+1) / 2 * .33 + .67;
                } else {
                    sineTime = 1;
                }
                fixed3 e = (0,0,0);
                if (timeElapsed < _HeatInterval + _CoolTime){
                    hsv = float3(heatRatio * .12, 1,1);
                    hsv.x *= sineTime * (pow(sineWaveUV, 2) * .85 + .15);//(sineTime + pow(sineWaveUV, 2)) / 2;
                    e = half3(hsv_to_rgb(hsv));// * c.g; //was cheating using hte green channel to give some variation across the surface
                }

                //_CoolTime is the number of seconds to cool

                if (heatRatio <= 0){
                    float coolRatio = (_CoolTime - (timeElapsed - _HeatInterval)) / _CoolTime;
                    if (coolRatio >= 0)
                        o.Emission = e * max(coolRatio,0) /* * (pow(sineWaveUV, 2) * .5 + .5)*/; //cooldown
                    else
                        o.Emission = (0,0,0);
                } else {  //this stuff should be done in the fragment shader
                    o.Emission = e;
                    o.Albedo = o.Albedo * (1-heatRatio) + o.Emission * heatRatio;//blends the emission color with albedo
                }

                //o.Metallic = 0;
                //o.Smoothness = 0;

			}
		ENDCG

    } 
    Fallback "Standard"
}

