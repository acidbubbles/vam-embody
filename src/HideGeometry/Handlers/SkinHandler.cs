using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Handlers
{
    public class SkinHandler : IHandler
    {
        private readonly DAZSkinV2 _skin;
        private readonly IEnumerable<string> _materialsToHide;
        private readonly int _materialsToHideMax;
        private List<SkinShaderMaterialSnapshot> _materialRefs;

        public SkinHandler(DAZSkinV2 skin, IEnumerable<string> materialsToHide, int materialsToHideMax)
        {
            _skin = skin;
            _materialsToHide = materialsToHide;
            _materialsToHideMax = materialsToHideMax;
        }

        public bool Prepare()
        {
            _materialRefs = new List<SkinShaderMaterialSnapshot>();

            foreach (var material in GetMaterialsToHide())
            {
                var materialInfo = SkinShaderMaterialSnapshot.FromMaterial(material);

                Shader shader;
                if (!ReplacementShaders.ShadersMap.TryGetValue(material.shader.name, out shader))
                    SuperController.LogError("Missing replacement shader: '" + material.shader.name + $"' ({material.name} will not be hidden)");

                if (shader != null)
                {
                    material.shader = shader;
                    materialInfo.alphaAdjustSupport = material.HasProperty("_AlphaAdjust");
                    materialInfo.alphaCutoffSupport = material.HasProperty("_Cutoff");
                    materialInfo.specColorSupport = material.HasProperty("_SpecColor");
                }
                else
                {
                    materialInfo.alphaAdjustSupport = materialInfo.originalAlphaAdjustSupport;
                    materialInfo.alphaCutoffSupport = materialInfo.originalAlphaCutoffSupport;
                    materialInfo.specColorSupport = materialInfo.originalSpecColorSupport;
                }

                _materialRefs.Add(materialInfo);
            }

            // This is a hack to force a refresh of the shaders cache
            _skin.BroadcastMessage("OnApplicationFocus", true);
            return true;
        }

        private IEnumerable<Material> GetMaterialsToHide()
        {
            var materials = new List<Material>(_materialsToHideMax);

            foreach (var material in _skin.GPUmaterials)
            {
                if (material == null)
                    continue;
                if (!_materialsToHide.Any(materialToHide => material.name.StartsWith(materialToHide)))
                    continue;

                materials.Add(material);
            }

            return materials;
        }

        public void Dispose()
        {
            foreach (var material in _materialRefs)
                material.material.shader = material.originalShader;

            // This is a hack to force a refresh of the shaders cache
            _skin.BroadcastMessage("OnApplicationFocus", true);

            _materialRefs.Clear();
        }

        public void BeforeRender()
        {
            for (var i = 0; i < _materialRefs.Count; i++)
            {
                var materialRef = _materialRefs[i];
                var material = materialRef.material;
                if (materialRef.alphaCutoffSupport)
                    material.SetFloat("_Cutoff", 0.3f);
                if (materialRef.alphaAdjustSupport)
                    material.SetFloat("_AlphaAdjust", -1f);
                var color = material.GetColor("_Color");
                material.SetColor("_Color", new Color(color.r, color.g, color.b, 0f));
                if (materialRef.specColorSupport)
                    material.SetColor("_SpecColor", new Color(0f, 0f, 0f, 0f));
            }
        }

        public void AfterRender()
        {
            for (var i = 0; i < _materialRefs.Count; i++)
            {
                var materialRef = _materialRefs[i];
                var material = materialRef.material;
                if (materialRef.alphaCutoffSupport)
                    material.SetFloat("_Cutoff", materialRef.originalAlphaCutoff);
                if (materialRef.alphaAdjustSupport)
                    material.SetFloat("_AlphaAdjust", materialRef.originalAlphaAdjust);
                var color = material.GetColor("_Color");
                material.SetColor("_Color", new Color(color.r, color.g, color.b, materialRef.originalColorAlpha));
                if (materialRef.specColorSupport)
                    material.SetColor("_SpecColor", materialRef.originalSpecColor);
            }
        }
    }
}
