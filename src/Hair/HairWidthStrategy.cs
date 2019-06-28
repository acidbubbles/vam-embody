#define POV_DIAGNOSTICS

using GPUTools.Hair.Scripts;

namespace Acidbubbles.ImprovedPoV.Hair
{
    public class HairWidthStrategy : IStrategy
    {
        public const string Name = "Zero Width";

        string IStrategy.Name
        {
            get { return Name; }
        }

        private PersonReference _person;

        public void Apply(PersonReference person)
        {
            _person = person;
            UpdateHairWidth(person.hair, false);
        }

        public void Restore()
        {
            UpdateHairWidth(_person.hair, true);
        }

        public void UpdateHairWidth(DAZHairGroup hair, bool enabled)
        {

            // NOTE: In progress
            // NOTE: Only applies to SimV2 hair
            // TODO: Publish that to mirrors
            // TODO: Push the original settings in the person reference for eventual restore
            foreach (var x in hair.gameObject.GetComponentsInChildren<HairSettings>())
            {
                // TODO: Restore completely
                x.LODSettings.UseFixedSettings = !enabled;
                x.LODSettings.FixedWidth = 0f;
            }
        }

        public IMirrorStrategy GetMirrorStrategy(object data)
        {
            return null;
        }
    }
}