// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/CubemapToEquirectangular" {
Properties {
        _MainTex ("Cubemap (RGB)", CUBE) = "" {}
		_RotationX ("RotationX", Range(0, 360)) = 0
		_RotationY ("RotationY", Range(0, 360)) = 0
		_RotationZ ("RotationZ", Range(0, 360)) = 0
    }

    Subshader {
        Pass {
            ZTest Always Cull Off ZWrite Off
            Fog { Mode off }      

            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma fragmentoption ARB_precision_hint_fastest
                //#pragma fragmentoption ARB_precision_hint_nicest
				//#pragma enable_d3d11_debug_symbols

                #include "UnityCG.cginc"

                #define PI    3.141592653589793
                #define TWOPI 6.283185307179587

                struct v2f {
                    float4 pos : POSITION;
                    float2 uv : TEXCOORD0;
                };

                samplerCUBE _MainTex;
				float _RotationX;
				float _RotationY;
				float _RotationZ;

				float4 RotateAroundX(float4 vertex, float radian)
				{
					float sina, cosa;
					sincos(radian, sina, cosa);

					float4x4 m;

					m[0] = float4(1, 0, 0, 0);
					m[1] = float4(0, cosa, -sina, 0);
					m[2] = float4(0, sina, cosa, 0);
					m[3] = float4(0, 0, 0, 1);

					return mul(m, vertex);
				}

				float4 RotateAroundY(float4 vertex, float radian)
				{
					float sina, cosa;
					sincos(radian, sina, cosa);

					float4x4 m;

					m[0] = float4(cosa, 0, sina, 0);
					m[1] = float4(0, 1, 0, 0);
					m[2] = float4(-sina, 0, cosa, 0);
					m[3] = float4(0, 0, 0, 1);

					return mul(m, vertex);
				}

				float4 RotateAroundZ(float4 vertex, float radian)
				{
					float sina, cosa;
					sincos(radian, sina, cosa);

					float4x4 m;

					m[0] = float4(cosa, -sina, 0, 0);
					m[1] = float4(sina, cosa, 0, 0);
					m[2] = float4(0, 0, 1, 0);
					m[3] = float4(0, 0, 0, 1);

					return mul(m, vertex);
				}
	

				float4x4 GetRotationMatrix(float xRadian, float yRadian, float zRadian)
				{
					float sina, cosa;
					sincos(xRadian, sina, cosa);

					float4x4 xMatrix;

					xMatrix[0] = float4(1, 0, 0, 0);
					xMatrix[1] = float4(0, cosa, -sina, 0);
					xMatrix[2] = float4(0, sina, cosa, 0);
					xMatrix[3] = float4(0, 0, 0, 1);

					sincos(yRadian, sina, cosa);

					float4x4 yMatrix;

					yMatrix[0] = float4(cosa, 0, sina, 0);
					yMatrix[1] = float4(0, 1, 0, 0);
					yMatrix[2] = float4(-sina, 0, cosa, 0);
					yMatrix[3] = float4(0, 0, 0, 1);

					sincos(zRadian, sina, cosa);

					float4x4 zMatrix;

					zMatrix[0] = float4(cosa, -sina, 0, 0);
					zMatrix[1] = float4(sina, cosa, 0, 0);
					zMatrix[2] = float4(0, 0, 1, 0);
					zMatrix[3] = float4(0, 0, 0, 1);

					return mul(mul(yMatrix, xMatrix), zMatrix);
				}

                v2f vert( appdata_img v )
                {
                    v2f o;
                    
					o.pos = UnityObjectToClipPos(v.vertex);
                    o.uv = v.texcoord.xy * float2(TWOPI, PI);
                    return o;
                }

                fixed4 frag(v2f i) : COLOR 
                {
                    float theta = i.uv.y;
                    float phi = i.uv.x;
                    float4 unit = float4(0,0,0,1);

                    unit.x = sin(phi) * sin(theta) * -1;
                    unit.y = cos(theta) * -1;
                    unit.z = cos(phi) * sin(theta) * -1;

					float4x4 rotationMatrix = GetRotationMatrix(radians(_RotationX), radians(_RotationY), radians(_RotationZ));
					float4 unit_rot = mul(rotationMatrix, unit);

                    return texCUBE(_MainTex, float3(unit_rot.x, unit_rot.y, unit_rot.z));
                }

            ENDCG
        }
    }
    Fallback Off
}