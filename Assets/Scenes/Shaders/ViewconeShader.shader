Shader "Unlit/ViewconeShader"
{
    Properties
    {
		_Color("Color", Color) = (0,0,0,0)
        _Distance("Distance", Range(1.0,30.0)) = 10.0
    }
    SubShader
    {
		
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
		Blend SrcAlpha OneMinusSrcAlpha	
		//AlphaToMask On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
				float dist : TEXCOORD0;
            };

			float4 _Color;
            float _Distance;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.dist = v.vertex.z;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
				fixed4 col = _Color ;
				//col.a = 0.2f;
				col.a = 1.f - i.dist / _Distance;
                return col;
            }
            ENDCG
        }
    }
}
