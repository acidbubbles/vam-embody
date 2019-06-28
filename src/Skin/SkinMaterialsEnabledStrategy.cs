#define POV_DIAGNOSTICS
using System.Linq;
using UnityEngine;

namespace Acidbubbles.ImprovedPoV.Skin
{
    public class SkinMaterialsEnabledStrategy : IStrategy
    {
        public const string Name = "Materials Enabled (performance)";

        string IStrategy.Name
        {
            get { return Name; }
        }

        private MemoizedPerson _person;

        public void Apply(MemoizedPerson person)
        {
            _person = person;
            UpdateMaterialsEnabled(person.skin, false);
        }

        public void Restore()
        {
            UpdateMaterialsEnabled(_person.skin, true);
        }

        public void UpdateMaterialsEnabled(DAZSkinV2 skin, bool enabled)
        {

            for (int i = 0; i < skin.GPUmaterials.Length; i++)
            {
                Material mat = skin.GPUmaterials[i];
                if (SkinMaterialsHelper.MaterialsToHide.Any(materialToHide => mat.name.StartsWith(materialToHide)))
                {
                    skin.materialsEnabled[i] = enabled;
                }
            }
        }
    }
}