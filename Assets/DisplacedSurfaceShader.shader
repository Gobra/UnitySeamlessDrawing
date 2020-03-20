Shader "Custom/DisplacedSurfaceShader"
{
	Properties
	{
		_Tess("Tessellation", Range(1, 64)) = 8
		_Splatmap("Splatmap", 2D) = "black" {}
		_Displacement("Displacement", Float) = 0.3
		_RegularColor("Regular Color", Color) = (1,1,1,1)
		_RegularTex("Snow (RGB)", 2D) = "white" {}
		_PressedColor("Pressed Color", Color) = (1,1,1,1)
		_PressedTex("Ground (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" } 
		LOD 200

		CGPROGRAM

		#pragma target 4.6

		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surfaceShader Standard fullforwardshadows vertex:vertexDisplacement tessellate:tessallateWithDistance

		// includes
		#include "Tessellation.cginc"
		#include "UnityCG.cginc"

		struct appdata
		{
			float4 vertex : POSITION;
			float4 tangent : TANGENT;
			float3 normal : NORMAL;
			float2 texcoord : TEXCOORD0;
			float2 texcoord1 : TEXCOORD1;
			float2 texcoord2 : TEXCOORD2;
		};

		struct Input
		{
			float2 uv_RegularTex;
			float2 uv_PressedTex;
			float2 uv_Splatmap;
		};

		// 1. Tessallation
		float _Tess;

		float4 tessallateWithDistance(appdata v0, appdata v1, appdata v2)
		{
			float minDist = 8.0;
			float maxDist = 256.0;
			return UnityDistanceBasedTess(v0.vertex, v1.vertex, v2.vertex, minDist, maxDist, _Tess);
		}

		// 2. Vertex shader
		sampler2D _Splatmap;
		float _Displacement;

		void vertexDisplacement(inout appdata v)
		{
			float d = tex2Dlod(_Splatmap, float4(v.texcoord, 0, 0)).r * _Displacement;
			v.vertex.xyz -= v.normal * d;
			v.vertex.xyz += v.normal * _Displacement;
		}

		// 3. Pixel shader
		sampler2D _RegularTex;
		fixed4 _RegularColor;
		sampler2D _PressedTex;
		fixed4 _PressedColor;

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surfaceShader(Input IN, inout SurfaceOutputStandard o)
		{
			half amount = tex2D(_Splatmap, IN.uv_Splatmap).r;

			// Albedo comes from a texture tinted by color
			fixed4 snow = tex2D(_RegularTex, IN.uv_RegularTex) * _RegularColor;
			fixed4 ground = tex2D(_PressedTex, IN.uv_PressedTex) * _PressedColor;
			fixed4 c = lerp(snow, ground, amount);

			o.Albedo = c.rgb;
			//o.Albedo = float3(IN.uv_Splatmap.x, IN.uv_Splatmap.x, IN.uv_Splatmap.x);

			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}

		ENDCG
	}

	FallBack "Diffuse"
}