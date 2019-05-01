﻿Shader "MyProcedural" {
    
    Properties {
        _Color("Main Color", Color) = (1,1,1,1)
    }
    
    SubShader {

        Tags { "LightMode" = "ForwardBase" }

        Pass {
            CGPROGRAM
            #include "UnityCG.cginc"
            #pragma target 5.0  
            #pragma vertex vert
            #pragma fragment frag

            struct Point {
                float3 vertex;
                float3 normal;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float4 col : COLOR;
            };

            uniform fixed4 _LightColor0;
            float4 _Color;
            
            StructuredBuffer<Point> points;
            StructuredBuffer<float3> offsets;
            StructuredBuffer<float3> colors;

            v2f vert(uint id : SV_VertexID, uint inst : SV_InstanceID)
            {
                v2f o;

                // Position
                float4 pos = float4(points[id].vertex + offsets[inst], 1.0f);
                float4 nor = float4(points[id].normal, 1.0f);
                o.pos = UnityObjectToClipPos(pos);

                // Lighting
                float3 normalDirection = normalize(nor.xyz);
                float4 AmbientLight = UNITY_LIGHTMODEL_AMBIENT;
                float4 LightDirection = normalize(_WorldSpaceLightPos0);
                float4 DiffuseLight = saturate(dot(LightDirection, normalDirection))*_LightColor0;
                o.col = float4(AmbientLight + DiffuseLight) * float4(colors[inst], 1.0f);
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 final = _Color;
                final *= i.col;
                return final;
            }

            ENDCG
        }
    }
}