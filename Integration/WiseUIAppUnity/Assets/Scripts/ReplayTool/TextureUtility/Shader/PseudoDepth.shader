Shader "Hidden/PseudoDepth"
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
                o.uv.w = distance(_WorldSpaceCameraPos, mul(unity_ObjectToWorld, v.vertex)); // depth
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float depth_val = i.uv.w;
				
 				float maxPerChannel = pow(_ProjectionParams.z,  1/3.0f);

				float b = depth_val % maxPerChannel;
 				float g = (depth_val / maxPerChannel) % maxPerChannel;
 				float r = (depth_val / maxPerChannel / maxPerChannel) % maxPerChannel;

				fixed4 color =  fixed4( r / maxPerChannel, g / maxPerChannel, b / maxPerChannel, 1.0 );
				return color;
            }
            ENDCG
        }
    }
}