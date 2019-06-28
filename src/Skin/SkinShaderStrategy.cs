#define POV_DIAGNOSTICS
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Acidbubbles.ImprovedPoV.Skin
{
    public class SkinShaderStrategy : IStrategy
    {
        public const string Name = "Shaders (allows mirrors)";

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

        string IStrategy.Name
        {
            get { return Name; }
        }

        private DAZSkinV2 _skin;
        private SkinShaderMirrorStrategy _mirrorStrategy;

        public void Apply(PersonReference person)
        {
            if (_mirrorStrategy != null) return;
            // throw new InvalidOperationException("Attempts to apply the shader strategy on a skin that already has the plugin enabled (reference).");

            var materials = new List<SkinShaderMaterialReference>();

            foreach (var material in SkinMaterialsHelper.GetMaterialsToHide(person.skin))
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

                materialInfo.ApplyReplacementShader(shader);

                materials.Add(materialInfo);
            }

            _skin = person.skin;
            person.skinStrategy = _mirrorStrategy = new SkinShaderMirrorStrategy(Name, materials);

            // This is a hack to force a refresh of the shaders cache
            person.skin.BroadcastMessage("OnApplicationFocus", true);
        }

        public void Restore()
        {
            var mirrorStrategy = _mirrorStrategy;

            _mirrorStrategy = null;

            // Already restored (abnormal)
            if (mirrorStrategy == null) throw new InvalidOperationException("Attempt to Restore but the previous material container does not exist");

            foreach (var material in mirrorStrategy.materials)
            {
                material.RestoreOriginalShader();
            }

            // This is a hack to force a refresh of the shaders cache
            if (_skin != null)
                _skin.BroadcastMessage("OnApplicationFocus", true);
        }

        public IMirrorStrategy GetMirrorStrategy(object data)
        {
            return SkinShaderMirrorStrategy.FromBroadcastable(Name, data);
        }
    }
}