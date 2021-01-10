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

        public static SkinShaderMaterialSnapshot FromMaterial(Material material)
        {
            return new SkinShaderMaterialSnapshot
            {
                material = material,
                originalShader = material.shader,
                originalAlphaAdjust = material.GetFloat("_AlphaAdjust"),
                originalColorAlpha = material.GetColor("_Color").a,
                originalSpecColor = material.GetColor("_SpecColor")
            };
        }
    }
}
