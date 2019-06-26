#define POV_DIAGNOSTICS
using System.Linq;
using UnityEngine;

namespace Acidbubbles.ImprovedPoV
{
    public class MaterialsEnabledStrategy : IStrategy
    {
        public const string Name = "Materials Enabled (performance)";

        string IStrategy.Name
        {
            get { return Name; }
        }

        public void Apply(DAZSkinV2 skin)
        {
            UpdateMaterialsEnabled(skin, false);
        }

        public void Restore(DAZSkinV2 skin)
        {
            UpdateMaterialsEnabled(skin, true);
        }

        public void UpdateMaterialsEnabled(DAZSkinV2 skin, bool enabled)
        {

            for (int i = 0; i < skin.GPUmaterials.Length; i++)
            {
                Material mat = skin.GPUmaterials[i];
                if (MaterialsHelper.MaterialsToHide.Any(materialToHide => mat.name.StartsWith(materialToHide)))
                {
                    skin.materialsEnabled[i] = enabled;
                }
            }
        }
    }
}