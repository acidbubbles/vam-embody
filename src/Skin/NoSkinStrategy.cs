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

        public void Apply(MemoizedPerson person)
        {
        }

        public void Restore()
        {
        }
    }
}