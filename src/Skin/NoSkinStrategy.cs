#define POV_DIAGNOSTICS

namespace Acidbubbles.ImprovedPoV.Skin
{
    public class NoSkinStrategy : IStrategy
    {
        public const string Name = "None (face mesh visible)";

        string IStrategy.Name
        {
            get { return Name; }
        }

        public void Apply(PersonReference person)
        {
        }

        public void Restore()
        {
        }

        public IMirrorStrategy GetMirrorStrategy(object data)
        {
            return null;
        }
    }
}