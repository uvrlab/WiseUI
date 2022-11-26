// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Post Outline"
{
    Properties
    {
		_NumberOfIterations ("Metallic",Float) = 10
        _MainTex("Main Texture",2D)="white"{}
		_OutlineColor("Color", Color) = (0,1,1,1)
    }
    SubShader 
    {
    Blend SrcAlpha OneMinusSrcAlpha
        Pass 
        {
            CGPROGRAM
     
            sampler2D _MainTex;
			float _NumberOfIterations;
			fixed4 _OutlineColor;
            //<SamplerName>_TexelSize is a float2 that says how much screen space a texel occupies.
            float2 _MainTex_TexelSize;
 
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
             
            struct v2f 
            {
                float4 pos : SV_POSITION;
                float2 uvs : TEXCOORD0;
            };
             
            v2f vert (appdata_base v) 
            {
                v2f o;
                 
                //Despite the fact that we are only drawing a quad to the screen, Unity requires us to multiply vertices by our MVP matrix, presumably to keep things working when inexperienced people try copying code from other shaders.
                o.pos = UnityObjectToClipPos(v.vertex);
                //Also, we need to fix the UVs to match our screen space coordinates. There is a Unity define for this that should normally be used.

                o.uvs = o.pos.xy / 2 + 0.5;
				o.uvs.y = 1 - o.uvs.y;

                return o;
            }
             
             
            half4 frag(v2f i) : COLOR 
            {
                //arbitrary number of iterations for now
 
                //split texel size into smaller words
                float TX_x=_MainTex_TexelSize.x;
                float TX_y=_MainTex_TexelSize.y;
 
                //and a final intensity that increments based on surrounding intensities.
                float ColorIntensityInRadius;

                //if something already exists underneath the fragment, discard the fragment.
                if(tex2D(_MainTex,i.uvs.xy).r>0 || tex2D(_MainTex,i.uvs.xy).g>0 || tex2D(_MainTex,i.uvs.xy).b>0)
                {
                    discard;
                }

				//i.uvs[1] = TX_y - i.uvs[1];
 
                //for every iteration we need to do horizontally
                for(int k=0;k<_NumberOfIterations;k+=1)
                {
                    //for every iteration we need to do vertically
                    for(int j=0;j<_NumberOfIterations;j+=1)
                    {
                        //increase our output color by the pixels in the area
                        ColorIntensityInRadius+=tex2D(_MainTex, i.uvs.xy+float2
                                                                   (
                                                                        (k-_NumberOfIterations/2)*TX_x,
                                                                        (j-_NumberOfIterations/2)*TX_y
                                                                   )
                                                     );
                    }
                }
 
                //output some intensity of teal
                return ColorIntensityInRadius*_OutlineColor;
            }
             
            ENDCG
 
        }
        //end pass        
    }
    //end subshader
}
//end shader