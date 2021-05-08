Shader "DissolverShader/DissolveShaderUnlit" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_DissolveMap ("Dissolve Map", 2D) = "white" {}
		_DissolveAmount ("DissolveAmount", Range(-1,1)) = 0
		_DissolveColor ("DissolveColor", Color) = (1,1,1,1)
		_DissolveEmission ("DissolveEmission", Range(0,2)) = 1
		_DissolveWidth ("DissolveWidth", Range(0,0.2)) = 0.05
	}
	SubShader {
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
		LOD 200
		ZWrite Off

		CGPROGRAM
		#pragma surface surf Unlit alpha:fade nofog noshadow
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _DissolveMap;

		struct Input {
			float2 uv_MainTex;
			float2 uv_DissolveMap;
		};

		half _DissolveAmount;
		half _DissolveEmission;
		half _DissolveWidth;
		fixed4 _Color;
		fixed4 _DissolveColor;

		void surf (Input IN, inout SurfaceOutput o) {

			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;			
			fixed4 mask = tex2D (_DissolveMap, IN.uv_DissolveMap);

			if(mask.r < _DissolveAmount)
				discard;

			o.Albedo = c.rgb;

			if(mask.r - _DissolveWidth < _DissolveAmount) {
				o.Albedo = _DissolveColor;
				o.Alpha = 1;
				o.Emission = _DissolveColor * _DissolveEmission;
			}
			else o.Alpha = c.a;
		}

		half4 LightingUnlit(SurfaceOutput s, half3 lightDir, half atten)
		{
			return half4(s.Albedo, s.Alpha);
		}
		ENDCG
	}
	FallBack "Unlit/Transparent"
}
