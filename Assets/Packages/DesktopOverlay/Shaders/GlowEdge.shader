// DesktopOverlay/GlowEdge
//
// Unlit, alpha-blended shader for monitor-edge glow quads.
//
// Properties controlled at runtime by MonitorEdgeGlow / EdgeGlowController:
//   _Color     – base glow colour (RGBA)
//   _Intensity – colour multiplier (boosts brightness above 1 for HDR-like look)
//   _Alpha     – master opacity (0 = invisible, 1 = full glow)
//
// The fragment shader applies a smooth falloff across the width of the quad
// (the short dimension) so the glow is brightest at the centre of the strip
// and fades to transparent at both long edges.  A gentler falloff along the
// length prevents hard clipping at the ends.
//
// Rendering notes:
//   - Queue = Transparent+1 so it draws above normal transparent objects.
//   - ZWrite Off + ZTest Always to ensure the glow always shows regardless of
//     what else is in the scene (there should be nothing else in an overlay).
//   - Blend SrcAlpha OneMinusSrcAlpha for correct DWM per-pixel alpha
//     compositing (the window background is cleared to RGBA 0,0,0,0).

Shader "DesktopOverlay/GlowEdge"
{
    Properties
    {
        _Color     ("Glow Color",   Color)          = (0.25, 0.65, 1, 1)
        _Intensity ("Intensity",    Float)          = 2.0
        _Alpha     ("Alpha",        Range(0, 1))    = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue"      = "Transparent+1"
        }

        Blend    SrcAlpha OneMinusSrcAlpha
        ZWrite   Off
        ZTest    Always
        Cull     Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            fixed4 _Color;
            float  _Intensity;
            float  _Alpha;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Remap UV to [-1, 1] in each axis.
                float2 c = abs(i.uv * 2.0 - 1.0);  // 0 at centre, 1 at edge

                // Primary falloff: across the strip's narrow dimension (U axis).
                // pow(..., 1.5) gives a smooth bell-curve without a hard centre.
                float falloffU = pow(saturate(1.0 - c.x), 1.5);

                // Secondary falloff: along the strip's long dimension (V axis).
                // Multiplied by a large factor so it only darkens the very ends.
                float falloffV = saturate(1.0 - c.y * 0.6);

                float alpha = _Alpha * falloffU * falloffV;

                return fixed4(_Color.rgb * _Intensity, alpha * _Color.a);
            }
            ENDCG
        }
    }

    // No fallback — if the shader is missing the glow simply won't render.
    FallBack Off
}
