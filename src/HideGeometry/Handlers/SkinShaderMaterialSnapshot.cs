using UnityEngine;

namespace Handlers
{
    public class SkinShaderMaterialSnapshot
    {
        public Material material;
        public bool alphaAdjustSupport;
        public bool specColorSupport;

        public Shader originalShader;
        public float originalAlphaAdjust;
        public float originalColorAlpha;
        public Color originalSpecColor;
        public bool originalAlphaAdjustSupport;
        public bool originalSpecColorSupport;

        public static SkinShaderMaterialSnapshot FromMaterial(Material material)
        {
            var originalAlphaAdjustSupport = material.HasProperty("_AlphaAdjust");
            var originalSpecColorSupport = material.HasProperty("_SpecColor");
            return new SkinShaderMaterialSnapshot
            {
                material = material,
                originalShader = material.shader,
                originalAlphaAdjustSupport = originalAlphaAdjustSupport,
                originalAlphaAdjust = originalAlphaAdjustSupport ? material.GetFloat("_AlphaAdjust") : 0,
                originalColorAlpha = material.GetColor("_Color").a,
                originalSpecColorSupport = originalSpecColorSupport,
                originalSpecColor = originalSpecColorSupport ? material.GetColor("_SpecColor") : Color.black
            };
        }
    }
}
