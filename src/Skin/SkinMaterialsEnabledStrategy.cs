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

        private PersonReference _person;

        public void Apply(PersonReference person)
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

        public IMirrorStrategy GetMirrorStrategy(object data)
        {
            // NOTE: Enabling and disabling materials during mirror render doesn't work
            return null;
        }
    }
}