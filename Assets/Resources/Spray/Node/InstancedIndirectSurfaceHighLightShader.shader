Shader "Instanced/HighLight" 
{
	Properties{
		_MainTex("Albedo (RGB)", 2D) = "white" {}
	_Glossiness("Smoothness", Range(0,1)) = 1.0
		_Metallic("Metallic", Range(0,1)) = 1.0
	}
		SubShader{
		Tags{ "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model
#pragma surface surf Standard noshadow
#pragma multi_compile_instancing
#pragma instancing_options procedural:setup

		sampler2D _MainTex;

	struct Input {
		float2 uv_MainTex;
	};

	struct Node
	{
		int id;
		float size;
		float3 pos;
		float4 color;
	};

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
	StructuredBuffer<Node> nodeBuffer;
#endif

	void setup()
	{
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
		float3 pos = nodeBuffer[unity_InstanceID].pos;
		float size = nodeBuffer[unity_InstanceID].size * 2.5;

		unity_ObjectToWorld._11_21_31_41 = float4(size, 0, 0, 0);
		unity_ObjectToWorld._12_22_32_42 = float4(0, size, 0, 0);
		unity_ObjectToWorld._13_23_33_43 = float4(0, 0, size, 0);
		unity_ObjectToWorld._14_24_34_44 = float4(pos.xyz, 1);
		unity_WorldToObject = unity_ObjectToWorld;
		unity_WorldToObject._14_24_34 *= -1;
		unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;
#endif
	}

	half _Glossiness;
	half _Metallic;

	void surf(Input IN, inout SurfaceOutputStandard o) 
	{
		float4 col = 1.0f;

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
		col = nodeBuffer[unity_InstanceID].color;
#else
		col = float4(0, 0, 1, 1);
#endif

		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * col;
		o.Albedo = c.rgb;
		o.Metallic = _Metallic;
		o.Smoothness = _Glossiness;
		o.Alpha = c.a;
		o.Emission = c.rgb;
	}
	ENDCG
	}
		FallBack "Diffuse"
}
