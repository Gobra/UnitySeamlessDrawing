Shader "Unlit/BrushShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Coordinates("Coordinates in UV space", Vector) = (0,0,0,0)
		_Size("Size in UV space", Float) = 1
		_Smooth("Smooth", Float) = 2
		_Strength("Strength", Range(0,1)) = 1
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }

		Pass
		{
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
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _Coordinates; 
			float _Size;
			float _Smooth;
			float _Strength;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			float frag (v2f i) : SV_Target
			{
				float absoluteDistance = distance(i.uv, _Coordinates.xy);
				float draw = pow(saturate(1 - absoluteDistance / _Size), _Smooth);
				float stroke = draw * _Strength;

				float old = tex2D(_MainTex, i.uv);
				return saturate(old + stroke);
			}
			ENDCG
		}
	}
}
