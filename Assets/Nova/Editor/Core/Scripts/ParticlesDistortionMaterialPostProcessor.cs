// --------------------------------------------------------------
// Copyright 2021 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using UnityEngine;

namespace Nova.Editor.Core.Scripts
{
    /// <summary>
    ///     Perform processing after properties of the material with the PerticlesDistortion shader have been changed.
    /// </summary>
    public static class ParticlesDistortionMaterialPostProcessor
    {
        private static readonly int BaseMapMirrorSamplingId =
            Shader.PropertyToID(MaterialPropertyNames.BaseMapMirrorSampling);

        private static readonly int BaseMapId = Shader.PropertyToID(MaterialPropertyNames.BaseMap);
        private static readonly int BaseMapRotationId = Shader.PropertyToID(MaterialPropertyNames.BaseMapRotation);

        private static readonly int BaseMapRotationCoordId =
            Shader.PropertyToID(MaterialPropertyNames.BaseMapRotationCoord);

        private static readonly int FlowMapId = Shader.PropertyToID(MaterialPropertyNames.FlowMap);
        private static readonly int FlowIntensityId = Shader.PropertyToID(MaterialPropertyNames.FlowIntensity);

        private static readonly int FlowIntensityCoordId =
            Shader.PropertyToID(MaterialPropertyNames.FlowIntensityCoord);

        private static readonly int AlphaTransitionMapId =
            Shader.PropertyToID(MaterialPropertyNames.AlphaTransitionMap);

        private static readonly int AlphaTransitionProgressCoordId =
            Shader.PropertyToID(MaterialPropertyNames.AlphaTransitionProgressCoord);

        private static readonly int AlphaTransitionModeId =
            Shader.PropertyToID(MaterialPropertyNames.AlphaTransitionMode);

        private static readonly int SoftParticlesEnabledId =
            Shader.PropertyToID(MaterialPropertyNames.SoftParticlesEnabled);

        private static readonly int DepthFadeEnabledId = Shader.PropertyToID(MaterialPropertyNames.DepthFadeEnabled);

        public static void SetupMaterialKeywords(Material material)
        {
            SetupBaseColorMaterialKeywords(material);
            SetupAlphaTransitionMaterialKeywords(material);
            SetupTransparencyMaterialKeywords(material);
        }

        private static void SetupBaseColorMaterialKeywords(Material material)
        {
            MaterialEditorUtility.SetKeyword(material, ShaderKeywords.BaseSamplerStatePointMirror, false);
            MaterialEditorUtility.SetKeyword(material, ShaderKeywords.BaseSamplerStateLinearMirror, false);
            MaterialEditorUtility.SetKeyword(material, ShaderKeywords.BaseSamplerStateTrilinearMirror, false);
            if (material.GetFloat(BaseMapMirrorSamplingId) >= 0.5f)
            {
                var baseMap = material.GetTexture(BaseMapId);
                if (baseMap != null)
                {
                    switch (baseMap.filterMode)
                    {
                        case FilterMode.Point:
                            MaterialEditorUtility.SetKeyword(material, ShaderKeywords.BaseSamplerStatePointMirror,
                                true);
                            break;
                        case FilterMode.Bilinear:
                            MaterialEditorUtility.SetKeyword(material, ShaderKeywords.BaseSamplerStateLinearMirror,
                                true);
                            break;
                        case FilterMode.Trilinear:
                            MaterialEditorUtility.SetKeyword(material, ShaderKeywords.BaseSamplerStateTrilinearMirror,
                                true);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            var baseMapRotationEnabled = material.GetFloat(BaseMapRotationId) != 0
                                         || (CustomCoord)material.GetFloat(
                                             BaseMapRotationCoordId) !=
                                         CustomCoord.Unused;
            MaterialEditorUtility.SetKeyword(material, ShaderKeywords.BaseMapRotationEnabled, baseMapRotationEnabled);

            var flowMapEnabled = material.GetTexture(FlowMapId) != null
                                 && (material.GetFloat(FlowIntensityId) != 0 ||
                                     (CustomCoord)material.GetFloat(FlowIntensityCoordId) !=
                                     CustomCoord.Unused);
            MaterialEditorUtility.SetKeyword(material, ShaderKeywords.FlowMapEnabled, flowMapEnabled);
        }

        private static void SetupAlphaTransitionMaterialKeywords(Material material)
        {
            var alphaTransitionEnabled = material.GetTexture(AlphaTransitionMapId) != null;
            
            var alphaTransitionMode =
                (AlphaTransitionMode)material.GetFloat(AlphaTransitionModeId);
            var fadeTransitionEnabled = alphaTransitionEnabled && alphaTransitionMode == AlphaTransitionMode.Fade;
            var dissolveTransitionEnabled =
                alphaTransitionEnabled && alphaTransitionMode == AlphaTransitionMode.Dissolve;
            MaterialEditorUtility.SetKeyword(material, ShaderKeywords.FadeTransitionEnabled, fadeTransitionEnabled);
            MaterialEditorUtility.SetKeyword(material, ShaderKeywords.DissolveTransitionEnabled,
                dissolveTransitionEnabled);
        }

        private static void SetupTransparencyMaterialKeywords(Material material)
        {
            var softParticlesEnabled = material.GetFloat(SoftParticlesEnabledId) > 0.5f;
            MaterialEditorUtility.SetKeyword(material, ShaderKeywords.SoftParticlesEnabled, softParticlesEnabled);

            var depthFadeEnabled = material.GetFloat(DepthFadeEnabledId) > 0.5f;
            MaterialEditorUtility.SetKeyword(material, ShaderKeywords.DepthFadeEnabled, depthFadeEnabled);
        }
    }
}