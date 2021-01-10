using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Handlers
{
    public static class KnownMaterials
    {
        private static readonly string[] _materialsToHide = new[]
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
            var materials = new List<Material>(_materialsToHide.Length);

            foreach (var material in skin.GPUmaterials)
            {
                if (material == null)
                    continue;
                if (!_materialsToHide.Any(materialToHide => material.name.StartsWith(materialToHide)))
                    continue;

                materials.Add(material);
            }

            return materials;
        }
    }
}
