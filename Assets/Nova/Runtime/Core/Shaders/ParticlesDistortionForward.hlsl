#ifndef NOVA_PARTICLESDISTORTIONFORWARD_INCLUDED
#define NOVA_PARTICLESDISTORTIONFORWARD_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "ParticlesDistortion.hlsl"

Varyings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    SETUP_VERTEX;
    SETUP_CUSTOM_COORD(input)
    TRANSFER_CUSTOM_COORD(input, output);

    output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
    output.projectedPosition = ComputeScreenPos(output.positionHCS);

    float2 baseMapUv = input.texcoord.xy;
    #ifdef _BASE_MAP_ROTATION_ENABLED
    half angle = _BaseMapRotation + GET_CUSTOM_COORD(_BaseMapRotationCoord)
    baseMapUv = RotateUV(baseMapUv, angle * PI * 2, _BaseMapRotationOffsets.xy);
    #endif

    baseMapUv.xy = TRANSFORM_TEX(baseMapUv, _BaseMap);
    baseMapUv.x += GET_CUSTOM_COORD(_BaseMapOffsetXCoord);
    baseMapUv.y += GET_CUSTOM_COORD(_BaseMapOffsetYCoord);
    output.baseUv.xy = baseMapUv;

    #ifdef _FLOW_MAP_ENABLED
    output.flowTransitionUVs.xy = TRANSFORM_TEX(input.texcoord.xy, _FlowMap);
    output.flowTransitionUVs.x += GET_CUSTOM_COORD(_FlowMapOffsetXCoord);
    output.flowTransitionUVs.y += GET_CUSTOM_COORD(_FlowMapOffsetYCoord);
    #endif

    #if defined(_FADE_TRANSITION_ENABLED) || defined(_DISSOLVE_TRANSITION_ENABLED)
    output.flowTransitionUVs.zw = TRANSFORM_TEX(input.texcoord.xy, _AlphaTransitionMap);
    output.flowTransitionUVs.z += GET_CUSTOM_COORD(_AlphaTransitionMapOffsetXCoord)
    output.flowTransitionUVs.w += GET_CUSTOM_COORD(_AlphaTransitionMapOffsetYCoord)
    #endif

    return output;
}

half4 frag(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    SETUP_FRAGMENT;
    SETUP_CUSTOM_COORD(input);

    #ifdef _FLOW_MAP_ENABLED
    half2 flow = SAMPLE_TEXTURE2D(_FlowMap, sampler_FlowMap, input.flowTransitionUVs.xy).xy;
    flow = flow * 2 - 1;
    flow *= _FlowIntensity + GET_CUSTOM_COORD(_FlowIntensityCoord);
    input.baseUv.xy += flow;
    #endif

    SamplerState baseMapSamplerState;
    #ifdef BASE_SAMPLER_STATE_OVERRIDE_ENABLED
    baseMapSamplerState = BASE_SAMPLER_STATE_NAME;
    #else
    baseMapSamplerState = sampler_BaseMap;
    #endif

    half2 distortion = SAMPLE_TEXTURE2D(_BaseMap, baseMapSamplerState, input.baseUv.xy).xy;
    distortion = distortion * 2.0 - 1.0;
    half distortionIntensity = _DistortionIntensity + GET_CUSTOM_COORD(_DistortionIntensityCoord)
    distortion *= 0.1 * distortionIntensity;

    #if defined(_FADE_TRANSITION_ENABLED) || defined(_DISSOLVE_TRANSITION_ENABLED)
    half transitionAlpha = SAMPLE_TEXTURE2D(_AlphaTransitionMap, sampler_AlphaTransitionMap, input.flowTransitionUVs.zw).x;
    half progress = _AlphaTransitionProgress + GET_CUSTOM_COORD(_AlphaTransitionProgressCoord);
    #if _VERTEX_ALPHA_AS_TRANSITION_PROGRESS
    progress += 1.0 - input.color.a;
    #endif
    progress = min(1.0, progress);

    #ifdef _FADE_TRANSITION_ENABLED
    progress = (progress * 2 - 1) * -1;
    transitionAlpha += progress;
    transitionAlpha = saturate(transitionAlpha);
    #elif _DISSOLVE_TRANSITION_ENABLED
    half dissolveWidth = lerp(0.5, 0.0001, _DissolveSharpness);
    progress = lerp(-dissolveWidth, 1.0 + dissolveWidth, progress);
    transitionAlpha = smoothstep(progress - dissolveWidth, progress + dissolveWidth, transitionAlpha);
    #endif
    distortion *= transitionAlpha;
    #endif

    #ifdef _SOFT_PARTICLES_ENABLED
    distortion *= SoftParticles(input.projectedPosition, _SoftParticlesIntensity);
    #endif

    #ifdef _DEPTH_FADE_ENABLED
    distortion *= DepthFade(_DepthFadeNear, _DepthFadeFar, _DepthFadeWidth, input.projectedPosition);
    #endif

    return half4(distortion, 0, 1);
}

#endif
