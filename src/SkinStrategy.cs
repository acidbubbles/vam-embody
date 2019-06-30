#define POV_DIAGNOSTICS
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Acidbubbles.ImprovedPoV
{
    public class SkinStrategy : MonoBehaviour
    {
        public static readonly string[] MaterialsToHide = new[]
        {
            "Lacrimals",
            "Pupils",
            "Lips",
            "Gums",
            "Irises",
            "Teeth",
            "Face",
            "InnerMouth",
            "Tongue",
            "EyeReflection",
            "Nostrils",
            "Cornea",
            "Eyelashes",
            "Sclera",
            "Ears",
            "Tear"
        };

        public static IList<Material> GetMaterialsToHide(DAZSkinV2 skin)
        {
            var materials = new List<Material>(MaterialsToHide.Length);

            foreach (var material in skin.GPUmaterials)
            {
                if (!MaterialsToHide.Any(materialToHide => material.name.StartsWith(materialToHide)))
                    continue;

                materials.Add(material);
            }

#if(POV_DIAGNOSTICS)
            // NOTE: Tear is not on all models
            if (materials.Count < MaterialsToHide.Length - 1)
                throw new Exception("Not enough materials found to hide. List: " + string.Join(", ", skin.GPUmaterials.Select(m => m.name).ToArray()));
#endif

            return materials;
        }

        private static Dictionary<string, Shader> ReplacementShaders = new Dictionary<string, Shader>
            {
                // Opaque materials
                { "Custom/Subsurface/GlossCullComputeBuff", Shader.Find("Custom/Subsurface/TransparentGlossSeparateAlphaComputeBuff") },
                { "Custom/Subsurface/GlossNMCullComputeBuff", Shader.Find("Custom/Subsurface/TransparentGlossNMSeparateAlphaComputeBuff") },
                { "Custom/Subsurface/GlossNMDetailCullComputeBuff", Shader.Find("Custom/Subsurface/TransparentGlossNMDetailNoCullSeparateAlphaComputeBuff") },
                { "Custom/Subsurface/CullComputeBuff", Shader.Find("Custom/Subsurface/TransparentSeparateAlphaComputeBuff") },

                // Transparent materials
                { "Custom/Subsurface/TransparentGlossNoCullSeparateAlphaComputeBuff", null },
                { "Custom/Subsurface/TransparentGlossComputeBuff", null },
                { "Custom/Subsurface/TransparentComputeBuff", null },
                { "Custom/Subsurface/AlphaMaskComputeBuff", null },
                { "Marmoset/Transparent/Simple Glass/Specular IBLComputeBuff", null },
            };

        private DAZSkinV2 _skin;
        private List<SkinShaderMaterialReference> _materialRefs;

        public void Configure(DAZSkinV2 skin)
        {
            _skin = skin;
            _materialRefs = new List<SkinShaderMaterialReference>();

            foreach (var material in GetMaterialsToHide(skin))
            {
#if (IMPROVED_POV)
                if(material == null)
                    throw new InvalidOperationException("Attempts to apply the shader strategy on a destroyed material.");

                if (material.GetInt(SkinShaderMaterialReference.ImprovedPovEnabledShaderKey) == 1)
                    throw new InvalidOperationException("Attempts to apply the shader strategy on a skin that already has the plugin enabled (shader key).");
#endif

                var materialInfo = SkinShaderMaterialReference.FromMaterial(material);

                Shader shader;
                if (!ReplacementShaders.TryGetValue(material.shader.name, out shader))
                    SuperController.LogError("Missing replacement shader: '" + material.shader.name + "'");

                if (shader != null) material.shader = shader;

                _materialRefs.Add(materialInfo);
            }

            // This is a hack to force a refresh of the shaders cache
            skin.BroadcastMessage("OnApplicationFocus", true);
        }

        public void OnDestroy()
        {
            foreach (var material in _materialRefs)
                material.material.shader = material.originalShader;

            _materialRefs = null;

            // This is a hack to force a refresh of the shaders cache
            _skin.BroadcastMessage("OnApplicationFocus", true);
        }

        public void OnWillRenderObject()
        {
            if (Camera.current.name == "MonitorRig")
            {
                foreach (var materialRef in _materialRefs)
                {
                    var material = materialRef.material;
                    material.SetFloat("_AlphaAdjust", -1f);
                    material.SetColor("_Color", new Color(0f, 0f, 0f, 0f));
                    material.SetColor("_SpecColor", new Color(0f, 0f, 0f, 0f));
                }
            }
        }

        public void OnRenderObject()
        {
            if (Camera.current.name == "MonitorRig")
            {
                foreach (var materialRef in _materialRefs)
                {
                    var material = materialRef.material;
                    material.SetFloat("_AlphaAdjust", materialRef.originalAlphaAdjust);
                    material.SetColor("_Color", materialRef.originalColor);
                    material.SetColor("_SpecColor", materialRef.originalSpecColor);
                }
            }
        }
    }

    public class SkinShaderMaterialReference
    {
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
    }
}