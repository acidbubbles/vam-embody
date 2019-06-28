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

        private MemoizedPerson _person;
        private SceneWatcher _watcher;

        public void Apply(MemoizedPerson person)
        {
            if (_person != null) return;
            // throw new InvalidOperationException("Attempts to apply the shader strategy on a skin that already has the plugin enabled (memoized).");

            var materials = new List<MemoizedMaterial>();

            foreach (var material in SkinMaterialsHelper.GetMaterialsToHide(person.skin))
            {
#if (IMPROVED_POV)
                if(material == null)
                    throw new InvalidOperationException("Attempts to apply the shader strategy on a destroyed material.");

                if (material.GetInt(MemoizedMaterial.ImprovedPovEnabledShaderKey) == 1)
                    throw new InvalidOperationException("Attempts to apply the shader strategy on a skin that already has the plugin enabled (shader key).");
#endif

                var materialInfo = MemoizedMaterial.FromMaterial(material);

                Shader shader;
                if (!ReplacementShaders.TryGetValue(material.shader.name, out shader))
                    SuperController.LogError("Missing replacement shader: '" + material.shader.name + "'");

                materialInfo.ApplyReplacementShader(shader);

                materials.Add(materialInfo);
            }

            person.materials = materials;
            _person = person;
            // TODO: Move this out of the Shader strategy, so the hair strategy can also make use of it
            _watcher = new SceneWatcher(_person);

            _watcher.Start();

            // This is a hack to force a refresh of the shaders cache
            person.skin.BroadcastMessage("OnApplicationFocus", true);
        }

        public void Restore()
        {
            var memoized = _person;
            var watcher = _watcher;

            _watcher.Stop();

            _person = null;
            _watcher = null;

            // Already restored (abnormal)
            if (memoized == null) throw new InvalidOperationException("Attempt to Restore but the previous material container does not exist");

            foreach (var material in memoized.materials)
            {
                material.RestoreOriginalShader();
            }

            // This is a hack to force a refresh of the shaders cache
            if (memoized != null)
                memoized.skin.BroadcastMessage("OnApplicationFocus", true);
        }
    }
}