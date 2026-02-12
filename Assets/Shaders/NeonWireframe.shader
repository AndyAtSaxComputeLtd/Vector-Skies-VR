Shader "VectorSkies/NeonWireframe"
{
    Properties
    {
        _Color ("Neon Color", Color) = (0, 1, 1, 1)
        _EmissionIntensity ("Emission Intensity", Range(0, 10)) = 2.0
        _BloomIntensity ("Bloom Intensity", Range(0, 5)) = 1.5
        _Thickness ("Line Thickness", Range(0.01, 0.5)) = 0.1
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        LOD 100
        Blend One One // Additive blending for glow
        ZWrite Off
        Cull Off
        
        Pass
        {
            Name "NeonWireframePass"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 4.5
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
                float3 worldPos : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _EmissionIntensity;
                float _BloomIntensity;
                float _Thickness;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.color = input.color;
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                // Use vertex color multiplied by material color
                half4 finalColor = input.color * _Color;
                
                // Apply emission for neon glow
                finalColor.rgb *= _EmissionIntensity;
                
                // Distance-based intensity fade for depth perception
                float distanceToCamera = length(_WorldSpaceCameraPos - input.worldPos);
                float fadeFactor = saturate(1.0 - (distanceToCamera / 200.0)); // Fade after 200 units
                
                finalColor.rgb *= fadeFactor;
                
                // Bloom boost
                finalColor.rgb *= _BloomIntensity;
                
                return finalColor;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
