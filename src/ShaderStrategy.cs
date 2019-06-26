using System;
using System.Collections.Generic;
using UnityEngine;

namespace Acidbubbles.ImprovedPoV
{
    public class ShaderStrategy : IStrategy
    {
        public const string Name = "Shaders (allows mirrors)";

        private static Dictionary<string, Shader> ReplacementShaders = new Dictionary<string, Shader>
            {
                // Opaque materials
                { "Custom/Subsurface/GlossCullComputeBuff", Shader.Find("Custom/Subsurface/TransparentGlossSeparateAlphaComputeBuff") },
                { "Custom/Subsurface/GlossNMCullComputeBuff", Shader.Find("Custom/Subsurface/TransparentGlossNMSeparateAlphaComputeBuff") },
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

        private MemoizedPerson _memoized;

        public void Apply(DAZSkinV2 skin)
        {
            // Check if already applied
            if (_memoized != null)
                return;

            var memoized = new MemoizedPerson();

            foreach (var material in MaterialsHelper.GetMaterialsToHide(skin))
            {
                var materialInfo = MemoizedMaterial.FromMaterial(material);

                Shader shader;
                if (!ReplacementShaders.TryGetValue(material.shader.name, out shader))
                    SuperController.LogError("Missing replacement shader: '" + material.shader.name + "'");

                materialInfo.ApplyReplacementShader(shader);

                memoized.Add(materialInfo);
            }

            State.Register(_memoized = memoized);

            // This is a hack to force a refresh of the shaders cache
            skin.BroadcastMessage("OnApplicationFocus", true);
        }

        public void Restore(DAZSkinV2 skin)
        {
            var memoized = _memoized;

            State.Unregister(memoized);
            _memoized = null;

            // Already restored (abnormal)
            if (memoized == null) throw new InvalidOperationException("Attempt to Restore but the previous material container does not exist");

            foreach (var material in memoized)
            {
                material.RestoreOriginalShader();
            }

            State.Unregister(memoized);

            // This is a hack to force a refresh of the shaders cache
            skin.BroadcastMessage("OnApplicationFocus", true);
        }
    }
}