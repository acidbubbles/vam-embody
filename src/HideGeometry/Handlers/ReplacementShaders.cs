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
            { "Custom/Subsurface/TransparentGlossNoCullSeparateAlphaComputeBuff", null },
            { "Custom/Subsurface/TransparentGlossComputeBuff", null },
            { "Custom/Subsurface/TransparentComputeBuff", null },
            { "Custom/Subsurface/AlphaMaskComputeBuff", null },
            { "Marmoset/Transparent/Simple Glass/Specular IBLComputeBuff", null },
        };

    }
}
