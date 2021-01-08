// #define POV_DIAGNOSTICS

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Handlers
{
    public class SkinHandler : IHandler
    {
        public class SkinShaderMaterialReference
        {
            public Material material;
            public Shader originalShader;
            public float originalAlphaAdjust;
            public float originalColorAlpha;
            public Color originalSpecColor;

            public static SkinShaderMaterialReference FromMaterial(Material material)
            {
                return new SkinShaderMaterialReference
                {
                    material = material,
                    originalShader = material.shader,
                    originalAlphaAdjust = material.GetFloat("_AlphaAdjust"),
                    originalColorAlpha = material.GetColor("_Color").a,
                    originalSpecColor = material.GetColor("_SpecColor")
                };
            }
        }

        public static readonly string[] MaterialsToHide = new[]
        {
            "Lacrimals",
            "Pupils",
            "Lips",
            "Gums",
            "Irises",
            "Teeth",
            "Face",
            "Head",
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
                if (material == null)
                    continue;
                if (!MaterialsToHide.Any(materialToHide => material.name.StartsWith(materialToHide)))
                    continue;

                materials.Add(material);
            }

#if (POV_DIAGNOSTICS)
            // NOTE: Tear is not on all models
            if (materials.Count < MaterialsToHide.Length - 1)
                throw new Exception("Not enough materials found to hide. List: " + string.Join(", ", skin.GPUmaterials.Select(m => m.name).ToArray()));
#endif

            return materials;
        }

        private static readonly Dictionary<string, Shader> _replacementShaders = new Dictionary<string, Shader>
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

        public int Configure(DAZSkinV2 skin)
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
                if (!_replacementShaders.TryGetValue(material.shader.name, out shader))
                    SuperController.LogError("Missing replacement shader: '" + material.shader.name + "'");

                if (shader != null) material.shader = shader;

                _materialRefs.Add(materialInfo);
            }

            // This is a hack to force a refresh of the shaders cache
            skin.BroadcastMessage("OnApplicationFocus", true);
            return HandlerConfigurationResult.Success;
        }

        public void Restore()
        {
            foreach (var material in _materialRefs)
                material.material.shader = material.originalShader;

            _materialRefs = null;

            // This is a hack to force a refresh of the shaders cache
            _skin.BroadcastMessage("OnApplicationFocus", true);
        }

        public void BeforeRender()
        {
            foreach (var materialRef in _materialRefs)
            {
                var material = materialRef.material;
                material.SetFloat("_AlphaAdjust", -1f);
                var color = material.GetColor("_Color");
                material.SetColor("_Color", new Color(color.r, color.g, color.b, 0f));
                material.SetColor("_SpecColor", new Color(0f, 0f, 0f, 0f));
            }
        }

        public void AfterRender()
        {
            foreach (var materialRef in _materialRefs)
            {
                var material = materialRef.material;
                material.SetFloat("_AlphaAdjust", materialRef.originalAlphaAdjust);
                var color = material.GetColor("_Color");
                material.SetColor("_Color", new Color(color.r, color.g, color.b, materialRef.originalColorAlpha));
                material.SetColor("_SpecColor", materialRef.originalSpecColor);
            }
        }
    }
}
