using UnityEngine;

namespace Handlers
{
    public class SkinShaderMaterialSnapshot
    {
        public Material material;
        public bool alphaAdjustSupport;
        public bool alphaCutoffSupport;
        public bool specColorSupport;

        public Shader originalShader;
        public float originalAlphaAdjust;
        public float originalAlphaCutoff;
        public float originalColorAlpha;
        public Color originalSpecColor;
        public bool originalAlphaAdjustSupport;
        public bool originalAlphaCutoffSupport;
        public bool originalSpecColorSupport;

        public static SkinShaderMaterialSnapshot FromMaterial(Material material)
        {
            var originalAlphaAdjustSupport = material.HasProperty("_AlphaAdjust");
            var originalAlphaCutoffSupport = material.HasProperty("_Cutoff");
            var originalSpecColorSupport = material.HasProperty("_SpecColor");
            return new SkinShaderMaterialSnapshot
            {
                material = material,
                originalShader = material.shader,
                originalAlphaAdjustSupport = originalAlphaAdjustSupport,
                originalAlphaAdjust = originalAlphaAdjustSupport ? material.GetFloat("_AlphaAdjust") : 0,
                originalAlphaCutoffSupport = originalAlphaCutoffSupport,
                originalAlphaCutoff = originalAlphaCutoffSupport ? material.GetFloat("_Cutoff") : 0,
                originalColorAlpha = material.GetColor("_Color").a,
                originalSpecColorSupport = originalSpecColorSupport,
                originalSpecColor = originalSpecColorSupport ? material.GetColor("_SpecColor") : Color.black
            };
        }
    }
}
