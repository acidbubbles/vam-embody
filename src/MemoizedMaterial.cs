#define POV_DIAGNOSTICS
using System.Collections.Generic;
using UnityEngine;

namespace Acidbubbles.ImprovedPoV
{
    public class MemoizedMaterial
    {
        public const string ImprovedPovEnabledShaderKey = "_ImprovedPovEnabled";

        public Material material;
        public Shader originalShader;
        public float originalAlphaAdjust;
        public Color originalColor;
        public Color originalSpecColor;
        public int originalRenderQueue;
        private bool _tainted;

        public static MemoizedMaterial FromMaterial(Material material)
        {
            var memoized = new MemoizedMaterial();
            memoized.material = material;
            memoized.originalShader = material.shader;
            memoized.originalAlphaAdjust = material.GetFloat("_AlphaAdjust");
            memoized.originalColor = material.GetColor("_Color");
            memoized.originalSpecColor = material.GetColor("_SpecColor");
            memoized.originalRenderQueue = material.renderQueue;
            return memoized;
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

        public static MemoizedMaterial FromBroadcastable(List<object> value)
        {
            return new MemoizedMaterial
            {
                material = (Material)value[0],
                originalAlphaAdjust = (float)value[1],
                originalColor = (Color)value[2],
                originalSpecColor = (Color)value[3]
            };
        }

        public List<object> ToBroadcastable()
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