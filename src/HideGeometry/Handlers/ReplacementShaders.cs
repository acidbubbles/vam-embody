using System.Collections.Generic;
using UnityEngine;

namespace Handlers
{
    public static class ReplacementShaders
    {
        public static readonly Dictionary<string, Shader> ShadersMap = new Dictionary<string, Shader>
        {
            // Opaque materials
            { "Custom/Subsurface/GlossCullComputeBuff", Shader.Find("Custom/Subsurface/TransparentGlossSeparateAlphaComputeBuff") },
            { "Custom/Subsurface/GlossNMCullComputeBuff", Shader.Find("Custom/Subsurface/TransparentGlossNMSeparateAlphaComputeBuff") },
            { "Custom/Subsurface/GlossNMDetailCullComputeBuff", Shader.Find("Custom/Subsurface/TransparentGlossNMDetailNoCullSeparateAlphaComputeBuff") },
            { "Custom/Subsurface/CullComputeBuff", Shader.Find("Custom/Subsurface/TransparentSeparateAlphaComputeBuff") },

            // Transparent materials
            { "Custom/Subsurface/TransparentGlossNMNoCullSeparateAlphaComputeBuff", null },
            { "Custom/Subsurface/TransparentGlossNoCullSeparateAlphaComputeBuff", null },
            { "Custom/Subsurface/TransparentGlossComputeBuff", null },
            { "Custom/Subsurface/TransparentComputeBuff", null },
            { "Custom/Subsurface/AlphaMaskComputeBuff", null },
            { "Marmoset/Transparent/Simple Glass/Specular IBLComputeBuff", null },

            // Hunting-Succubus' tesselation material
            { "Custom/Subsurface/GlossNMTessMappedFixedComputeBuff", Shader.Find("Custom/Subsurface/TransparentGlossNMDetailNoCullSeparateAlphaComputeBuff") },

            // NorthernShikima.SkinMicroDetail tesselation material
            { "Custom/Subsurface/GlossNMDetailTessMappedComputeBuff", Shader.Find("Custom/Subsurface/TransparentGlossNMSeparateAlphaComputeBuff") },

            // If we currently work with incorrectly restored materials, let's just keep them
            { "Custom/Subsurface/TransparentGlossSeparateAlphaComputeBuff", null },
            { "Custom/Subsurface/TransparentGlossNMSeparateAlphaComputeBuff", null },
            { "Custom/Subsurface/TransparentGlossNMDetailNoCullSeparateAlphaComputeBuff", null },
            { "Custom/Subsurface/TransparentSeparateAlphaComputeBuff", null }
        };

    }
}
