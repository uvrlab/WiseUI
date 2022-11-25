// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Copyright 2016 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

Shader "SphericalMapping/UnlitTexture" {
 Properties {
   _Color ("Color", Color) = (1,1,1,1)
   _MainTex ("Texture", 2D) = "white" {}
 }
 SubShader {
   Tags { "RenderType"="Opaque" }
   Cull Off
   Blend Off
   ZTest Always
   ZWrite Off
   Lighting Off
   Fog {Mode Off}

   Pass {
     CGPROGRAM
     #pragma vertex vert
     #pragma fragment frag

     #include "UnityCG.cginc"

     struct appdata {
       float4 vertex : POSITION;
       float4 color : COLOR;
       float2 uv : TEXCOORD0;
     };

     struct v2f {
       float2 uv : TEXCOORD0;
       float4 color : COLOR;
       float4 vertex : SV_POSITION;
     };

     sampler2D _MainTex;
     float4 _MainTex_ST;
     float4 _Color;

    
	float4 RotateAroundY(float4 vertex, float radian)
	{
		float sina, cosa;
		sincos(radian, sina, cosa);

		float4x4 m;

		m[0] = float4(cosa, 0, sina, 0) * -1; // x축 대칭 변환을 위해 *-1.
		m[1] = float4(0, 1, 0, 0);
		m[2] = float4(-sina, 0, cosa, 0);
		m[3] = float4(0, 0, 0, 1);

		return mul(m, vertex);
	}

     v2f vert (appdata v) 
	{
		   v2f o;
		   float4 rot = RotateAroundY(v.vertex, radians(-90));
		   o.vertex = UnityObjectToClipPos(rot);
		   o.color = v.color;
		   o.uv = TRANSFORM_TEX(v.uv, _MainTex);
		   return o;
    }

     fixed4 frag (v2f i) : COLOR 
	{
       fixed4 col = tex2D(_MainTex, i.uv) * i.color * _Color;
	   /*
       if(col.a == 0)
       {
       discard;
       }*/
       return col;

    }
     ENDCG
   }
 }
}
