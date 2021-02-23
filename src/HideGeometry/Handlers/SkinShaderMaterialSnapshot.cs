using UnityEngine;

namespace Handlers
{
    public class SkinShaderMaterialSnapshot
    {
        public Material material;
        public Shader originalShader;
        public float originalAlphaAdjust;
        public float originalColorAlpha;
        public Color originalSpecColor;
        public bool supportsAlphaAdjust;
        public bool supportsSpecColor;

        public static SkinShaderMaterialSnapshot FromMaterial(Material material)
        {
            var supportsAlphaAdjust = material.HasProperty("_AlphaAdjust");
            var supportsSpecColor = material.HasProperty("_SpecColor");
            return new SkinShaderMaterialSnapshot
            {
                material = material,
                originalShader = material.shader,
                supportsAlphaAdjust = supportsAlphaAdjust,
                originalAlphaAdjust = supportsAlphaAdjust ? material.GetFloat("_AlphaAdjust") : 0,
                originalColorAlpha = material.GetColor("_Color").a,
                supportsSpecColor = supportsSpecColor,
                originalSpecColor = supportsSpecColor ? material.GetColor("_SpecColor") : Color.black
            };
        }
    }
}
