#define POV_DIAGNOSTICS

namespace Acidbubbles.ImprovedPoV
{
    public class NoStrategy : IStrategy
    {
        public const string Name = "None (face mesh visible)";

        string IStrategy.Name
        {
            get { return Name; }
        }

        public void Apply(DAZSkinV2 skin)
        {
        }

        public void Restore(DAZSkinV2 skin)
        {
        }
    }
}