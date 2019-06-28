#define POV_DIAGNOSTICS
using System.Collections.Generic;
using UnityEngine;

namespace Acidbubbles.ImprovedPoV.Skin
{
    public class SkinShaderMaterialReference
    {
        public const string ImprovedPovEnabledShaderKey = "_ImprovedPovEnabled";

        public Material material;
        public Shader originalShader;
        public float originalAlphaAdjust;
        public Color originalColor;
        public Color originalSpecColor;
        public int originalRenderQueue;
        private bool _tainted;

        public static SkinShaderMaterialReference FromMaterial(Material material)
        {
            var materialRef = new SkinShaderMaterialReference();
            materialRef.material = material;
            materialRef.originalShader = material.shader;
            materialRef.originalAlphaAdjust = material.GetFloat("_AlphaAdjust");
            materialRef.originalColor = material.GetColor("_Color");
            materialRef.originalSpecColor = material.GetColor("_SpecColor");
            materialRef.originalRenderQueue = material.renderQueue;
            return materialRef;
        }

        public void ApplyReplacementShader(Shader shader)
        {
            if (shader != null) material.shader = shader;
            material.SetInt(ImprovedPovEnabledShaderKey, 1);
            MakeInvisible();
        }

        public void RestoreOriginalShader()
        {
            MakeVisible();
            material.SetInt(ImprovedPovEnabledShaderKey, 0);
            material.shader = originalShader;
        }

        public void MakeVisible()
        {
            if (material.GetInt(ImprovedPovEnabledShaderKey) != 1) return;

            material.SetFloat("_AlphaAdjust", originalAlphaAdjust);
            material.SetColor("_Color", originalColor);
            material.SetColor("_SpecColor", originalSpecColor);
        }

        public void MakeInvisible()
        {
            if (material.GetInt(ImprovedPovEnabledShaderKey) != 1) return;

            material.SetFloat("_AlphaAdjust", -1f);
            material.SetColor("_Color", new Color(0f, 0f, 0f, 0f));
            material.SetColor("_SpecColor", new Color(0f, 0f, 0f, 0f));
        }

        public static SkinShaderMaterialReference FromBroadcastable(object value)
        {
            var list = (List<object>)value;
            return new SkinShaderMaterialReference
            {
                material = (Material)list[0],
                originalAlphaAdjust = (float)list[1],
                originalColor = (Color)list[2],
                originalSpecColor = (Color)list[3]
            };
        }

        public object ToBroadcastable()
        {
            return new List<object>{
                material,
                originalAlphaAdjust,
                originalColor,
                originalSpecColor
            };
        }
    }
}