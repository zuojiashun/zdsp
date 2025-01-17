// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Splatmap/Splatmap12Unlit"
{
	Properties
	{
		_SplatMap01 ("Splat Map 01", 2D) = "black" {}
		_SplatMap02 ("Splat Map 02", 2D) = "black" {}
		_SplatMap03 ("Splat Map 03", 2D) = "black" {}
		_Albedo01 ("Albedo 01", 2D) = "white" {}
		_Albedo02 ("Albedo 02", 2D) = "white" {}
		_Albedo03 ("Albedo 03", 2D) = "white" {}
		_Albedo04 ("Albedo 04", 2D) = "white" {}
		_Albedo05 ("Albedo 05", 2D) = "white" {}
		_Albedo06 ("Albedo 06", 2D) = "white" {}
		_Albedo07 ("Albedo 07", 2D) = "white" {}
		_Albedo08 ("Albedo 08", 2D) = "white" {}
		_Albedo09 ("Albedo 09", 2D) = "white" {}
		_Albedo10 ("Albedo 10", 2D) = "white" {}
		_Albedo11 ("Albedo 11", 2D) = "white" {}
		_Albedo12 ("Albedo 12", 2D) = "white" {}
	}

	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#include "UnityCG.cginc"

			sampler2D _SplatMap01, _SplatMap02, _SplatMap03;
			float4 _SplatMap01_ST, _SplatMap02_ST, _SplatMap03_ST;
			sampler2D _Albedo01, _Albedo02, _Albedo03, _Albedo04, _Albedo05, _Albedo06, _Albedo07, _Albedo08, _Albedo09, _Albedo10, _Albedo11, _Albedo12;
			float4 _Albedo01_ST, _Albedo02_ST, _Albedo03_ST, _Albedo04_ST, _Albedo05_ST, _Albedo06_ST, _Albedo07_ST, _Albedo08_ST, _Albedo09_ST, _Albedo10_ST, _Albedo11_ST, _Albedo12_ST;

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv0 : TEXCOORD0;
			};

			v2f vert(appdata_base v)
			{
				v2f o;
				o.uv0 = v.texcoord.xy;
				o.pos = UnityObjectToClipPos(v.vertex);
				return o;
			}

			fixed3 frag(v2f i) : COLOR
			{
				fixed4 splatMap01 = tex2D(_SplatMap01, TRANSFORM_TEX(i.uv0, _SplatMap01)).rgba;
				fixed4 splatMap02 = tex2D(_SplatMap02, TRANSFORM_TEX(i.uv0, _SplatMap02)).rgba;
				fixed4 splatMap03 = tex2D(_SplatMap03, TRANSFORM_TEX(i.uv0, _SplatMap03)).rgba;
				fixed3 albedo01 = tex2D(_Albedo01, TRANSFORM_TEX(i.uv0, _Albedo01)).rgb;
				fixed3 albedo02 = tex2D(_Albedo02, TRANSFORM_TEX(i.uv0, _Albedo02)).rgb;
				fixed3 albedo03 = tex2D(_Albedo03, TRANSFORM_TEX(i.uv0, _Albedo03)).rgb;
				fixed3 albedo04 = tex2D(_Albedo04, TRANSFORM_TEX(i.uv0, _Albedo04)).rgb;
				fixed3 albedo05 = tex2D(_Albedo05, TRANSFORM_TEX(i.uv0, _Albedo05)).rgb;
				fixed3 albedo06 = tex2D(_Albedo06, TRANSFORM_TEX(i.uv0, _Albedo06)).rgb;
				fixed3 albedo07 = tex2D(_Albedo07, TRANSFORM_TEX(i.uv0, _Albedo07)).rgb;
				fixed3 albedo08 = tex2D(_Albedo08, TRANSFORM_TEX(i.uv0, _Albedo08)).rgb;
				fixed3 albedo09 = tex2D(_Albedo09, TRANSFORM_TEX(i.uv0, _Albedo09)).rgb;
				fixed3 albedo10 = tex2D(_Albedo10, TRANSFORM_TEX(i.uv0, _Albedo10)).rgb;
				fixed3 albedo11 = tex2D(_Albedo11, TRANSFORM_TEX(i.uv0, _Albedo11)).rgb;
				fixed3 albedo12 = tex2D(_Albedo12, TRANSFORM_TEX(i.uv0, _Albedo12)).rgb;

				return splatMap01.r * albedo01.rgb
					+ splatMap01.g * albedo02.rgb
					+ splatMap01.b * albedo03.rgb
					+ splatMap01.a * albedo04.rgb
					+ splatMap02.r * albedo05.rgb
					+ splatMap02.g * albedo06.rgb
					+ splatMap02.b * albedo07.rgb
					+ splatMap02.a * albedo08.rgb
					+ splatMap03.r * albedo09.rgb
					+ splatMap03.g * albedo10.rgb
					+ splatMap03.b * albedo11.rgb
					+ splatMap03.a * albedo12.rgb;
			}
		
			ENDCG
		}
	}
	
	FallBack "Diffuse"
}
