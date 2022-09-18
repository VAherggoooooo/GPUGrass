Shader "GPUInstance/Grass"
{
     Properties
    {
        [MainColor] _BaseColor ("BaseColor", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("BaseMap", 2D) = "white" {}
        _Rota ("Rota", float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline"}

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #pragma multi_compile_instancing
        #pragma instancing_options procedural:setup
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float _Rota;
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST;

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                StructuredBuffer<float3> positionBuffer;        
            #endif
        CBUFFER_END

        //>>>>>>>>>>>>>>>>>>>> math function <<<<<<<<<<<<<<<<<<<<<<<<<<<<<
        inline float3 Rotate(float angle, float3 ori, float3 c)
        {
            float a = angle / 180.0 * 3.1415926;
            ori -= c;
            float3x3 r = float3x3
            (
                float3(cos(a), 0, sin(a)),
                float3(0, 1, 0),
                float3(-sin(a), 0, cos(a))
            );
            float3 res = mul(r, ori);
            res += c;
            return res;
        }
        float hash(float2 p)
        {
            return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
        }
        //>>>>>>>>>>>>>>>>>>>> end <<<<<<<<<<<<<<<<<<<<<<<<<<<<<

        ENDHLSL


        Pass
        {
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            struct Attributes
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionHCS  : SV_POSITION;
                float3 wPos         : TEXCOORD1;
            };

            void setup()
            {
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    float3 data = positionBuffer[unity_InstanceID];                                     
                    //需要手动设置两个矩阵
                    unity_ObjectToWorld._11_21_31_41 = float4(1, 0, 0, 0);
                    unity_ObjectToWorld._12_22_32_42 = float4(0, 1, 0, 0);
                    unity_ObjectToWorld._13_23_33_43 = float4(0, 0, 1, 0);
                    unity_ObjectToWorld._14_24_34_44 = float4(data.xyz, 1);
                    unity_WorldToObject = unity_ObjectToWorld;
                    unity_WorldToObject._14_24_34 *= -1;
                    unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;
                #endif
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
            
                float3 worldC = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                IN.positionOS.xz += sin(_Time.y * 0.1 + worldC.xz * 0.2) * 0.2 * IN.uv.y;
                float rotation = hash(worldC.xz) * 50;
                IN.positionOS.xyz = Rotate(_Rota * rotation, IN.positionOS.xyz, 0);
                IN.positionOS.xyz *= clamp(hash(worldC.xz) * 1.2, 0.9, 1.15);     

                OUT.wPos = mul(unity_ObjectToWorld, IN.positionOS).xyz;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 texCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.wPos.xz * _BaseMap_ST.xy + _BaseMap_ST.zw);
                return texCol * _BaseColor;
            }
            ENDHLSL
        }        
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"   
    }
}
