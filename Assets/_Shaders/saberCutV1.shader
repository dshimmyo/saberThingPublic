Shader "Shimmy/saberCut" {
    
    Properties {
        _Color ("Color", Color) = (1,1,0,1)
        _MainTex ("Texture", 2D) = "white" {}
		_EmissionMap("EmissionMap", 2D) = "black" {}
		_EmissionGain ("EmissionGain", Float) = 1
        [HideInInspector] 
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
        [Toggle] _UseHeatMapToggle ("HeatMapToggle", Float) = 1
        //[Toggle] _UseDisplacementToggle ("UseDisplacement", Float) = 1
        [Toggle] _ProceduralCutBumpToggle ("ProceduralCutBump", Float) = 0

    }
    
   SubShader {
        Tags {
            "Queue" = "Geometry"
        }
        ZWrite On
        //Cull Off //use this when testing meshcut capping

        CGPROGRAM
            #pragma surface surf Standard vertex:disp addshadow//halfasview//approxview //vertex:vert addshadow
            #pragma target 3.5 //fixes stupid error 'Too many texture interpolators would be used for ForwardBase pass...'  //test encoding heat in uv2 coordinate
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
                float3 viewDir;//compare to normal to quench the marching ants at glancing angles
                float3 worldNormal; INTERNAL_DATA
                float3 worldPos;
                float3 localPos;
				//float4 outpos : SV_POSITION;
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
            fixed _UseDisplacementToggle = (float)1;
            fixed _UseHeatMapToggle;
            fixed _ProceduralCutBumpToggle;
			float4 _MainTex_TexelSize;

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

		float3 get_cheap_color_grad(float signal)
		{
			float3 outColor = float3(0, 0, 0);

			float redThreshold = .45;
			float yellowThreshold = .65;
            float whiteThreshold = 1;

			if (signal < redThreshold && signal > 0.0001)
			{
				outColor.r = signal / redThreshold ;// = new Color(signal * .667f, 0, 0);
				outColor.g = 0;
				outColor.b = 0;
			}
			else if (signal <= yellowThreshold)//2 //red hot at 1.5, 2 and beyond is yellow
			{
				float channel = (signal - redThreshold) * (1/(yellowThreshold - redThreshold));//(signal - .5) * .5;//(signal - 1.5f) * .5f;
				outColor.r = 1;
				outColor.g = channel;
				outColor.b = 0;
			}
			else if (signal <= whiteThreshold)//> yellowThreshold)//.75)//2
			{
				float channel = (signal - yellowThreshold) * (1/(whiteThreshold - yellowThreshold));//lerp(1,6,signal - yellowThreshold);//(signal - .5) * 4;//.75 //want it to start at 1
				outColor.r = channel;// = float3(channel, channel, 0);
				outColor.g = channel;
				outColor.b = 0;
			}
            else if (signal > whiteThreshold)
            {
                float channel = signal - whiteThreshold;
                outColor.r = 1;
                outColor.g = 1;
                outColor.b = min(1,channel);

            }

			return outColor;
		}

		float3 get_cheap_color_gradBaked(float signal)
		{
            float3 outColor = float3(0, 0, 0);

            //float redThreshold = .45;
            //float yellowThreshold = .65;
            //float whiteThreshold = 1;

            if (signal < .45 && signal > 0.0001)
            {
                outColor.r = signal / .45 ;// = new Color(signal * .667f, 0, 0);
                outColor.g = 0;
                outColor.b = 0;
            }
            else if (signal <= .65)//2 //red hot at 1.5, 2 and beyond is yellow
            {
                float channel = (signal - .45) * 5;// (1/(.65 - .45)) = 1/.2 = 5
                outColor.r = 1;
                outColor.g = channel;
                outColor.b = 0;
            }
            else if (signal <= 1)//> yellowThreshold)//.75)//2
            {
                float channel = (signal - .65) * 2.857;//(1/(1 - .65)) = 1/.35 = 2.857
                outColor.r = 1;//channel;// = float3(channel, channel, 0);
                outColor.g = channel;
                outColor.b = 0;
            }
            else if (signal < 2)
            {
                float channel = signal - 1;
                //float otherChannel = max(0,channel - 1);
                outColor.r = 1;
                outColor.g = 1;
                outColor.b = channel;//min(1,channel);

            } 
            else 
            {
                float channel = signal - 1;
                //float otherChannel = max(0,channel - 1);
                outColor.r = channel;
                outColor.g = channel;
                outColor.b = channel;//min(1,channel);
            }

            return outColor;
		}

		struct appdata {
			float4 vertex: POSITION;
			float3 normal: NORMAL;
			float2 texcoord: TEXCOORD0; //hack?
			float2 texcoord1: TEXCOORD1; //hack?
			float2 texcoord2: TEXCOORD2; //hack?
			float4 tangent: TANGENT; //hack?
			
			//float4 outpos : SV_POSITION;//clip space? was SV_POSITION but I got an error, think this is very hacky and possibly going to cause issues
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

            
			//float4 clipPos = UnityObjectToClipPos(v.vertex);
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
		}

		half FastSqrtInvAroundOne(half x)
		{
			const half a0 = 15.0f / 8.0f;
			const half a1 = -5.0f / 4.0f;
			const half a2 = 3.0f / 8.0f;

			return a0 + a1 * x + a2 * x * x;
		}

		half3 FastNormalize(fixed3 v)
		{
			half len_sq = v.x * v.x + v.y * v.y + v.z * v.z;
			half len_inv = FastSqrtInvAroundOne(len_sq);
			return half3(v.x* len_inv, v.y* len_inv, v.z* len_inv);
		}

        void disp (inout appdata v)//, out Input o //just hacking trying to fix a weird artifact
        {
            if (v.texcoord1.y < 1 && v.texcoord1.y > 0.0001)
            {
                //if (_UseDisplacementToggle==1)//don't need to toggle it because the signal will never be adjusted
                half dist = length(ObjSpaceViewDir(v.vertex));
                half minDist = 2;//within this distance diplacement is at maximum
                half maxDist = 7;//beyond thid distance displacement will be disabled
                fixed dispFilterMult = 1;
                if (dist > minDist)
                {
                    dispFilterMult = lerp(1,0,(dist - minDist)/(maxDist-minDist));
                }
                if (dist < maxDist)
                {
                    //these 4 lines are the most concise and lest computationally heavy version of the displacement that seems to look good globally
                    half3 dispMult = mul(unity_WorldToObject, half3(.1,.1,.1));//start with neutral vector mult, scaled to object space
                    dispMult *= v.texcoord1.y;//min(1,v.texcoord1.y);//calculate the actual displacement value from the tex coord and the arbitrary distance
                    dispMult = abs(dispMult);
                    v.vertex.xyz += -v.normal.xyz * dispMult * dispFilterMult;//dispMult;//half3(abs(dispMult.x),abs(dispMult.y),abs(dispMult.z));//displacement
                    //v.texcoord1.x *= dispFilterMult;
                }
            }
			//o.worldpos = mul(unity_ObjectToWorld, v.vector.xyz);//more hacking
			//v.outpos.xyz = v.vertex.xyz;
        }

		/*
		struct VertexInput {
			float4 vertex : POSITION;       //local vertex position
			float3 normal: NORMAL;
			float2 texcoord: TEXCOORD0; //hack?
			float2 texcoord1: TEXCOORD1; //hack?
			float2 texcoord2: TEXCOORD2; //hack?
			float4 tangent: TANGENT; //hack?
		};
		struct VertexOutput {
			float4 pos : SV_POSITION;       //screen clip space position and depth

		};
		VertexOutput disp(VertexInput v)
		{
			VertexOutput o = (VertexOutput)0;
			if (v.texcoord1.y < 1 && v.texcoord1.y > 0.0001)
			{
				float value = min(1, v.texcoord1.y) * .1;//.05 wimpy
				//value = mul(unity_ObjectToWorld, value);//reduced displacement on the smaller object
				value = mul(unity_WorldToObject, value);
				v.vertex.xyz += -v.normal.xyz * value;//displacement
				o.pos = UnityObjectToClipPos(v.vertex);
				return o;
			}
		}
		*/
        float hash( float n )
        {
            return frac(sin(n)*43758.5453);
        }
         
        float noiseTweaked( float2 xx )
        {
            // The noise function returns a value in the range -1.0f -> 1.0f
            float3 x = (float3(xx.x,xx.y,.1) - float3(xx.x,xx.y,.75) * 3 + float3(xx.y,xx.x,.25) * 64 - float3(xx.y * .5,xx.x,.25) * 128 + float3(xx.x,.45,xx.y) * 300)/3 ;
            float3 p = floor(x);
            float3 f = frac(x);
         
            f       = f*f*(3.0-2.0*f);
            float n = p.x + p.y*57.0 + 113.0*p.z;
         
            return lerp(lerp(lerp( hash(n+0.0), hash(n+1.0),f.x),
                           lerp( hash(n+57.0), hash(n+58.0),f.x),f.y),
                       lerp(lerp( hash(n+113.0), hash(n+114.0),f.x),
                           lerp( hash(n+170.0), hash(n+171.0),f.x),f.y),f.z) * (sin(xx.x * 3) + 1) * (sin(xx.y * 5) + 1) * .25 ;
        }
        float noise( float2 xx )
        {
            // The noise function returns a value in the range -1.0f -> 1.0f
			//modified to return 0-1
            float3 x = float3(xx.x, xx.y, 1);
            float3 p = floor(x);
            float3 f = frac(x);
         
            f       = f*f*(3.0-2.0*f);
            float n = p.x + p.y*57.0 + 113.0*p.z;
         
            return (lerp(lerp(lerp( hash(n+0.0), hash(n+1.0),f.x),
                           lerp( hash(n+57.0), hash(n+58.0),f.x),f.y),
                       lerp(lerp( hash(n+113.0), hash(n+114.0),f.x),
                           lerp( hash(n+170.0), hash(n+171.0),f.x),f.y),f.z) +1)/2;
        }
		

        float noise3D( float3 xx)//se
        {
            // The noise function returns a value in the range -1.0f -> 1.0f
            float3 x = xx;//float3(abs(xx.x), abs(xx.y), abs(xx.z)) ;//* mul(unity_ObjectToWorld, float3(scale,scale,scale));//transform this position into a worldspace scale adjusted position
            float3 p = floor(x);
            float3 f = frac(x);
         
            f       = f*f*(3.0-2.0*f);
            float n = p.x + p.y*57.0 + 113.0*p.z;
         
            return lerp(0,1,(1+lerp(lerp(lerp( hash(n+0.0), hash(n+1.0),f.x),
                           lerp( hash(n+57.0), hash(n+58.0),f.x),f.y),
                       lerp(lerp( hash(n+113.0), hash(n+114.0),f.x),
                           lerp( hash(n+170.0), hash(n+171.0),f.x),f.y),f.z))/2);
        }

        float filterSides(float value,float filter)
        {
            return lerp(.5,value,filter);
        }

        float HNoise(fixed2 myUv)
        {   //scaling uvs doesn't make sense. make an object space proc that scales based on the worldspace scale
            half2 scaledUV = myUv;// * (_MainTex_TexelSize.ba * .001);//mul(unity_WorldToObject,fixed2(1,1));
            return (saturate(noise(scaledUV * 128)) * .5//128 might be too highres, also try to get a consistent scale
                    + saturate((noise(scaledUV * 6) ) ) * .5//nice and blurry photoshop cloud
                    ) * max(noise(scaledUV * 4) * 2 - .75,0.5);//for some reason going above 1 creates dark swimmy artifacts
        }

        fixed3 heatToNormal(fixed2 myUv, float2 heightMapTexelSize)//send it (uv, _HeightMap_TexelSize)
        {
            float bumpStrength = 10;
            float3 normal = float3(0, 0, 1);
       
            float heightSampleCenter = HNoise(myUv);//tex2D (_HeightMap, IN.uv_MainTex).r;
            float heightSampleRight = HNoise(myUv + float2(heightMapTexelSize.x, 0));//tex2D (_HeightMap, IN.uv_MainTex + float2(_HeightMap_TexelSize.x, 0)).r;
            float heightSampleUp = HNoise(myUv + float2(0, heightMapTexelSize.y));//tex2D (_HeightMap, IN.uv_MainTex + float2(0, _HeightMap_TexelSize.y)).r;
     
            float sampleDeltaRight = heightSampleRight - heightSampleCenter;
            float sampleDeltaUp = heightSampleUp - heightSampleCenter;
     
            normal = cross(
            float3(1, 0, sampleDeltaRight * bumpStrength),// * _BumpStrength),
            float3(0, 1, sampleDeltaUp * bumpStrength));//_BumpStrength));
     
            normal = normalize(normal);

            return normal;
        }

        void surf (Input IN, inout SurfaceOutputStandard o) 
        {
            float camDist = distance(IN.worldPos, _WorldSpaceCameraPos);
            half minDistance = 2;
            half maxDistance = 20;
            fixed distanceFilterMult = 1;

            float3 cameraRay = IN.worldPos - _WorldSpaceCameraPos.xyz;
            float3 camDir = normalize(cameraRay);
            half myDot = -dot(WorldNormalVector (IN, o.Normal),camDir);//after hacking back and forth this actually works!
            float noiseScaleFilter = lerp(0,1,myDot);
            if (camDist > minDistance)
            {
                distanceFilterMult = lerp (1,0,(camDist - minDistance) / (maxDistance - minDistance));
            }

            fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
            c.rgb *= _Color;

            float4 h = float4(1,1,1,1);
            if (_UseHeatMapToggle == 1){
                h = tex2D (_HeatMap, IN.uv_HeatMap);
            } else {

                //procedural heat noise signal
                fixed2 myUv = IN.uv_HeatMap;//start with a uv, or consider a worldspace scaled 3d noise

                //3d noise
                //float3 pos = IN.localPos;
                //h = (noise3D(pos * 100));// * .5
                //+ noise3D(pos * 33) * .5)
                //* lerp(.15,.7,((sin(myUv.x * 7 + myUv.y * 4) + 1) / 2
                //+ (sin(myUv.x * 5 + myUv.y * 8) + 1) / 2 ));

                
                //2d noise
                //h = (saturate(noise(myUv * 128)) * .5//looks pixelated
                //    + saturate((noise(myUv * 6) ) ) * .5//nice and blurry photoshop cloud
                //    ) * saturate(noise(myUv * 4) * 2 - 1);
                h = HNoise(myUv);
            }
            fixed2 myUv2 = IN.uv2_HeatEncodeTex;
            myUv2.x *= distanceFilterMult;
			if (myUv2.y > 0) {//after cut or if displaced, noise up the albedo
				c *= saturate(h) * .5 + .5;
			}

            half timeElapsed = _Time[1] - _HeatStart;
            float heatRatio = 0;
            float heatDisplacementDarkenMultiplier = 1;
            float heatNoise = 1;
            if (camDist < maxDistance)
            {
                if (_HeatSignalEncodeToggle > 0)
                {
                    heatRatio = myUv2.x; //test encoding heat in uv2 coordinate
                    if (myUv2.y > 0.001 && myUv2.y < .99){
                        float edgeboost = min(lerp(0,.5,myUv2.y * 100),1);
                        float interiorMask = max(min(1,lerp(1,0,(myUv2.y - .02) * 100)),0);//.02 to .03
                        if (myUv2.y > .02)
                            edgeboost *= interiorMask;
    					float desat = .1;//.2;
    					float boost = 2;
                        c.rgb += (desat * edgeboost);//desat around edges
    					c.rgb *= max(1, boost * edgeboost * (1-desat));//boost around edges
                        //c.r = saturate(c.r);
                        //c.g = saturate(c.g);
                        //c.b = saturate(c.b);

                        c = saturate(c);
                        heatDisplacementDarkenMultiplier = min(max(lerp(1,.5,myUv2.y * 50),.5),1);//needed to min it to .2 because lerp isn't clamped
                    }
                } else {
                    heatRatio = (_HeatInterval - timeElapsed) / _HeatInterval;
                }
            }

            if (heatDisplacementDarkenMultiplier < .99)
                heatNoise = saturate(lerp(tex2D(_EmissionMap, IN.uv_EmissionMap),1,heatDisplacementDarkenMultiplier ));

            c.rgb *= heatNoise;
            o.Albedo = c.rgb; 

            if (_UseHeatMapToggle)
            {
                o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_BumpMap));
            } 
            else 
            {
                if (myUv2.y >= 1 && _ProceduralCutBumpToggle == 1)//displaced/burnt surfaces will get the procedural grunge bump
                    o.Normal = heatToNormal(IN.uv_HeatMap,_MainTex_TexelSize);
                else
                    o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_BumpMap));
            }
            //fixed values fall between -2 and +2

            float3 e = float3(0,0,0);
            if (camDist < maxDistance)
            {
    			if (timeElapsed < _HeatInterval || _HeatSignalEncodeToggle > 0) {
    				heatRatio *= h.x * (1 - _HeatMapMin) + _HeatMapMin;//IN.uv_HeatMap.x;
					if (_HeatColorLookupToggle==0) 
					{
						e = float3(heatRatio * heatRatio * 7 * get_cheap_color_gradBaked(heatRatio));//changed half to float to fix emission artifact!
					} else {//original cheap version
						fixed2 hcl_uv = 1;// (0.0, heatRatio);
						hcl_uv.x *= saturate(heatRatio);
                        fixed4 hcl = tex2D(_HeatColorLookup, hcl_uv);//I read somewhere that looking up a texture takes a while, maybe there's an even cheaper way...
						//fixed power = 1.15;// 1.15;
						//e = half3(pow(hcl.x, power), pow(hcl.y, power), pow(hcl.z, power)) * 2;// half3(hcl.xyz);
						e = float3(hcl.xyz);
					}
                }
            }

			fixed4 em = tex2D(_EmissionMap, IN.uv_EmissionMap);

            o.Emission = em * _EmissionGain;//guess I'm not going to bother to limit it
            //if (myUv2.x < 0.0001 )//was heatRatio //texel stuff isn't doing anything _MainTex_TexelSize.x > 1
			//{
            //    o.Emission = /*e * 0 +*/max(0, min(4,em * _EmissionGain) );// (0, 0, 0);
                //o.Emission = saturate(em) * _EmissionGain;// (0, 0, 0);
            //} else {
            if (myUv2.x > 0.0001 )
            {
                //o.Emission = e * _HeatEmissionBoost + max(0, min(4, em * _EmissionGain));//* lerp(1,0,distance / 10); //distance stuff didn't work
                o.Emission += e * _HeatEmissionBoost;//* lerp(1,0,distance / 10); //distance stuff didn't work
            }
            
			//o.Specular = 1;
            //o.Specular = tex2D (_Specular, IN.uv_Specular) * _SpecGain * heatNoise;
			o.Smoothness = _Smoothness * tex2D(_Specular, IN.uv_Specular) * heatNoise; //1;// _Smoothness;// tex2D(_Specular, IN.uv_Specular);
             // Metallic and smoothness come from slider variables
             o.Metallic = tex2D (_Specular, IN.uv_Specular) * _SpecGain * heatNoise;//_Metallic;
             //o.Smoothness = _Glossiness;
        }
        ENDCG

    } 
    Fallback "Standard"
}

