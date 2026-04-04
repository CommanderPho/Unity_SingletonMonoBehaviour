Shader "Overlay/GlowEffect"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0, 1, 1, 1)
        _Intensity("Intensity", Float) = 0
        _Softness("Softness", Float) = 0.3
        [Enum(Left,0,Right,1,Top,2,Bottom,3)] _EdgeDirection ("Edge Direction", Int) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Intensity;
                float _Softness;
                int _EdgeDirection;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.position = TransformObjectToHClip(input.position.xyz);
                output.uv = input.uv;
                return output;
            }

            float GetEdgeGlow(float2 uv, int direction)
            {
                float glow;
                if (direction == 0)
                {
                    glow = 1.0 - uv.x;
                }
                else if (direction == 1)
                {
                    glow = uv.x;
                }
                else if (direction == 2)
                {
                    glow = uv.y;
                }
                else
                {
                    glow = 1.0 - uv.y;
                }

                glow = smoothstep(0.0, _Softness, glow);
                glow = pow(glow, 1.5);
                return glow;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float glow = GetEdgeGlow(input.uv, _EdgeDirection);
                half alpha = glow * _Intensity;
                return half4(_BaseColor.rgb * glow, alpha);
            }
            ENDHLSL
        }
    }
}
