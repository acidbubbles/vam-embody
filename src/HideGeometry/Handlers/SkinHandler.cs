using System.Collections.Generic;
using UnityEngine;

namespace Handlers
{
    public class SkinHandler : IHandler
    {
        private readonly DAZSkinV2 _skin;
        private List<SkinShaderMaterialSnapshot> _materialRefs;

        public SkinHandler(DAZSkinV2 skin)
        {
            _skin = skin;
        }

        public bool Prepare()
        {
            _materialRefs = new List<SkinShaderMaterialSnapshot>();

            foreach (var material in KnownMaterials.GetMaterialsToHide(_skin))
            {
                var materialInfo = SkinShaderMaterialSnapshot.FromMaterial(material);

                Shader shader;
                if (!ReplacementShaders.ShadersMap.TryGetValue(material.shader.name, out shader))
                    SuperController.LogError("Missing replacement shader: '" + material.shader.name + "'");

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

                    SuperController.LogMessage($"{materialInfo.material.name}: {(materialInfo.originalShader == materialInfo.material.shader ? "KEEP" : "CHANGE")}  {(materialInfo.alphaCutoffSupport ? "CUTOFF" : "")}");

                _materialRefs.Add(materialInfo);
            }

            // This is a hack to force a refresh of the shaders cache
            _skin.BroadcastMessage("OnApplicationFocus", true);
            return true;
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
