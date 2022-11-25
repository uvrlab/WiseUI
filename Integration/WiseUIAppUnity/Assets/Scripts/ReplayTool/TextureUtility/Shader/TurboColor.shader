Shader "Hidden/TurboColor"
{
    Properties
    {
        _MainTex("", 2D) = "white" {}
        _Cutoff("", Float) = 0.5
        _Color("", Color) = (1,1,1,1)
    }
    SubShader
    {
        CGINCLUDE
 
        inline float Linear01FromEyeToLinear01FromNear(float depth01)
        {
            float near = _ProjectionParams.y;
            float far = _ProjectionParams.z;
            return (depth01 - near / far) * (1 + near / far);
        }
 
        ENDCG
 
        Tags{ "RenderType" = "Opaque" }
 
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
 
            #include "UnityCG.cginc"
 
            struct v2f
            {
                float4 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
 
            v2f vert(appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				//o.uv.w = COMPUTE_DEPTH_01;        // depth
                o.uv.w = distance(_WorldSpaceCameraPos, mul(unity_ObjectToWorld, v.vertex));//COMPUTE_DEPTH_01;        // depth
                return o;
            }
			
			fixed4 TurboColormap(float x) 
			{
				const float4 kRedVec4 = float4(0.13572138, 4.61539260, -42.66032258, 132.13108234);
				const float4 kGreenVec4 = float4(0.09140261, 2.19418839, 4.84296658, -14.18503333);
				const float4 kBlueVec4 = float4(0.10667330, 12.64194608, -60.58204836, 110.36276771);
				const float2 kRedVec2 = float2(-152.94239396, 59.28637943);
				const float2 kGreenVec2 = float2(4.27729857, 2.82956604);
				const float2 kBlueVec2 = float2(-89.90310912, 27.34824973);
  
				x = saturate(x);
				float4 v4 = float4( 1.0, x, x * x, x * x * x);
				float2 v2 = float2(v4[2], v4[3]) * v4[2];// v4.zw * v4.z;

				return 
				fixed4(
				dot(v4, kRedVec4)   + dot(v2, kRedVec2),
				dot(v4, kGreenVec4) + dot(v2, kGreenVec2),
				dot(v4, kBlueVec4)  + dot(v2, kBlueVec2), 1);
			}
			
            fixed4 frag(v2f i) : SV_Target
            {
			
				float depth_val = i.uv.w / _ProjectionParams.z;
				return TurboColormap(1-depth_val);
                //float linearZFromNear = Linear01FromEyeToLinear01FromNear(i.uv.w);
                //float k = 0.25; // compression factor
                //return pow(linearZFromNear, k);
            }
            ENDCG
        }
    }
}