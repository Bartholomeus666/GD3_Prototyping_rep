Shader "Custom/AdaptiveSplitScreen"
{
    Properties
    {
        _CamTex1    ("Camera 1 Texture", 2D)              = "black" {}
        _CamTex2    ("Camera 2 Texture", 2D)              = "black" {}
        _SplitNormal("Split Normal (UV space)", Vector)   = (1, 0, 0, 0)
        _SplitCenter("Split Center (UV space)", Vector)   = (0.5, 0.5, 0, 0)
        _LineWidth  ("Line Width", Float)                 = 0.008
        _LineColor  ("Line Color", Color)                 = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Overlay"
        }

        ZWrite Off
        ZTest  Always
        Cull   Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            TEXTURE2D(_CamTex1); SAMPLER(sampler_CamTex1);
            TEXTURE2D(_CamTex2); SAMPLER(sampler_CamTex2);

            float2 _SplitNormal;
            float2 _SplitCenter;
            float  _LineWidth;
            float4 _LineColor;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // Flip Y — Unity RenderTextures are inverted on OpenGL-based platforms
                #if UNITY_UV_STARTS_AT_TOP
                    float2 camUV = float2(uv.x, uv.y);
                #else
                    float2 camUV = float2(uv.x, 1.0 - uv.y);
                #endif

                // Signed distance from split line
                float dist = dot(uv - _SplitCenter, _SplitNormal);

                // Draw dividing line
                if (abs(dist) < _LineWidth * 0.5)
                    return _LineColor;

                // Sample the correct camera's render texture
                if (dist >= 0.0)
                    return SAMPLE_TEXTURE2D(_CamTex1, sampler_CamTex1, camUV);
                else
                    return SAMPLE_TEXTURE2D(_CamTex2, sampler_CamTex2, camUV);
            }
            ENDHLSL
        }
    }
}
