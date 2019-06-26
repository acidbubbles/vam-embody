#define POV_DIAGNOSTICS
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Acidbubbles.ImprovedPoV
{
    public class MaterialsHelper
    {
        public static readonly string[] MaterialsToHide = new[]
        {
            "Lacrimals",
            "Pupils",
            "Lips",
            "Gums",
            "Irises",
            "Teeth",
            "Face",
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
                if (!MaterialsToHide.Any(materialToHide => material.name.StartsWith(materialToHide)))
                    continue;

                materials.Add(material);
            }

            if(materials.Count < 13)
                throw new Exception("Not enough materials found to hide. List: " + string.Join(", ", skin.GPUmaterials.Select(m => m.name).ToArray()));

            return materials;
        }
    }
}