// Swarm - Special renderer that draws a swarm of swirling/crawling lines.
// https://github.com/keijiro/Swarm
Shader "Curves/TubeO"
{
    Properties
    {
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
        _Metallic("Metallic", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
        CGPROGRAM

        #pragma surface surf Standard vertex:vert noshadow
        #pragma instancing_options procedural:setup

        struct Input
        {
            float4 color : COLOR;
        };

        half _Smoothness;
        half _Metallic;

		float4x4 _LocalToWorld;
		float4x4 _WorldToLocal;

        float _Radius;

        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED

        StructuredBuffer<float4> _TangentBuffer;
        StructuredBuffer<float4> _NormalBuffer;
		StructuredBuffer<float4> _ColorBuffer;
		StructuredBuffer<float3> _CurvePointsBuffer;

        uint _InstanceCount;
        uint _HistoryLength;

        #endif

        void vert(inout appdata_full v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED

            float phi = v.vertex.x; // Angle in slice
            float cap = v.vertex.y; // -1:head, +1:tail
            float seg = v.vertex.z; // Segment index

            // Index of the current slice in the buffers.
            uint idx = unity_InstanceID;
            idx += _InstanceCount * seg;

            float3 p = _CurvePointsBuffer[idx].xyz; // Position
            float3 t = _TangentBuffer[idx].xyz;		// Curve-TNB: Tangent 
            float3 n = _NormalBuffer[idx].xyz;		// Curve-TNB: Normal
            float3 b = cross(t, n);					// Curve-TNB: Binormal

            float3 normal = n * cos(phi) + b * sin(phi); // Surface normal

			float3 vcolor = _ColorBuffer[idx];

            // Feedback the results.
            v.vertex = float4(p + normal * _Radius * (1 - abs(cap)), 1);
            v.normal = normal * (1 - abs(cap)) + n * cap;
            v.color.r = vcolor.x;
			v.color.g = vcolor.y;
			v.color.b = vcolor.z;

            #endif
        }

        void setup()
        {
			unity_ObjectToWorld = _LocalToWorld;
			unity_WorldToObject = _WorldToLocal;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
			o.Albedo.r = IN.color.r;
			o.Albedo.g = IN.color.g;
			o.Albedo.b = IN.color.b;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
        }

        ENDCG
    }
    FallBack "Diffuse"
}
