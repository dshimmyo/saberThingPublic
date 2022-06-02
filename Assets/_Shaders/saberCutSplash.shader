Shader "Shimmy/saberCutSplash" {
    
    Properties {
        _Color ("Color", Color) = (1,1,0,1)
        _MainTex ("Texture", 2D) = "white" {}
		_EmissionMap("EmissionMap", 2D) = "black" {}
		_EmissionGain ("EmissionGain", Float) = 1
        _HeatEncodeTex ("HeatEncode", 2D) = "white" {}//test encoding heat in uv2 coordinate //why did I need this map?
        _SpecGain("SpecularGain", Float) = 0
        _Specular ("Specular", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
		_Smoothness("Smoothness", Float) = 0

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
        //Cull Off//unfortunately I might need this to avoid the bloom artifact//didn't fix it.
        CGPROGRAM
            #pragma surface surf StandardSpecular vertex:vert halfasview//approxview //vertex:vert addshadow
            #pragma target 3.0 //fixes stupid error 'Too many texture interpolators would be used for ForwardBase pass...'  //test encoding heat in uv2 coordinate
            sampler2D _MainTex;
			sampler2D _EmissionMap;
			sampler2D _HeatMap;
			sampler2D _Specular;
            sampler2D _BumpMap;
			sampler2D _HeatColorLookup;
            sampler2D _HeatEncodeTex;//testing

            struct Input { 
                float2 uv_MainTex: TEXCOORD0;
                float2 uv2_HeatEncodeTex; //test encoding heat in uv2 coordinate
                float2 uv_HeatMap;
				float2 uv_EmissionMap;
                float2 uv_Specular;
				//float2 uv_Smoothness;
                float2 uv_BumpMap;
				float2 uv_HeatColorLookup;
            };
            float _SpecGain;
			float _EmissionGain;
            fixed4 _Color;
			fixed _Smoothness;
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

		struct appdata {
			float4 vertex: POSITION;
			float3 normal: NORMAL;
			float2 texcoord: TEXCOORD0; //hack?
			float2 texcoord1: TEXCOORD1; //hack?
			float2 texcoord2: TEXCOORD2; //hack?
			float4 tangent: TANGENT; //hack?
			//float4 outpos : POSITION;//clip space? was SV_POSITION but I got an error, think this is very hacky and possibly going to cause issues
		};
		//struct v2f {
		//	float uv : TEXCOORD0;
		//};
		void vert(inout appdata v) {

			/*
			float3 displaceVector;
			displaceVector = v.vertex * .01;
			displaceVector = v.vertex * .01;

			float3 mult;
			mult = float3(0, 0, 0);
			float _Waviness = 3;
			float _WaveOffset = _Time.y * 100;
			if (v.texcoord1.x > 1.05 && v.texcoord1.y < .9) {
				v.vertex.xyz += lerp(0, 1, v.texcoord1.x - 1.05)
					* displaceVector * _Waviness
					* ((sin((v.vertex.x + _WaveOffset))
						+ sin((v.vertex.y + _WaveOffset))
						+ sin((v.vertex.z + _WaveOffset))));
			}*/

			//try displacement in worldspace float3 pos = mul (_Object2World, i.uv).xyz;
			//v2f o;
			//o.uv = v.texcoord;
			//v.outpos = UnityObjectToClipPos(v.vertex);
			float4 clipPos = UnityObjectToClipPos(v.vertex);
			float3 viewPos = UnityObjectToViewPos(v.vertex);
			if (v.texcoord1.x > 1) {//this looks good but I can't normalize the perceived scale because it depends on the uv mapping
				float heatSignal = pow(min(lerp(0, 1, v.texcoord1.x * 3),1),2) * .001;
				float heatSignalSpeedMult = _Time.y * 35 * lerp(.5, 1, heatSignal);
				float weirdMult = 10/(viewPos.z % 10);// 1 / viewPos.z;
				float weirdMultx = .1 / (viewPos.x % 10);// 1 / viewPos.z;
				float weirdMulty = .1 / (viewPos.y % 10);// 1 / viewPos.z;

				v.texcoord.x += sin(heatSignalSpeedMult) * heatSignal;// *.1;// *weirdMultx;// *.01;
				v.texcoord.y += sin(heatSignalSpeedMult) * heatSignal;// *.1;// *weirdMulty;// *.01;


			}

			//return o;
		}

            void surf (Input IN, inout SurfaceOutputStandardSpecular o) {
                fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
                fixed4 h = tex2D (_HeatMap, IN.uv_HeatMap);
				//if (h.a <= 0)
				{
					h.r *= h.a;// 0;
					h.g *= h.a;//= 0;
					h.b *= h.a;//= 0;
				}
                c *= h * .5 + .5;

                half timeElapsed = _Time[1] - _HeatStart;
                float heatRatio = 0;
                float heatDisplacementDarkenMultiplier = 1;
                float heatNoise = 1;
                if (_HeatSignalEncodeToggle > 0)
                {
					heatRatio = IN.uv2_HeatEncodeTex.x + .05; //test encoding heat in uv2 coordinate
                    if (IN.uv2_HeatEncodeTex.y > 0.001 && IN.uv2_HeatEncodeTex.y < .99){
                        float edgeboost = min(lerp(0,.5,IN.uv2_HeatEncodeTex.y * 100),1);
                        float interiorMask = max(min(1,lerp(1,0,(IN.uv2_HeatEncodeTex.y - .02) * 100)),0);//.02 to .03
                        if (IN.uv2_HeatEncodeTex.y > .02)
                            edgeboost *= interiorMask;
						float desat = .2;
						float boost = 2;
                        c.rgb += (desat * edgeboost);//desat around edges
						c.rgb *= max(1, boost * edgeboost * (1-desat));//boost around edges
                        c.r = min(c.r,1);
                        c.g = min(c.g, 1);
                        c.b = min(c.b, 1);
                        heatDisplacementDarkenMultiplier = min(max(lerp(1,.5,IN.uv2_HeatEncodeTex.y * 50),.5),1);//needed to min it to .2 because lerp isn't clamped
                    }
                } else {
                    heatRatio = (_HeatInterval - timeElapsed) / _HeatInterval;
                }
                c.rgb *= _Color;
                //if (heatDisplacementDarkenMultiplier < .9)
                    //c.rgb *= lerp(tex2D(_EmissionMap, IN.uv_EmissionMap),1,heatDisplacementDarkenMultiplier);
                //c.rgb *= heatDisplacementDarkenMultiplier;
                if (heatDisplacementDarkenMultiplier < .99)
                    heatNoise = min(lerp(tex2D(_EmissionMap, IN.uv_EmissionMap),1,heatDisplacementDarkenMultiplier ),1);
                c.rgb *= heatNoise;
                o.Albedo = c.rgb; /* lerp(tex2D(_EmissionMap, IN.uv_EmissionMap),1,heatDisplacementDarkenMultiplier)*/
                o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_BumpMap));

                //fixed values fall between -2 and +2

                fixed3 e = fixed3(0,0,0);

				if (timeElapsed < _HeatInterval || _HeatSignalEncodeToggle > 0) {
					//heatRatio *= sineWaveUV;//fuck this sine wave, it's a waste of time!
					heatRatio *= h.x * (1 - _HeatMapMin) + _HeatMapMin;//IN.uv_HeatMap.x;
					
					float heatWavinessMult = 1;
					//if (IN.uv2_HeatEncodeTex.x > 1.05)
					//{
					//	heatWavinessMult = lerp(.9, 1.1, sin(_Time.y * 10));
					//}
					if (_HeatColorLookupToggle==0) {//I just looked at the profiler and I think this procedural version is faster than the texture lookup!
						fixed h = heatRatio * heatWavinessMult * .18 - .02;
                        fixed s = 1;//(1 - pow(1 - heatRatio, 1)) * .05 + .95; //1 is cheaper but also oddly more beautiful
                        fixed v = heatRatio * heatWavinessMult * heatRatio * 7;//was min(blah,6)
						e = half3(hsv_to_rgb(half3(h,s,v))); //hsv looks much better with half precision compared to fixed. 
					} else {//original cheap version
						fixed2 hcl_uv = 1;// (0.0, heatRatio);
						hcl_uv *= heatRatio * heatWavinessMult;
                        float4 hcl = tex2D(_HeatColorLookup, hcl_uv);//I read somewhere that looking up a texture takes a while, maybe there's an even cheaper way...
						fixed power = 1.15;// 1.15;
						e = half3(pow(hcl.x, power),pow(hcl.y, power),pow(hcl.z, power)) * 2;// half3(hcl.xyz);
					}
                }

                if (heatRatio < 0.0001){
					o.Emission = /*e * 0 +*/ tex2D(_EmissionMap, IN.uv_EmissionMap) * _EmissionGain;// (0, 0, 0);
                } else {
                    o.Emission = e * _HeatEmissionBoost + tex2D(_EmissionMap, IN.uv_EmissionMap) * _EmissionGain;
                }
				//o.Specular = 1;
                o.Specular = tex2D (_Specular, IN.uv_Specular) * _SpecGain * heatNoise;
				o.Smoothness = _Smoothness * tex2D(_Specular, IN.uv_Specular) * heatNoise; //1;// _Smoothness;// tex2D(_Specular, IN.uv_Specular);
            }
        ENDCG

    } 
    Fallback "Standard"
}

