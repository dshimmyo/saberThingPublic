Shader "Shimmy/saberCutV2" {
    
    Properties {
        _Color ("Color", Color) = (1,1,0,1)
        _MainTex ("Texture", 2D) = "white" {}
        //_HeatEncodeTex ("HeatEncode", 2D) = "white" {}//test encoding heat in uv2 coordinate
        _Specular ("Specular", 2D) = "black" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}

        _HeatInterval ("HeatInterval", Float) = 15
        _HeatStart ("HeatStart", Float) = 0

        _HeatMap ("Heat Map", 2D) = "white" {}
        _HeatMapMin ("HeatMapMin", Range (0,1))=0.1
        _HeatEmissionBoost ("HeatEmissionBoost", Float) = 3
		_HeatColorLookup ("HeatColorLookup",2D) = "white" {}//put a map in here that ramps from 0->1 in U with heat map colors
		[Toggle] _HeatColorLookupToggle ("HeatColorLookup", Float) = 0
        [Toggle] _HeatSignalEncodeToggle ("HeatSignalEncode", Float) = 0

    }
    
   SubShader {
        Tags {
            "Queue" = "Geometry"
        }
        ZWrite On
        //Cull off
        CGPROGRAM
            #pragma surface surf StandardSpecular halfasview//approxview //vertex:vert addshadow
            #pragma target 3.5 //fixes stupid error 'Too many texture interpolators would be used for ForwardBase pass...'  //test encoding heat in uv2 coordinate
            sampler2D _MainTex;
            sampler2D _HeatMap;
            sampler2D _Specular;
            sampler2D _BumpMap;
			sampler2D _HeatColorLookup;
            //sampler2D _HeatEncodeTex;//testing

            struct Input { 
                float2 uv_MainTex: TEXCOORD0;
                float2 uv2_MainTex2; //strange solution to an error by adding the 2
                float2 uv_HeatMap;
                float2 uv_Specular;
                float2 uv_BumpMap;
				float2 uv_HeatColorLookup;
            };

            fixed4 _Color;
            fixed4 _HeatStartColor;
            fixed4 _HeatEndColor;
            fixed _HeatMapMin;
            fixed _HeatEmissionBoost;
            half _HeatInterval;
            half _HeatStart;
			fixed3 _HeatColorArray[128];// = new Vector4[10];
			//float _Segments[1000];
			//fixed4 _HeatColorLookup;
            fixed _HeatColorLookupToggle;
            fixed _HeatSignalEncodeToggle;

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

            void surf (Input IN, inout SurfaceOutputStandardSpecular o) {
				//_HeatColorArray = new fixed3[

                fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
                fixed4 h = tex2D (_HeatMap, IN.uv_HeatMap);
                fixed4 fake = tex2D (_MainTex, IN.uv2_MainTex2); //test encoding heat in uv2 coordinate

                half sineWaveUV = sin((IN.uv_MainTex.x + IN.uv_MainTex.y) * 3.14 * 10) * .3 + .7; //wave across the surface diagonally
                c *= h * .5 + .5;

                half timeElapsed = _Time[1] - _HeatStart;
                float heatRatio = 0;
                if (_HeatSignalEncodeToggle > 0)
                {
                    heatRatio = IN.uv2_MainTex2.x; //test encoding heat in uv2 coordinate
                } else {
                    heatRatio = (_HeatInterval - timeElapsed) / _HeatInterval;
                }
                o.Albedo = (c.rgb * _Color);
                o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_BumpMap));

                half3 hsv = half3(0,0,0);
                //fixed values fall between -2 and +2

                fixed3 e = fixed3(0,0,0);

				if (timeElapsed < _HeatInterval || _HeatSignalEncodeToggle > 0) {
					heatRatio *= sineWaveUV;
					heatRatio *= h.x * (1 - _HeatMapMin) + _HeatMapMin;//IN.uv_HeatMap.x;
					//int heatColorIndex = (int)(heatRatio * 127);
					if (_HeatColorLookupToggle==0) {//I just looked at the profiler and I think this procedural version is faster than the texture lookup!
						hsv = half3(heatRatio * .18 - .02, (1 - pow(1 - heatRatio, 2)) * .05 + .95, min(heatRatio*heatRatio * 7, 6));//original expensive(?) setting
						//hsv = half3(heatRatio * .18 - .02, (heatRatio) * .05 + .95, min(heatRatio*heatRatio * 7, 6));//attempting a cheaper setting
						e = half3(hsv_to_rgb(hsv)); //hsv looks much better with half precision compared to fixed. 
					} else {//original cheap version
						fixed2 hcl_uv = 1;// (0.0, heatRatio);
						hcl_uv *= heatRatio;
                        float4 hcl = tex2D(_HeatColorLookup, hcl_uv);//I read somewhere that looking up a texture takes a while, maybe there's an even cheaper way...
						fixed power = 1;// 1.15;
						e = half3(pow(hcl.x, power),pow(hcl.y, power),pow(hcl.z, power)) * 2;// half3(hcl.xyz);
					}
                }

                if (heatRatio <= 0){
					o.Emission = e * 0;// (0, 0, 0);
                } else {  //this stuff should be done in the fragment shader maybe
                    o.Emission = e * _HeatEmissionBoost;
                }
                o.Specular = tex2D (_Specular, IN.uv_Specular);
            }
        ENDCG

    } 
    Fallback "Standard"
}

