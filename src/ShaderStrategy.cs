#define POV_DIAGNOSTICS
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

        private MemoizedPerson _memoized;
        private SceneWatcher _watcher;

        public void Apply(DAZSkinV2 skin)
        {
            // TODO: After loading a new skin, only reloading the plugin will work and hide the face. Why?

            if (_memoized != null) return;
                // throw new InvalidOperationException("Attempts to apply the shader strategy on a skin that already has the plugin enabled (memoized).");

            SuperController.LogMessage("Apply " + skin.name);
            _memoized = new MemoizedPerson();
            _watcher = new SceneWatcher(_memoized);

            foreach (var material in MaterialsHelper.GetMaterialsToHide(skin))
            {
                #if(IMPROVED_POV)
                if (material.GetInt(MemoizedMaterial.ImprovedPovEnabledShaderKey) == 1)
                    throw new InvalidOperationException("Attempts to apply the shader strategy on a skin that already has the plugin enabled (shader key).");

                var materialInfo = MemoizedMaterial.FromMaterial(material);

                Shader shader;
                if (!ReplacementShaders.TryGetValue(material.shader.name, out shader))
                    SuperController.LogError("Missing replacement shader: '" + material.shader.name + "'");

                materialInfo.ApplyReplacementShader(shader);

                _memoized.Add(materialInfo);
            }

            _watcher.Start();

            // This is a hack to force a refresh of the shaders cache
            skin.BroadcastMessage("OnApplicationFocus", true);
        }

        public void Restore(DAZSkinV2 skin)
        {
            SuperController.LogMessage("Restore " + skin.name);
            var memoized = _memoized;
            var watcher = _watcher;

            _watcher.Stop();

            _memoized = null;
            _watcher = null;

            // Already restored (abnormal)
            if (memoized == null) throw new InvalidOperationException("Attempt to Restore but the previous material container does not exist");

            foreach (var material in memoized)
            {
                material.RestoreOriginalShader();
            }

            // This is a hack to force a refresh of the shaders cache
            skin.BroadcastMessage("OnApplicationFocus", true);
        }
    }
}